namespace Inked.RefundProcessor.Services;

public interface IOrderService
{
    Task MarkOrderAsRefunded(int orderId);
}
