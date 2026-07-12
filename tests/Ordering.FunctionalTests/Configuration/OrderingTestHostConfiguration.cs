using eShop.Testing.Common;

using Microsoft.Extensions.Configuration;

namespace eShop.Ordering.FunctionalTests.Configuration;

internal sealed class OrderingTestHostConfigurationOptions
{
    public required OrderingFunctionalTestMode Mode { get; init; }
    public string PostgresConnectionString { get; init; } = string.Empty;
    public string? PostgresResourceName { get; init; }
    public string? IdentityApiUrl { get; init; }
    public string? EventBusConnectionString { get; init; }
    public string? EventBusSubscriptionClientName { get; init; }
}

internal static class OrderingTestHostConfiguration
{
    public static void Apply(IConfigurationBuilder config, OrderingTestHostConfigurationOptions options)
    {
        var settings = new Dictionary<string, string?>(TestLogging.DefaultConfiguration)
        {
            ["Identity:Url"] = options.IdentityApiUrl ?? "http://localhost/identity",
            ["Identity:Audience"] = "orders"
        };

        switch (options.Mode)
        {
            case OrderingFunctionalTestMode.Aspire
            or OrderingFunctionalTestMode.AspireMessagingOutbox
            or OrderingFunctionalTestMode.AspireMessagingRabbitMq when options.PostgresResourceName is not null:
                settings[$"ConnectionStrings:{options.PostgresResourceName}"] = options.PostgresConnectionString;
                break;
            case OrderingFunctionalTestMode.Testcontainers:
                settings["ConnectionStrings:orderingdb"] = options.PostgresConnectionString;
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
