namespace eShop.Warehouse.Infrastructure.EntityConfigurations;

class WarehouseInventoryEntityTypeConfiguration : IEntityTypeConfiguration<WarehouseInventory>
{
    public void Configure(EntityTypeBuilder<WarehouseInventory> builder)
    {
        builder.ToTable("warehouse_inventory");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseHiLo("inventoryseq", "warehouse");

        builder.Property(i => i.WarehouseId)
            .IsRequired();

        builder.Property(i => i.CatalogItemId)
            .IsRequired();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.LastUpdated)
            .IsRequired();

        builder.HasIndex(i => new { i.WarehouseId, i.CatalogItemId })
            .IsUnique();

        builder.HasIndex(i => i.CatalogItemId);
    }
}
