using Microsoft.EntityFrameworkCore.Storage;

namespace eShop.Catalog.FunctionalTests.Configuration;

internal static class InMemoryDatabaseOptions
{
    internal const string DatabaseName = "CatalogFunctionalTests";

    internal static readonly InMemoryDatabaseRoot DatabaseRoot = new();
}
