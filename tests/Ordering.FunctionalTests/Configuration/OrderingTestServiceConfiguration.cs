using eShop.EventBus.Abstractions;
using eShop.Ordering.API.Application.IntegrationEvents;
using eShop.Ordering.API.Application.Queries;
using eShop.Ordering.API.Infrastructure;
using eShop.Ordering.Infrastructure;
using eShop.Ordering.API.Infrastructure.Services;
using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.FunctionalTests.Mocks;
using eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;
using eShop.Ordering.Infrastructure.Idempotency;
using eShop.Ordering.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace eShop.Ordering.FunctionalTests.Configuration;

internal static class OrderingTestServiceConfiguration
{
    public static void ConfigureRepositoryMock(IServiceCollection services, OrderingRepositoryMockStore store)
    {
        RemoveSharedExternalServices(services);
        services.RemoveAll<IOrderRepository>();
        services.RemoveAll<IBuyerRepository>();
        services.RemoveAll<IRequestManager>();
        services.RemoveAll<IOrderQueries>();
        services.RemoveAll<DbContextOptions<OrderingContext>>();
        services.RemoveAll<OrderingContext>();

        AddInMemoryOrderingContext(services);
        services.AddSingleton(store);
        services.AddScoped<OrderingRepositoryMockUnitOfWork>();
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<IBuyerRepository, InMemoryBuyerRepository>();
        services.AddSingleton<IOrderQueries, InMemoryOrderQueries>();
        services.AddSingleton<IRequestManager, InMemoryRequestManager>();
        AddSharedTestDoubles(services);
    }

    public static void ConfigureEfCoreInMemory(IServiceCollection services)
    {
        RemoveSharedExternalServices(services);
        services.RemoveAll<DbContextOptions<OrderingContext>>();
        services.RemoveAll<OrderingContext>();

        AddInMemoryOrderingContext(services);
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
        services.RemoveAll<IOrderingIntegrationEventService>();
        services.RemoveAll<IIdentityService>();
    }

    private static void AddInMemoryOrderingContext(IServiceCollection services)
    {
        services.AddDbContext<OrderingContext>(options =>
        {
            options.UseInMemoryDatabase(InMemoryDatabaseOptions.DatabaseName, InMemoryDatabaseOptions.SharedRoot);
            options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            options.UseInternalServiceProvider(InMemoryDatabaseOptions.EfServiceProvider);
        });
    }

    private static void AddSharedTestDoubles(IServiceCollection services)
    {
        services.AddSingleton<IOrderingIntegrationEventService, NoOpOrderingIntegrationEventService>();
        services.AddSingleton<IIdentityService, MockIdentityService>();
    }
}
