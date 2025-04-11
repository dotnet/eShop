using Inked.WebAppComponents.Catalog;

namespace Inked.WebApp.Services;

public interface IBasketState
{
    public Task<IReadOnlyCollection<BasketItem>> GetBasketItemsAsync();

    public Task AddAsync(CatalogItem item);
}
