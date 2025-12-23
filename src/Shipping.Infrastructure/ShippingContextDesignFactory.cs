namespace eShop.Shipping.Infrastructure;

public class ShippingContextDesignFactory : IDesignTimeDbContextFactory<ShippingContext>
{
    public ShippingContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShippingContext>()
            .UseNpgsql("Host=localhost;Database=eShop.Shipping;Username=postgres;Password=postgres");

        return new ShippingContext(optionsBuilder.Options);
    }
}
