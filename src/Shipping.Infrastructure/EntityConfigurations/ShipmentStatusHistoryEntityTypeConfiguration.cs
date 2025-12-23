namespace eShop.Shipping.Infrastructure.EntityConfigurations;

public class ShipmentStatusHistoryEntityTypeConfiguration : IEntityTypeConfiguration<ShipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
    {
        builder.ToTable("ShipmentStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .UseHiLo("shipmentstatushistoryseq");

        builder.Property(h => h.ShipmentId)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(h => h.Timestamp)
            .IsRequired();

        builder.Property(h => h.Notes)
            .HasMaxLength(500);

        builder.HasOne<Shipment>()
            .WithMany(s => s.StatusHistory)
            .HasForeignKey(h => h.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShipmentWaypoint>()
            .WithMany()
            .HasForeignKey(h => h.WaypointId)
            .IsRequired(false);
    }
}
