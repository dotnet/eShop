using Inked.EventBus.Abstractions;

namespace Inked.WebApp.Services.OrderStatus.IntegrationEvents;

public class OrderStatusChangedToAwaitingValidationIntegrationEventHandler(
    OrderStatusNotificationService orderStatusNotificationService,
    ILogger<OrderStatusChangedToAwaitingValidationIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStatusChangedToAwaitingValidationIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToAwaitingValidationIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id,
            @event);
        await orderStatusNotificationService.NotifyOrderStatusChangedAsync(@event.BuyerIdentityGuid);
    }
}
