using eShop.Warehouse.Domain.SeedWork;
using WarehouseEntity = eShop.Warehouse.Domain.AggregatesModel.WarehouseAggregate.Warehouse;

namespace eShop.Warehouse.API.Infrastructure;

public class WarehouseContextSeed(
    ILogger<WarehouseContextSeed> logger) : IDbSeeder<WarehouseContext>
{
    public async Task SeedAsync(WarehouseContext context)
    {
        if (!await context.Warehouses.AnyAsync())
        {
            logger.LogInformation("Seeding warehouse data...");

            // Seed warehouses with geographic coordinates for shipping optimization
            var warehouses = new List<WarehouseEntity>
            {
                new("Seattle Main Warehouse", "123 Warehouse Blvd", "Seattle", "USA", 47.6062, -122.3321),
                new("Los Angeles Distribution Hub", "456 Distribution Ave", "Los Angeles", "USA", 34.0522, -118.2437),
                new("New York East Warehouse", "789 Logistics Way", "New York", "USA", 40.7128, -74.0060),
                new("Chicago Central Depot", "321 Supply Chain Rd", "Chicago", "USA", 41.8781, -87.6298)
            };

            context.Warehouses.AddRange(warehouses);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} warehouses", warehouses.Count);

            // Reload warehouses to get IDs
            warehouses = await context.Warehouses.ToListAsync();

            // Seed inventory for first 20 catalog items across warehouses
            var random = new Random(42); // Fixed seed for reproducibility
            var inventoryItems = new List<WarehouseInventory>();

            foreach (var warehouse in warehouses)
            {
                for (int catalogItemId = 1; catalogItemId <= 20; catalogItemId++)
                {
                    var quantity = random.Next(10, 200);
                    inventoryItems.Add(new WarehouseInventory(warehouse.Id, catalogItemId, quantity));
                }
            }

            context.WarehouseInventories.AddRange(inventoryItems);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} inventory items across {WarehouseCount} warehouses",
                inventoryItems.Count, warehouses.Count);
        }
        else
        {
            logger.LogInformation("Warehouse data already exists, skipping seed");
        }
    }
}
