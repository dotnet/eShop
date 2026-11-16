using FluentValidation;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        
        // Add the authentication services to DI
        builder.AddDefaultAuthentication();

        services.AddDbContext<OrderingContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("orderingdb"));
        });
        builder.EnrichNpgsqlDbContext<OrderingContext>();

        services.AddMigration<OrderingContext, OrderingContextSeed>();

        // Add the integration services that consume the DbContext
        services.AddTransient<IIntegrationEventLogService>(sp => new IntegrationEventLogService<OrderingContext>(
            sp.GetRequiredService<OrderingContext>(), typeof(OrderStartedIntegrationEvent).Assembly));

        services.AddTransient<IOrderingIntegrationEventService, OrderingIntegrationEventService>();

        builder.AddRabbitMqEventBus("eventbus")
               .AddEventBusSubscriptions();

        services.AddHttpContextAccessor();
        services.AddTransient<IIdentityService, IdentityService>();

        services.AddScoped<OrderApplicationService>();

        // Register validators used explicitly by the order application service.
        services.AddValidatorsFromAssemblyContaining<CancelOrderCommandValidator>();

        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IRequestManager, RequestManager>();
    }

    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus.AddSubscription<GracePeriodConfirmedIntegrationEvent, GracePeriodConfirmedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStockConfirmedIntegrationEvent, OrderStockConfirmedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStockRejectedIntegrationEvent, OrderStockRejectedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderPaymentFailedIntegrationEvent, OrderPaymentFailedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderPaymentSucceededIntegrationEvent, OrderPaymentSucceededIntegrationEventHandler>();
    }
}
