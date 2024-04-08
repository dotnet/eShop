using eShop.Catalog.API.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Memory;

public static class Extensions
{
    private const int VectorSize = 1536;

    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<CatalogContext>("catalogdb");

        // REVIEW: This is done for development ease but shouldn't be here in production
        builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

        // Add the integration services that consume the DbContext
        builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();

        builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

        builder.AddRabbitMqEventBus("eventbus")
               .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
               .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();

        builder.Services.AddOptions<CatalogOptions>()
            .BindConfiguration(nameof(CatalogOptions));

        builder.Services.AddOptions<AIOptions>()
            .BindConfiguration("AI");
        var openAIOptions = builder.Configuration.GetSection("AI").Get<AIOptions>()?.OpenAI;

        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("embedding")) && !string.IsNullOrEmpty(openAIOptions.EmbeddingName))
        {
            builder.AddKeyedNpgsqlDataSource("catalogdb", null, builder => builder.UseVector());
            builder.AddAzureOpenAIClient("embedding");
            builder.Services.AddAzureOpenAITextEmbeddingGeneration(openAIOptions.EmbeddingName);
            builder.Services.AddSingleton<IMemoryStore, PostgresMemoryStore>(provider =>
            {
                var dataSource = provider.GetRequiredKeyedService<NpgsqlDataSource>("catalogdb");

                return new(dataSource, VectorSize);
            });
            builder.Services.AddSingleton<ISemanticTextMemory, SemanticTextMemory>();
        }

        builder.Services.AddSingleton<ICatalogAI, CatalogAI>();
    }
}
