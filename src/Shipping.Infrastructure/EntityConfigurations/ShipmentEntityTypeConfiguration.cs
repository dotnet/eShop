namespace eShop.Shipping.Infrastructure.EntityConfigurations;

public class ShipmentEntityTypeConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .UseHiLo("shipmentseq");

        builder.Property(s => s.OrderId)
            .IsRequired();

        builder.HasIndex(s => s.OrderId)
            .IsUnique();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.CustomerAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.CustomerCity)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.CustomerCountry)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasOne<Shipper>()
            .WithMany()
            .HasForeignKey(s => s.ShipperId)
            .IsRequired(false);

        var waypointsNavigation = builder.Metadata.FindNavigation(nameof(Shipment.Waypoints));
        waypointsNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        var statusHistoryNavigation = builder.Metadata.FindNavigation(nameof(Shipment.StatusHistory));
        statusHistoryNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
