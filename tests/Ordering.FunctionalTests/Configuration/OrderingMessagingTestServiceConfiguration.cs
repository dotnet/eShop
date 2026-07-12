using eShop.EventBus.Abstractions;
using eShop.Ordering.API.Application.IntegrationEvents.Events;
using eShop.Ordering.API.Infrastructure.Services;
using eShop.Ordering.FunctionalTests.Mocks;

using eShop.Testing.Common.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace eShop.Ordering.FunctionalTests.Configuration;

internal static class OrderingMessagingTestServiceConfiguration
{
    public static void ConfigureOutboxWithSpyBus(IServiceCollection services)
    {
        RemoveRabbitMqInfrastructure(services);

        services.AddSingleton<CapturingEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<CapturingEventBus>());
        services.RemoveAll<IIdentityService>();
        services.AddSingleton<IIdentityService, MockIdentityService>();
    }

    public static void ConfigureRabbitMqWithCaptureHandlers(IServiceCollection services)
    {
        IntegrationEventCapture.Reset();

        services.RemoveAll<IIdentityService>();
        services.AddSingleton<IIdentityService, MockIdentityService>();

        services
            .AddCaptureSubscription<OrderStartedIntegrationEvent>()
            .AddCaptureSubscription<OrderStatusChangedToSubmittedIntegrationEvent>();
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
