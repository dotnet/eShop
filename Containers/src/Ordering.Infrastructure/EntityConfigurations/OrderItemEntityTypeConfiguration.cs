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
            .Property("_discount")
            .HasColumnName("Discount");

        orderItemConfiguration
            .Property("_productName")
            .HasColumnName("ProductName");

        orderItemConfiguration
            .Property("_unitPrice")
            .HasColumnName("UnitPrice");

        orderItemConfiguration
            .Property("_units")
            .HasColumnName("Units");

        orderItemConfiguration
            .Property("_pictureUrl")
            .HasColumnName("PictureUrl");
    }
}
