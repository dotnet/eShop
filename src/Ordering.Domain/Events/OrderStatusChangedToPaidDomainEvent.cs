namespace Inked.Ordering.Domain.Events;

/// <summary>
///     Event used when the order is paid
/// </summary>
public class OrderStatusChangedToPaidDomainEvent
    : INotification
{
    public OrderStatusChangedToPaidDomainEvent(int orderId,
        IEnumerable<OrderItem> orderItems)
    {
        OrderId = orderId;
        OrderItems = orderItems;
    }

    public int OrderId { get; }
    public IEnumerable<OrderItem> OrderItems { get; }
}
