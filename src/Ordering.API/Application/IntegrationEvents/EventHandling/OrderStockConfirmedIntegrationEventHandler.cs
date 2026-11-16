namespace eShop.Ordering.API.Application.IntegrationEvents.EventHandling;

public class OrderStockConfirmedIntegrationEventHandler(
    ICommandHandler<SetStockConfirmedOrderStatusCommand, bool> commandHandler,
    ILogger<OrderStockConfirmedIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderStockConfirmedIntegrationEvent>
{
    public async Task Handle(OrderStockConfirmedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var command = new SetStockConfirmedOrderStatusCommand(@event.OrderId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.OrderNumber),
            command.OrderNumber,
            command);

        await commandHandler.Handle(command);
    }
}
