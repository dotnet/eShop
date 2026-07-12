using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
using eShop.Ordering.Domain.Seedwork;
using eShop.Ordering.Infrastructure.Repositories;

namespace eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;

internal sealed class InMemoryBuyerRepository(OrderingRepositoryMockStore store, OrderingRepositoryMockUnitOfWork unitOfWork) : IBuyerRepository
{
    public IUnitOfWork UnitOfWork => unitOfWork;

    public Buyer Add(Buyer buyer)
    {
        unitOfWork.Track(buyer);
        return buyer;
    }

    public Buyer Update(Buyer buyer)
    {
        unitOfWork.Track(buyer);
        return buyer;
    }

    public Task<Buyer> FindAsync(string buyerIdentityGuid) =>
        Task.FromResult(store.Buyers.SingleOrDefault(buyer => buyer.IdentityGuid == buyerIdentityGuid)!);

    public Task<Buyer> FindByIdAsync(int id) =>
        Task.FromResult(store.Buyers.SingleOrDefault(buyer => buyer.Id == id)!);
}
