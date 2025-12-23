using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eShop.Warehouse.Infrastructure;

public class WarehouseContextDesignFactory : IDesignTimeDbContextFactory<WarehouseContext>
{
    public WarehouseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WarehouseContext>()
            .UseNpgsql("Host=localhost;Database=warehousedb;Username=postgres;Password=postgres");

        return new WarehouseContext(optionsBuilder.Options);
    }
}
