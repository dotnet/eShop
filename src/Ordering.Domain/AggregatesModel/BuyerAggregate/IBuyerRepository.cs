namespace eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

// This defines the Repository Contracts or Interfaces at the Domain Layer,
// as a requirement for the Buyer Aggregate.
public interface IBuyerRepository : IRepository<Buyer>
{
    Buyer Add(Buyer buyer);
    Buyer Update(Buyer buyer);
    Task<Buyer> FindAsync(string BuyerIdentityGuid);
    Task<Buyer> FindByIdAsync(int id);
}

