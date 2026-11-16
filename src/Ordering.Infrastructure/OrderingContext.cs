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

    private IDbContextTransaction _currentTransaction;

    public OrderingContext(DbContextOptions<OrderingContext> options) : base(options) { }

    public IDbContextTransaction GetCurrentTransaction() => _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction != null;

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
