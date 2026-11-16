namespace eShop.Ordering.API.Application.IntegrationEvents.EventHandling;

public class OrderPaymentSucceededIntegrationEventHandler(
    OrderApplicationService ordering,
    ILogger<OrderPaymentSucceededIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>
{
    public async Task Handle(OrderPaymentSucceededIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var command = new SetPaidOrderStatusCommand(@event.OrderId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.OrderNumber),
            command.OrderNumber,
            command);

        await ordering.SetPaidOrderStatusAsync(command);
    }
}
