namespace Inked.Ordering.Domain.Events;

public class OrderStatusChangedToReceivedReturnDomainEvent
    : INotification
{
    public OrderStatusChangedToReceivedReturnDomainEvent(int orderId)
    {
        OrderId = orderId;
    }

    public int OrderId { get; }
}
