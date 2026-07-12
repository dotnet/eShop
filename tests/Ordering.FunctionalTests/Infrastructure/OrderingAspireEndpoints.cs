namespace eShop.Ordering.FunctionalTests.Infrastructure;

internal sealed record OrderingAspireEndpoints(
    string PostgresConnectionString,
    string? EventBusConnectionString);
