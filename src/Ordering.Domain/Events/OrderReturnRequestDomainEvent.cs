using AggregatesModel_OrderAggregate_Order = Inked.Ordering.Domain.AggregatesModel.OrderAggregate.Order;

namespace Inked.Ordering.Domain.Events;

public class OrderReturnRequestedDomainEvent : INotification
{
    public OrderReturnRequestedDomainEvent(AggregatesModel_OrderAggregate_Order order)
    {
        Order = order;
    }

    public AggregatesModel_OrderAggregate_Order Order { get; }
}
