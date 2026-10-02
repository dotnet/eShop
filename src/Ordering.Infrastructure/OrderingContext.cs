using eShop.IntegrationEventLogEF;

namespace eShop.Ordering.Infrastructure;

/// <remarks>
/// Add migrations using the following command inside the 'Ordering.Infrastructure' project directory:
///
/// dotnet ef migrations add --startup-project Ordering.API --context OrderingContext [migration-name]
/// </remarks>
public class OrderingContext : DbContext, IUnitOfWork
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentMethod> Payments { get; set; }
    public DbSet<Buyer> Buyers { get; set; }
    public DbSet<CardType> CardTypes { get; set; }

    private readonly IMediator _mediator;
    private IDbContextTransaction _currentTransaction;

    public OrderingContext(DbContextOptions<OrderingContext> options) : base(options) { }

    public IDbContextTransaction GetCurrentTransaction() => _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction != null;

    public OrderingContext(DbContextOptions<OrderingContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));


        System.Diagnostics.Debug.WriteLine("OrderingContext::ctor ->" + this.GetHashCode());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.ApplyConfiguration(new ClientRequestEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentMethodEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrderEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CardTypeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BuyerEntityTypeConfiguration());
        modelBuilder.UseIntegrationEventLogs();
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch Domain Events collection.
        // Note: commands are wrapped in an explicit transaction by TransactionBehavior, so both orderings
        // below result in a single database transaction. The real trade-off is:
        //
        // A) Dispatch BEFORE SaveChanges (current approach):
        //    Domain event handlers run while changes are still pending in the ChangeTracker.
        //    Handlers may call SaveEntitiesAsync themselves, producing multiple intermediate flushes,
        //    all within the same explicit transaction. The final SaveChanges below flushes any remaining changes.
        //    If handlers depend on DB-generated IDs, they must either use client-side ID generation
        //    (for example, HiLo) or perform an intermediate SaveChanges/SaveEntitiesAsync flush first.
        //
        // B) Dispatch AFTER SaveChanges:
        //    Handlers run after the original changes have been flushed, and because they execute within the
        //    same explicit transaction they can observe that already-flushed state via SQL queries.
        //    The transaction is created with ReadCommitted isolation (see BeginTransactionAsync).
        //    DB-generated IDs are available. Handler side-effects are flushed in their own SaveChanges calls,
        //    still within the same explicit transaction opened by TransactionBehavior.
        await _mediator.DispatchDomainEventsAsync(this);

        // After executing this line all the changes (from the Command Handler and Domain Event Handlers) 
        // performed through the DbContext will be committed
        _ = await base.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null) return null;

        _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction != _currentTransaction) throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current");

        try
        {
            await SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            if (HasActiveTransaction)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public void RollbackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            if (HasActiveTransaction)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }
}

#nullable enable
