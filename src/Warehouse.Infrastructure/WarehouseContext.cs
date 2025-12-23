using WarehouseEntity = eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate.Warehouse;

namespace eShop.Warehouse.Infrastructure;

/// <remarks>
/// Add migrations using the following command inside the 'Warehouse.API' project directory:
///
/// dotnet ef migrations add --context WarehouseContext [migration-name]
/// </remarks>
public class WarehouseContext : DbContext, IUnitOfWork
{
    public DbSet<WarehouseEntity> Warehouses { get; set; }
    public DbSet<WarehouseInventory> WarehouseInventories { get; set; }

    public WarehouseContext(DbContextOptions<WarehouseContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("warehouse");
        modelBuilder.ApplyConfiguration(new EntityConfigurations.WarehouseEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EntityConfigurations.WarehouseInventoryEntityTypeConfiguration());
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }
}
