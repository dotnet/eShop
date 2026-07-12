namespace eShop.Catalog.FunctionalTests;

public enum CatalogFunctionalTestMode
{
    Aspire,
    AspireMessagingOutbox,
    AspireMessagingRabbitMq,
    RepositoryMock,
    EfCoreInMemory,
    Testcontainers
}
