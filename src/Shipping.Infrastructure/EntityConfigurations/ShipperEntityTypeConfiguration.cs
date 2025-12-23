namespace eShop.Shipping.Infrastructure.EntityConfigurations;

public class ShipperEntityTypeConfiguration : IEntityTypeConfiguration<Shipper>
{
    public void Configure(EntityTypeBuilder<Shipper> builder)
    {
        builder.ToTable("Shippers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .UseHiLo("shipperseq");

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.Property(s => s.IsAvailable)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();
    }
}
