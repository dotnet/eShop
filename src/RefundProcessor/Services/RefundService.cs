using Inked.Ordering.API.Application.IntegrationEvents.Events;

namespace Inked.RefundProcessor.Services;

public class RefundService : IRefundService
{
    private readonly ILogger<RefundService> _logger;
    private readonly IOrderService _orderService;

    public RefundService(IOrderService orderService, ILogger<RefundService> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task ProcessRefund(int orderId, string buyerId)
    {
        _logger.LogInformation("Processing refund for order {OrderId} and buyer {BuyerId}.", orderId, buyerId);
        // Implementation to process the refund
        await _orderService.MarkOrderAsRefunded(orderId);
    }

    public async Task TriggerRefundDomainEvent(int orderId, string buyerId)
    {
        _logger.LogInformation("Triggering RefundDomainEvent for order {OrderId} and buyer {BuyerId}.", orderId,
            buyerId);
        var refundDomainEvent = new OrderRefundedIntegrationEvent(orderId, buyerId);
        // Code to publish the event (e.g., using an event bus)
        await Task.CompletedTask;
    }
}
