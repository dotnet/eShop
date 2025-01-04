namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

// This defines the Repository Contracts or Interfaces at the Domain Layer,
// as a requirement for the Order Aggregate.
public interface IOrderRepository : IRepository<Order>
{
    Order Add(Order order);

    void Update(Order order);

    Task<Order> GetAsync(int orderId);
}
