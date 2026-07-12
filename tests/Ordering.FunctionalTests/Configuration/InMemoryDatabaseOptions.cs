using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Ordering.FunctionalTests.Configuration;

internal static class InMemoryDatabaseOptions
{
    internal const string DatabaseName = "OrderingFunctionalTests";

    internal static readonly InMemoryDatabaseRoot SharedRoot = new();

    internal static readonly IServiceProvider EfServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();
}
