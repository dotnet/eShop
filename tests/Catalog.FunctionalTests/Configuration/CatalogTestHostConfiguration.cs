using eShop.Testing.Common;

using Microsoft.Extensions.Configuration;

namespace eShop.Catalog.FunctionalTests.Configuration;

internal sealed class CatalogTestHostConfigurationOptions
{
    public required CatalogFunctionalTestMode Mode { get; init; }
    public string PostgresConnectionString { get; init; } = string.Empty;
    public string? PostgresResourceName { get; init; }
    public string? EventBusConnectionString { get; init; }
    public string? EventBusSubscriptionClientName { get; init; }
}

internal static class CatalogTestHostConfiguration
{
    public static void Apply(IConfigurationBuilder config, CatalogTestHostConfigurationOptions options)
    {
        var settings = new Dictionary<string, string?>(TestLogging.DefaultConfiguration);

        switch (options.Mode)
        {
            case CatalogFunctionalTestMode.Aspire
            or CatalogFunctionalTestMode.AspireMessagingOutbox
            or CatalogFunctionalTestMode.AspireMessagingRabbitMq when options.PostgresResourceName is not null:
                settings[$"ConnectionStrings:{options.PostgresResourceName}"] = options.PostgresConnectionString;
                break;
            case CatalogFunctionalTestMode.Testcontainers:
                settings["ConnectionStrings:catalogdb"] = options.PostgresConnectionString;
                break;
        }

        if (!string.IsNullOrWhiteSpace(options.EventBusConnectionString))
        {
            settings["ConnectionStrings:EventBus"] = options.EventBusConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(options.EventBusSubscriptionClientName))
        {
            settings["EventBus:SubscriptionClientName"] = options.EventBusSubscriptionClientName;
        }

        config.AddInMemoryCollection(settings);
    }
}
