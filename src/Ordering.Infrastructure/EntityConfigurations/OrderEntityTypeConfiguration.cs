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
            .Property(o => o.BuyerId);
            
        orderConfiguration
            .Property(o => o.OrderDate);

        orderConfiguration
            .Property(o => o.OrderStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        orderConfiguration
            .Property(o => o.PaymentId)
            .HasColumnName("PaymentMethodId");

        orderConfiguration
            .Property(o => o.Description);

        orderConfiguration.HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(o => o.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        orderConfiguration.HasOne(o => o.Buyer)
            .WithMany()
            .HasForeignKey(o => o.BuyerId);
    }
}
