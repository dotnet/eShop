// File: src/Ordering.Api/EventHandlers/RefundDomainEventHandler.cs

namespace Inked.Ordering.API.Application.IntegrationEvents.EventHandling;

public class OrderRefundedIntegrationEventHandler(
    IMediator mediator,
    ILogger<OrderRefundedIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderRefundedIntegrationEvent>
{
    public async Task Handle(OrderRefundedIntegrationEvent @event)
    {
        logger.LogInformation("Handling RefundIntegrationEvent for order {OrderId} and buyer {BuyerId}.",
            @event.OrderId, @event.BuyerId);
        var command = new SetPaidOrderStatusCommand(@event.OrderId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.OrderNumber),
            command.OrderNumber,
            command);

        await mediator.Send(command);
    }
}
