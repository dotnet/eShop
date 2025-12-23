namespace eShop.Ordering.API.Application.IntegrationEvents.EventHandling;

public class ShipmentCompletedIntegrationEventHandler(
    IMediator mediator,
    ILogger<ShipmentCompletedIntegrationEventHandler> logger) :
    IIntegrationEventHandler<ShipmentCompletedIntegrationEvent>
{
    public async Task Handle(ShipmentCompletedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var command = new ShipOrderCommand(@event.OrderId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.OrderNumber),
            command.OrderNumber,
            command);

        await mediator.Send(command);
    }
}
