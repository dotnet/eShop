namespace eShop.Warehouse.API.Extensions;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Add the authentication services to DI
        builder.AddDefaultAuthentication();

        // Add DbContext using Aspire pattern
        builder.AddNpgsqlDbContext<WarehouseContext>("warehousedb");

        // Add migration and seed
        services.AddMigration<WarehouseContext, WarehouseContextSeed>();

        // Add repository
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();

        // Add HttpContextAccessor for identity
        services.AddHttpContextAccessor();
    }
}
