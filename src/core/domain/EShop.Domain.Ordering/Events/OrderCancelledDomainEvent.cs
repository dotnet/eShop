using EShop.Domain.Ordering.Entities;
using EShop.Domain.SharedKernel.Events;

namespace EShop.Domain.Ordering.Events;

public class OrderCancelledDomainEvent : IDomainEvent
{
    public Order Order { get; }

    public OrderCancelledDomainEvent(Order order)
    {
        Order = order;
    }
}

