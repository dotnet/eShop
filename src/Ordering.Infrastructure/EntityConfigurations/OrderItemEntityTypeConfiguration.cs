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
            .Property(o => o.Discount)
            .HasColumnName("Discount");

        orderItemConfiguration
            .Property(o => o.ProductName)
            .HasColumnName("ProductName");

        orderItemConfiguration
            .Property(o => o.UnitPrice)
            .HasColumnName("UnitPrice");

        orderItemConfiguration
            .Property(o => o.Units)
            .HasColumnName("Units");

        orderItemConfiguration
            .Property(o => o.PictureUrl)
            .HasColumnName("PictureUrl");
    }
}
