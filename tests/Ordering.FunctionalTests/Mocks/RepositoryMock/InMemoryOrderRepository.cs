using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.Seedwork;
using eShop.Ordering.Infrastructure.Repositories;

using Order = eShop.Ordering.Domain.AggregatesModel.OrderAggregate.Order;

namespace eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;

internal sealed class InMemoryOrderRepository(OrderingRepositoryMockStore store, OrderingRepositoryMockUnitOfWork unitOfWork) : IOrderRepository
{
    public IUnitOfWork UnitOfWork => unitOfWork;

    public Order Add(Order order)
    {
        unitOfWork.Track(order);
        return order;
    }

    public Task<Order> GetAsync(int orderId) =>
        Task.FromResult(store.Orders.Single(order => order.Id == orderId));

    public void Update(Order order) => unitOfWork.Track(order);
}
