using EShop.Domain.Ordering.Entities;
using EShop.Domain.SharedKernel.Events;

namespace EShop.Domain.Ordering.Events;

public class OrderShippedDomainEvent : IDomainEvent
{
    public Order Order { get; }

    public OrderShippedDomainEvent(Order order)
    {
        Order = order;
    }
}
