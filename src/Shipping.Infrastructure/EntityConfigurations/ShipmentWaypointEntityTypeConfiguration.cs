namespace eShop.Shipping.Infrastructure.EntityConfigurations;

public class ShipmentWaypointEntityTypeConfiguration : IEntityTypeConfiguration<ShipmentWaypoint>
{
    public void Configure(EntityTypeBuilder<ShipmentWaypoint> builder)
    {
        builder.ToTable("ShipmentWaypoints");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .UseHiLo("shipmentwaypointseq");

        builder.Property(w => w.ShipmentId)
            .IsRequired();

        builder.Property(w => w.WarehouseId)
            .IsRequired();

        builder.Property(w => w.WarehouseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Sequence)
            .IsRequired();

        builder.HasOne<Shipment>()
            .WithMany(s => s.Waypoints)
            .HasForeignKey(w => w.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.ShipmentId, w.Sequence })
            .IsUnique();
    }
}
