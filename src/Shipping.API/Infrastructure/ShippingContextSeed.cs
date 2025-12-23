namespace eShop.Shipping.API.Infrastructure;

public class ShippingContextSeed : IDbSeeder<ShippingContext>
{
    public async Task SeedAsync(ShippingContext context)
    {
        // Seed default shipper if none exists
        if (!await context.Shippers.AnyAsync())
        {
            var defaultShipper = new Shipper(
                name: "Default Shipper",
                phone: "1234567890",
                userId: "shipper-default-id",
                currentWarehouseId: 1
            );

            context.Shippers.Add(defaultShipper);
            await context.SaveChangesAsync();
        }
    }
}
