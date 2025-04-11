namespace Inked.Ordering.Domain.Events;

public class OrderShippedDomainEvent : INotification
{
    public OrderShippedDomainEvent(Order order)
    {
        Order = order;
    }

    public Order Order { get; }
}
