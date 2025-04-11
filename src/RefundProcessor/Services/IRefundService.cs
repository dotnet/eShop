public interface IRefundService
{
    Task ProcessRefund(int orderId, string buyerId);
    Task TriggerRefundDomainEvent(int orderId, string buyerId);
}
