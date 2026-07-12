namespace eShop.Ordering.FunctionalTests;

internal static class FunctionalTestModeReader
{
    internal const string EnvironmentVariableName = "ESHOP_ORDERING_FUNCTIONAL_TEST_MODE";

    public static OrderingFunctionalTestMode? ReadOverrideFromEnvironment() =>
        ReadOverrideFromEnvironment(EnvironmentVariableName);

    public static OrderingFunctionalTestMode? ReadOverrideFromEnvironment(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? null : Parse(value);
    }

    public static OrderingFunctionalTestMode ReadFromEnvironment() =>
        ReadOverrideFromEnvironment() ?? OrderingFunctionalTestMode.Aspire;

    public static OrderingFunctionalTestMode ReadFromEnvironment(string variableName) =>
        ReadOverrideFromEnvironment(variableName) ?? OrderingFunctionalTestMode.Aspire;

    private static OrderingFunctionalTestMode Parse(string value) =>
        value.ToLowerInvariant() switch
        {
            "repositorymock" or "mockrepository" or "mock" => OrderingFunctionalTestMode.RepositoryMock,
            "efcoreinmemory" or "inmemory" => OrderingFunctionalTestMode.EfCoreInMemory,
            "testcontainers" or "testcontainer" => OrderingFunctionalTestMode.Testcontainers,
            "aspire" => OrderingFunctionalTestMode.Aspire,
            "aspiremessagingoutbox" or "aspire-messaging-outbox" or "messaging-outbox" => OrderingFunctionalTestMode.AspireMessagingOutbox,
            "aspiremessagingrabbitmq" or "aspire-messaging-rabbitmq" or "messaging-rabbitmq" => OrderingFunctionalTestMode.AspireMessagingRabbitMq,
            _ => throw new InvalidOperationException(
                $"Unknown functional test mode '{value}' in environment variable '{EnvironmentVariableName}'.")
        };
}
