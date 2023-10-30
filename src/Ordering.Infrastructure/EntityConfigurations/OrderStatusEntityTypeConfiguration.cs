namespace eShop.Ordering.Infrastructure.EntityConfigurations;

class OrderStatusEntityTypeConfiguration
    : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> orderStatusConfiguration)
    {
        orderStatusConfiguration.ToTable("orderstatus");

        orderStatusConfiguration.Property(o => o.Id)
            .ValueGeneratedNever();

        orderStatusConfiguration.Property(o => o.Name)
            .HasMaxLength(200);
    }
}
