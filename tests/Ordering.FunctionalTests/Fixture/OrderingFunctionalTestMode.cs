namespace eShop.Ordering.FunctionalTests;

public enum OrderingFunctionalTestMode
{
    Aspire,
    AspireMessagingOutbox,
    AspireMessagingRabbitMq,
    RepositoryMock,
    EfCoreInMemory,
    Testcontainers
}
