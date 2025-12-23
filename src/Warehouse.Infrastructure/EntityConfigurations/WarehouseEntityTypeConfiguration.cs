using WarehouseEntity = eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate.Warehouse;

namespace eShop.Warehouse.Infrastructure.EntityConfigurations;

class WarehouseEntityTypeConfiguration : IEntityTypeConfiguration<WarehouseEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseEntity> builder)
    {
        builder.ToTable("warehouses");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).UseHiLo("warehouseseq", "warehouse");

        builder.Property(w => w.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(w => w.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Latitude)
            .IsRequired();

        builder.Property(w => w.Longitude)
            .IsRequired();

        builder.Property(w => w.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasMany(w => w.Inventory)
            .WithOne()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        var navigation = builder.Metadata.FindNavigation(nameof(WarehouseEntity.Inventory));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
