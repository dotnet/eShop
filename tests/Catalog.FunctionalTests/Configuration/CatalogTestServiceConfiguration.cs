using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Infrastructure.Repositories;
using eShop.Catalog.API.IntegrationEvents;
using eShop.Catalog.API.Services;
using eShop.Catalog.FunctionalTests.Configuration;
using eShop.Catalog.FunctionalTests.Mocks;
using eShop.Catalog.FunctionalTests.Mocks.RepositoryMock;
using eShop.EventBus.Abstractions;
using eShop.IntegrationEventLogEF.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace eShop.Catalog.FunctionalTests.Configuration;

internal static class CatalogTestServiceConfiguration
{
    public static void ConfigureRepositoryMock(IServiceCollection services, CatalogRepositoryMockStore store)
    {
        RemoveSharedExternalServices(services);
        services.RemoveAll<ICatalogRepository>();
        services.RemoveAll<CatalogContext>();
        services.RemoveAll<DbContextOptions<CatalogContext>>();

        services.AddSingleton(store);
        services.AddSingleton<ICatalogRepository, InMemoryCatalogRepository>();
        AddSharedTestDoubles(services);
    }

    public static void ConfigureEfCoreInMemory(IServiceCollection services)
    {
        RemoveSharedExternalServices(services);
        services.RemoveAll<ICatalogRepository>();
        services.RemoveAll<CatalogContext>();
        services.RemoveAll<InMemoryCatalogContext>();
        services.RemoveAll<DbContextOptions<CatalogContext>>();
        services.RemoveAll<DbContextOptions<InMemoryCatalogContext>>();

        services.AddDbContext<InMemoryCatalogContext>(options =>
            options.UseInMemoryDatabase(InMemoryDatabaseOptions.DatabaseName, InMemoryDatabaseOptions.DatabaseRoot));
        services.AddScoped<CatalogContext>(sp => sp.GetRequiredService<InMemoryCatalogContext>());
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        AddSharedTestDoubles(services);
    }

    public static void ConfigureSharedExternalDependencies(IServiceCollection services)
    {
        RemoveSharedExternalServices(services);
        AddSharedTestDoubles(services);
    }

    private static void RemoveSharedExternalServices(IServiceCollection services)
    {
        services.RemoveAll<IHostedService>();
        services.RemoveAll<IEventBus>();
        services.RemoveAll<ICatalogAI>();
        services.RemoveAll<ICatalogIntegrationEventService>();
        services.RemoveAll<IIntegrationEventLogService>();
    }

    private static void AddSharedTestDoubles(IServiceCollection services)
    {
        services.AddSingleton<ICatalogAI, FakeCatalogAI>();
        services.AddTransient<ICatalogIntegrationEventService, NoOpCatalogIntegrationEventService>();
        services.AddSingleton<IIntegrationEventLogService, NoOpIntegrationEventLogService>();
    }
}
