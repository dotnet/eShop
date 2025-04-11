namespace Inked.Ordering.Domain.Events;

/// <summary>
///     Event used when the grace period order is confirmed
/// </summary>
public class OrderStatusChangedToAwaitingValidationDomainEvent
    : INotification
{
    public OrderStatusChangedToAwaitingValidationDomainEvent(int orderId,
        IEnumerable<OrderItem> orderItems)
    {
        OrderId = orderId;
        OrderItems = orderItems;
    }

    public int OrderId { get; }
    public IEnumerable<OrderItem> OrderItems { get; }
}
