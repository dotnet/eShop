namespace Inked.RefundProcessor.Services;

public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    public async Task MarkOrderAsRefunded(int orderId)
    {
        _logger.LogInformation("Marking order {OrderId} as refunded.", orderId);
        // Implementation to mark the order as refunded
        await Task.CompletedTask;
    }
}
