namespace eShop.Ordering.Domain.Events;

public class OrderCompletedDomainEvent : INotification
{
    public Order Order { get; }

    public OrderCompletedDomainEvent(Order order)
    {
        Order = order;
    }
}

