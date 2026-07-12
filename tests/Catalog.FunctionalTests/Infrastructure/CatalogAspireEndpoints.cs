namespace eShop.Catalog.FunctionalTests.Infrastructure;

internal sealed record CatalogAspireEndpoints(
    string PostgresConnectionString,
    string? EventBusConnectionString);
