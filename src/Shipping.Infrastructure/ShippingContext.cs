using eShop.IntegrationEventLogEF;

namespace eShop.Shipping.Infrastructure;

/// <remarks>
/// Add migrations using the following command inside the 'Shipping.Infrastructure' project directory:
///
/// dotnet ef migrations add --startup-project ../Shipping.API --context ShippingContext [migration-name]
/// </remarks>
public class ShippingContext : DbContext, IUnitOfWork
{
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentWaypoint> ShipmentWaypoints { get; set; }
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistory { get; set; }
    public DbSet<Shipper> Shippers { get; set; }

    private readonly IMediator _mediator;
    private IDbContextTransaction? _currentTransaction;

    public ShippingContext(DbContextOptions<ShippingContext> options) : base(options)
    {
        _mediator = null!;
    }

    public IDbContextTransaction? GetCurrentTransaction() => _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction != null;

    public ShippingContext(DbContextOptions<ShippingContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shipping");
        modelBuilder.ApplyConfiguration(new ShipmentEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentWaypointEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ShipmentStatusHistoryEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ShipperEntityTypeConfiguration());
        modelBuilder.UseIntegrationEventLogs();
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _mediator.DispatchDomainEventsAsync(this);
        _ = await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null) return null!;

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
                _currentTransaction?.Dispose();
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
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }
    }
}
