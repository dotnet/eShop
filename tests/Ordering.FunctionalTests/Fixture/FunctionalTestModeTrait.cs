namespace eShop.Ordering.FunctionalTests;

internal static class FunctionalTestModeTrait
{
    internal const string Name = "FunctionalTestMode";

    public static string ToTraitValue(OrderingFunctionalTestMode mode) =>
        mode switch
        {
            OrderingFunctionalTestMode.RepositoryMock => "mock",
            OrderingFunctionalTestMode.EfCoreInMemory => "inmemory",
            OrderingFunctionalTestMode.Testcontainers => "testcontainers",
            OrderingFunctionalTestMode.Aspire => "aspire",
            OrderingFunctionalTestMode.AspireMessagingOutbox => "aspire-messaging-outbox",
            OrderingFunctionalTestMode.AspireMessagingRabbitMq => "aspire-messaging-rabbitmq",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public static OrderingFunctionalTestMode FromTraitValue(string value) =>
        value.ToLowerInvariant() switch
        {
            "repositorymock" or "mockrepository" or "mock" => OrderingFunctionalTestMode.RepositoryMock,
            "efcoreinmemory" or "inmemory" => OrderingFunctionalTestMode.EfCoreInMemory,
            "testcontainers" or "testcontainer" => OrderingFunctionalTestMode.Testcontainers,
            "aspire" => OrderingFunctionalTestMode.Aspire,
            "aspire-messaging-outbox" or "messaging-outbox" => OrderingFunctionalTestMode.AspireMessagingOutbox,
            "aspire-messaging-rabbitmq" or "messaging-rabbitmq" => OrderingFunctionalTestMode.AspireMessagingRabbitMq,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown functional test mode trait value.")
        };
}
