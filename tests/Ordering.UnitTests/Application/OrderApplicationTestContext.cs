using eShop.IntegrationEventLogEF;
using eShop.Ordering.API.Application.IntegrationEvents;
using eShop.Ordering.API.Application.Validations;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Infrastructure;
using eShop.Ordering.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace eShop.Ordering.UnitTests.Application;

internal sealed class OrderApplicationTestContext : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public OrderingContext DbContext { get; }
    public IOrderingIntegrationEventService IntegrationEvents { get; } = Substitute.For<IOrderingIntegrationEventService>();
    public OrderApplicationService Application { get; }
    public List<eShop.EventBus.Events.IntegrationEvent> Events { get; } = [];
    public List<Guid> PublishedTransactions { get; } = [];

    public OrderApplicationTestContext(IRequestManager requestManager = null, DbTransactionInterceptor transactionInterceptor = null)
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<OrderingContext>().UseSqlite(_connection);
        if (transactionInterceptor is not null)
        {
            options.AddInterceptors(transactionInterceptor);
        }

        DbContext = new SqliteOrderingContext(options.AddInterceptors(new SqliteTransactionInterceptor())
            .ReplaceService<IExecutionStrategyFactory, TestExecutionStrategyFactory>().Options);
        DbContext.Database.EnsureCreated();
        DbContext.CardTypes.Add(new CardType { Id = 1, Name = "Amex" });
        DbContext.SaveChanges();

        IntegrationEvents.AddAndSaveEventAsync(default).ReturnsForAnyArgs(async call =>
        {
            var integrationEvent = call.Arg<eShop.EventBus.Events.IntegrationEvent>();
            var transaction = DbContext.GetCurrentTransaction();
            Assert.IsNotNull(transaction);
            DbContext.Add(new IntegrationEventLogEntry(integrationEvent, transaction.TransactionId));
            await DbContext.SaveChangesAsync();
            Events.Add(integrationEvent);
        });
        IntegrationEvents.PublishEventsThroughEventBusAsync(default).ReturnsForAnyArgs(call =>
        {
            Assert.IsFalse(DbContext.HasActiveTransaction, "Events must only be published after commit.");
            PublishedTransactions.Add(call.Arg<Guid>());
            return Task.CompletedTask;
        });

        Application = new OrderApplicationService(
            DbContext, new OrderRepository(DbContext), new BuyerRepository(DbContext), requestManager ?? new RequestManager(DbContext),
            IntegrationEvents,
            new CreateOrderCommandValidator(NullLogger<CreateOrderCommandValidator>.Instance),
            new CancelOrderCommandValidator(NullLogger<CancelOrderCommandValidator>.Instance),
            new ShipOrderCommandValidator(NullLogger<ShipOrderCommandValidator>.Instance),
            NullLogger<OrderApplicationService>.Instance);
    }

    public static CreateOrderCommand CreateCommand(DateTime? expiration = null) => new(
        [new BasketItem { ProductId = 1, ProductName = "Mug", UnitPrice = 10, Quantity = 2, PictureUrl = "mug.png" }],
        "buyer-id", "Buyer", "City", "Street", "State", "Country", "Zip",
        "XXXXXXXXXXXX1234", "Buyer", expiration ?? DateTime.UtcNow.AddYears(1), "123", 1);

    public async Task<Order> CreateOrderAsync()
    {
        Assert.IsTrue(await Application.CreateOrderAsync(CreateCommand(), Guid.NewGuid()));
        return await DbContext.Orders.SingleAsync();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class SqliteOrderingContext(DbContextOptions<OrderingContext> options) : OrderingContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(null);
            // SQLite uses generated integer keys rather than PostgreSQL's HiLo sequences.
            foreach (var sequence in modelBuilder.Model.GetSequences().ToArray())
            {
                modelBuilder.Model.RemoveSequence(sequence.Name, sequence.Schema);
            }
        }

    }

    private sealed class SqliteTransactionInterceptor : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            // SQLite supports serializable transactions, not the production ReadCommitted isolation level.
            return ValueTask.FromResult(InterceptionResult<DbTransaction>.SuppressWithResult(connection.BeginTransaction()));
        }
    }

    private sealed class TestExecutionStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new TestExecutionStrategy(dependencies);
    }

    private sealed class TestExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TimeoutException;
    }
}
