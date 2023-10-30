using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventBusBuilderExtensions
{
    public static IEventBusBuilder ConfigureJsonOptions(this IEventBusBuilder eventBusBuilder, Action<JsonSerializerOptions> configure)
    {
        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            configure(o.JsonSerializerOptions);
        });

        return eventBusBuilder;
    }

    public static IEventBusBuilder AddSubscription<T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TH>(this IEventBusBuilder eventBusBuilder)
        where T : IntegrationEvent
        where TH : class, IIntegrationEventHandler<T>
    {
        // Use keyed services to register multiple handlers for the same event type
        // the consumer can use IKeyedServiceProvider.GetKeyedService<IIntegrationEventHandler>(typeof(T)) to get all
        // handlers for the event type.

        // MediatR assembly scanning breaks with keyed services see https://github.com/jbogard/MediatR/issues/942.
        // eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T));

        // Instead, we'll register the handler as transient and use the HandlerTypes on SubscriptionInfo to get all
        // handlers for the event type.
        eventBusBuilder.Services.AddTransient<TH>();

        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            // Keep track of all registered event types and their name mapping. We send these event types over the message bus
            // and we don't want to do Type.GetType, so we keep track of the name mapping here.

            // This list will also be used to subscribe to events from the underlying message broker implementation.
            o.EventTypes[typeof(T).Name] = typeof(T);

            // Handle the case where the same handler is registered twice for the same event type
            if (o.HandlerTypes.TryGetValue(typeof(T), out var handlerTypes))
            {
                if (!handlerTypes.Add(typeof(TH)))
                {
                    throw new InvalidOperationException($"Handler Type {typeof(TH).GetGenericTypeName()} already registered for '{typeof(T)}'");
                }
            }
            else
            {
                o.HandlerTypes[typeof(T)] = [typeof(TH)];
            }
        });

        return eventBusBuilder;
    }
}
