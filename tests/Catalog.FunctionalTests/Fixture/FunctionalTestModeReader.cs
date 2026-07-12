namespace eShop.Catalog.FunctionalTests;

internal static class FunctionalTestModeReader
{
    internal const string EnvironmentVariableName = "ESHOP_CATALOG_FUNCTIONAL_TEST_MODE";

    public static CatalogFunctionalTestMode? ReadOverrideFromEnvironment() =>
        ReadOverrideFromEnvironment(EnvironmentVariableName);

    public static CatalogFunctionalTestMode? ReadOverrideFromEnvironment(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? null : Parse(value);
    }

    public static CatalogFunctionalTestMode ReadFromEnvironment() =>
        ReadOverrideFromEnvironment() ?? CatalogFunctionalTestMode.Aspire;

    public static CatalogFunctionalTestMode ReadFromEnvironment(string variableName) =>
        ReadOverrideFromEnvironment(variableName) ?? CatalogFunctionalTestMode.Aspire;

    private static CatalogFunctionalTestMode Parse(string value) =>
        value.ToLowerInvariant() switch
        {
            "repositorymock" or "mockrepository" or "mock" => CatalogFunctionalTestMode.RepositoryMock,
            "efcoreinmemory" or "inmemory" => CatalogFunctionalTestMode.EfCoreInMemory,
            "testcontainers" or "testcontainer" => CatalogFunctionalTestMode.Testcontainers,
            "aspire" => CatalogFunctionalTestMode.Aspire,
            "aspiremessagingoutbox" or "aspire-messaging-outbox" or "messaging-outbox" => CatalogFunctionalTestMode.AspireMessagingOutbox,
            "aspiremessagingrabbitmq" or "aspire-messaging-rabbitmq" or "messaging-rabbitmq" => CatalogFunctionalTestMode.AspireMessagingRabbitMq,
            _ => throw new InvalidOperationException(
                $"Unknown functional test mode '{value}' in environment variable '{EnvironmentVariableName}'.")
        };
}
