using eShop.EventBus.Extensions;

using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;

using Microsoft.Extensions.DependencyInjection;

namespace eShop.Testing.Common.Messaging;

public static class MessagingTestServiceCollectionExtensions
{
    public static IServiceCollection AddCaptureSubscription<TIntegrationEvent>(this IServiceCollection services)
        where TIntegrationEvent : IntegrationEvent
    {
        services.AddKeyedTransient<IIntegrationEventHandler, IntegrationEventCaptureHandler<TIntegrationEvent>>(typeof(TIntegrationEvent));
        services.Configure<EventBusSubscriptionInfo>(options => options.EventTypes[typeof(TIntegrationEvent).Name] = typeof(TIntegrationEvent));
        return services;
    }
}
