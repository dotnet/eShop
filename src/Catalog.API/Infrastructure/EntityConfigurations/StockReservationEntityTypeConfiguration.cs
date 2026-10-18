namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

class StockReservationEntityTypeConfiguration
    : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservation");

        // One reservation per order, the key is what makes handling an order twice impossible
        builder.HasKey(sr => sr.OrderId);

        builder.Property(sr => sr.OrderId)
            .ValueGeneratedNever();

        builder.OwnsMany(sr => sr.Items, items => items.ToJson());
    }
}
