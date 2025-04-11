using Inked.Basket.API.IntegrationEvents.EventHandling.Events;
using Inked.Basket.API.Repositories;

namespace Inked.Basket.API.IntegrationEvents.EventHandling;

public class OrderStartedIntegrationEventHandler(
    IBasketRepository repository,
    ILogger<OrderStartedIntegrationEventHandler> logger) : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id,
            @event);

        await repository.DeleteBasketAsync(@event.UserId);
    }
}
