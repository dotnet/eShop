namespace Inked.Ordering.Domain.Events;

/// <summary>
///     Event used when the order stock items are confirmed
/// </summary>
public class OrderStatusChangedToStockConfirmedDomainEvent
    : INotification
{
    public OrderStatusChangedToStockConfirmedDomainEvent(int orderId)
    {
        OrderId = orderId;
    }

    public int OrderId { get; }
}
