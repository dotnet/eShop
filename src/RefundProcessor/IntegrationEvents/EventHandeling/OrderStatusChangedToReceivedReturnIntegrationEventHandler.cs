// File: src/RefundProcessor/IntegrationEvents/Handlers/OrderStatusChangedToReceivedReturnIntegrationEventHandler.cs

using Inked.Ordering.API.Application.IntegrationEvents.Events;

namespace Inked.RefundProcessor.IntegrationEvents.EventHandeling;

public class
    OrderStatusChangedToReceivedReturnIntegrationEventHandler : IIntegrationEventHandler<
    OrderStatusChangedToReceivedReturnIntegrationEvent>
{
    private readonly ILogger<OrderStatusChangedToReceivedReturnIntegrationEventHandler> _logger;
    private readonly IRefundService _refundService;

    public OrderStatusChangedToReceivedReturnIntegrationEventHandler(
        IRefundService refundService,
        ILogger<OrderStatusChangedToReceivedReturnIntegrationEventHandler> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    public async Task Handle(OrderStatusChangedToReceivedReturnIntegrationEvent @event)
    {
        _logger.LogInformation("Processing refund for order {OrderId}", @event.OrderId);

        await _refundService.ProcessRefund(@event.OrderId, @event.BuyerId);

        _logger.LogInformation("Order {OrderId} marked as refunded.", @event.OrderId);
    }
}
