namespace eShop.Shipping.API.Extensions;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Add the authentication services to DI
        builder.AddDefaultAuthentication();

        // Add DbContext
        services.AddDbContext<ShippingContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("shippingdb"));
        });
        builder.EnrichNpgsqlDbContext<ShippingContext>();

        services.AddMigration<ShippingContext, ShippingContextSeed>();

        // Add the integration services that consume the DbContext
        services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<ShippingContext>>();
        services.AddTransient<IShippingIntegrationEventService, ShippingIntegrationEventService>();

        builder.AddRabbitMqEventBus("eventbus")
               .AddEventBusSubscriptions();

        services.AddHttpContextAccessor();
        services.AddTransient<IIdentityService, IdentityService>();

        // Configure MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(Program));
        });

        // Register repositories
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShipperRepository, ShipperRepository>();
    }

    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus.AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();
        eventBus.AddSubscription<OrderCancelledIntegrationEvent, OrderCancelledIntegrationEventHandler>();
    }
}
