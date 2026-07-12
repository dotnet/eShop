using eShop.Catalog.API.IntegrationEvents.Events;
using eShop.EventBus.Abstractions;
using eShop.Catalog.API.Services;
using eShop.Catalog.FunctionalTests.Mocks;

using eShop.Testing.Common.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace eShop.Catalog.FunctionalTests.Configuration;

internal static class CatalogMessagingTestServiceConfiguration
{
    public static void ConfigureOutboxWithSpyBus(IServiceCollection services)
    {
        RemoveRabbitMqInfrastructure(services);

        services.AddSingleton<CapturingEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<CapturingEventBus>());
        services.RemoveAll<ICatalogAI>();
        services.AddSingleton<ICatalogAI, FakeCatalogAI>();
    }

    public static void ConfigureRabbitMqWithCaptureHandlers(IServiceCollection services)
    {
        IntegrationEventCapture.Reset();

        services.RemoveAll<ICatalogAI>();
        services.AddSingleton<ICatalogAI, FakeCatalogAI>();

        services.AddCaptureSubscription<ProductPriceChangedIntegrationEvent>();
    }

    private static void RemoveRabbitMqInfrastructure(IServiceCollection services)
    {
        services.RemoveAll<IEventBus>();

        var hostedServices = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .ToList();

        foreach (var descriptor in hostedServices)
        {
            services.Remove(descriptor);
        }
    }
}
