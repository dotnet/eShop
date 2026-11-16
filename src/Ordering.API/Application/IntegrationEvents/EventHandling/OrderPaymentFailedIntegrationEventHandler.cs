namespace eShop.Ordering.API.Application.IntegrationEvents.EventHandling;

public class OrderPaymentFailedIntegrationEventHandler(
    OrderApplicationService ordering,
    ILogger<OrderPaymentFailedIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>
{
    public async Task Handle(OrderPaymentFailedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var command = new CancelOrderCommand(@event.OrderId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.OrderNumber),
            command.OrderNumber,
            command);

        await ordering.CancelOrderAsync(command);
    }
}
