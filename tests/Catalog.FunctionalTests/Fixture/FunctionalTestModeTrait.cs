namespace eShop.Catalog.FunctionalTests;

internal static class FunctionalTestModeTrait
{
    internal const string Name = "FunctionalTestMode";

    public static string ToTraitValue(CatalogFunctionalTestMode mode) =>
        mode switch
        {
            CatalogFunctionalTestMode.RepositoryMock => "mock",
            CatalogFunctionalTestMode.EfCoreInMemory => "inmemory",
            CatalogFunctionalTestMode.Testcontainers => "testcontainers",
            CatalogFunctionalTestMode.Aspire => "aspire",
            CatalogFunctionalTestMode.AspireMessagingOutbox => "aspire-messaging-outbox",
            CatalogFunctionalTestMode.AspireMessagingRabbitMq => "aspire-messaging-rabbitmq",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public static CatalogFunctionalTestMode FromTraitValue(string value) =>
        value.ToLowerInvariant() switch
        {
            "repositorymock" or "mockrepository" or "mock" => CatalogFunctionalTestMode.RepositoryMock,
            "efcoreinmemory" or "inmemory" => CatalogFunctionalTestMode.EfCoreInMemory,
            "testcontainers" or "testcontainer" => CatalogFunctionalTestMode.Testcontainers,
            "aspire" => CatalogFunctionalTestMode.Aspire,
            "aspire-messaging-outbox" or "messaging-outbox" => CatalogFunctionalTestMode.AspireMessagingOutbox,
            "aspire-messaging-rabbitmq" or "messaging-rabbitmq" => CatalogFunctionalTestMode.AspireMessagingRabbitMq,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown functional test mode trait value.")
        };
}
