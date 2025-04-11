namespace Inked.Ordering.Domain.Events;

public class OrderReceivedReturnDomainEvent : INotification
{
    public OrderReceivedReturnDomainEvent(Order order)
    {
        Order = order;
    }

    public Order Order { get; }
}
