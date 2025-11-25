using EShop.Domain.SharedKernel.Events;

namespace EShop.Domain.Ordering.Events;

/// <summary>
/// Event used when the order stock items are confirmed
/// </summary>
public class OrderStatusChangedToStockConfirmedDomainEvent
    : IDomainEvent
{
    public int OrderId { get; }

    public OrderStatusChangedToStockConfirmedDomainEvent(int orderId)
        => OrderId = orderId;
}
