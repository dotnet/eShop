namespace eShop.Ordering.Infrastructure.EntityConfigurations;

class OrderItemEntityTypeConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> orderItemConfiguration)
    {
        orderItemConfiguration.ToTable("orderItems");

        orderItemConfiguration.Ignore(b => b.DomainEvents);

        orderItemConfiguration.Property(o => o.Id)
            .UseHiLo("orderitemseq");

        orderItemConfiguration.Property<int>("OrderId");

        orderItemConfiguration
            .Property(o => o.Discount);

        orderItemConfiguration
            .Property(o => o.ProductName);

        orderItemConfiguration
            .Property(o => o.UnitPrice);

        orderItemConfiguration
            .Property(o => o.Units);

        orderItemConfiguration
            .Property(o => o.PictureUrl);
    }
}
