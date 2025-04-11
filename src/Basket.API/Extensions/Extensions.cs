using System.Text.Json.Serialization;
using Inked.Basket.API.IntegrationEvents.EventHandling;
using Inked.Basket.API.IntegrationEvents.EventHandling.Events;
using Inked.Basket.API.Repositories;

namespace Inked.Basket.API.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultAuthentication();

        builder.AddRedisClient("redis");

        builder.Services.AddSingleton<IBasketRepository, RedisBasketRepository>();

        builder.AddRabbitMqEventBus("eventbus")
            .AddSubscription<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>()
            .ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventContext.Default));
    }
}

[JsonSerializable(typeof(OrderStartedIntegrationEvent))]
internal partial class IntegrationEventContext : JsonSerializerContext
{
}
