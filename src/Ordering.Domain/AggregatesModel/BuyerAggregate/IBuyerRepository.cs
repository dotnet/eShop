namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

//This is just the RepositoryContracts or Interface defined at the Domain Layer
//as requisite for the Buyer Aggregate

public interface IBuyerRepository
{
    Buyer Add(Buyer buyer);
    Buyer Update(Buyer buyer);
    Task<Buyer> FindAsync(string BuyerIdentityGuid);
    Task<Buyer> FindByIdAsync(int id);
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}

