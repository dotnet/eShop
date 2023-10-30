using eShop.Catalog.API.Services;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // TODO: We need to expose an API for doing this easily as part of the AddNpgsqlDbContext call,
        // see https://github.com/dotnet/aspire/issues/438. For now, we need to do this manually.
        // Order is important here as EF will ignore multiple calls to AddDbContext*.
        builder.Services.AddDbContextPool<CatalogContext>(dbContextOptionsBuilder =>
        {
            dbContextOptionsBuilder.UseNpgsql(builder =>
            {
                const int maxRetryCount = 6;

                builder.EnableRetryOnFailure(maxRetryCount);
                builder.UseVector();
            });
        });

        builder.AddNpgsqlDbContext<CatalogContext>("CatalogDB");

        // REVIEW: This is done for development ease but shouldn't be here in production
        builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

        // Add the integration services that consume the DbContext
        builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();

        builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

        builder.AddRabbitMqEventBus("EventBus")
               .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
               .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();

        builder.Services.AddOptions<CatalogOptions>()
            .BindConfiguration(nameof(CatalogOptions));

        builder.Services.AddOptions<AIOptions>()
            .BindConfiguration("AI");

        builder.Services.AddSingleton<ICatalogAI, CatalogAI>();
    }
}
