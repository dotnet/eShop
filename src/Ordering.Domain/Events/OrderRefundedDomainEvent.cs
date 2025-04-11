// File: src/Ordering.Domain/Events/RefundDomainEvent.cs

namespace Inked.Ordering.Domain.Events;

public class OrderRefundedDomainEvent : INotification
{
    public OrderRefundedDomainEvent(Order order)
    {
        Order = order;
    }

    public Order Order { get; }
}
