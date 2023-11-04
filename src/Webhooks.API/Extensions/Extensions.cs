internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultAuthentication();

        builder.AddRabbitMqEventBus("EventBus")
               .AddEventBusSubscriptions();

        builder.AddNpgsqlDbContext<WebhooksContext>("WebHooksDB");

        builder.Services.AddMigration<WebhooksContext>();

        builder.Services.AddTransient<IGrantUrlTesterService, GrantUrlTesterService>();
        builder.Services.AddTransient<IWebhooksRetriever, WebhooksRetriever>();
        builder.Services.AddTransient<IWebhooksSender, WebhooksSender>();
    }

    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus.AddSubscription<ProductPriceChangedIntegrationEvent, ProductPriceChangedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStatusChangedToShippedIntegrationEvent, OrderStatusChangedToShippedIntegrationEventHandler>();
        eventBus.AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();
    }
}
