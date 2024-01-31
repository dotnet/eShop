namespace eShop.Ordering.Infrastructure.EntityConfigurations;

class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> orderConfiguration)
    {
        orderConfiguration.ToTable("orders");

        orderConfiguration.Ignore(b => b.DomainEvents);

        orderConfiguration.Property(o => o.Id)
            .UseHiLo("orderseq");

        //Address value object persisted as owned entity type supported since EF Core 2.0
        orderConfiguration
            .OwnsOne(o => o.Address);

        orderConfiguration
            .Property("_buyerId")
            .HasColumnName("BuyerId");

        orderConfiguration
            .Property("_orderDate")
            .HasColumnName("OrderDate");

        orderConfiguration
            .Property(o => o.OrderStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        orderConfiguration
            .Property("_paymentMethodId")
            .HasColumnName("PaymentMethodId");

        orderConfiguration.Property<string>("Description");

        orderConfiguration.HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey("_paymentMethodId")
            .OnDelete(DeleteBehavior.Restrict);

        orderConfiguration.HasOne<Buyer>()
            .WithMany()
            .HasForeignKey("_buyerId");
    }
}
