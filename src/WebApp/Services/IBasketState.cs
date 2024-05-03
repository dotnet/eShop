using eShop.WebAppComponents.Catalog;

namespace eShop.WebApp.Services
{
    public interface IBasketState
    {
        public Task<IReadOnlyCollection<BasketItem>> GetBasketItemsAsync();

        public Task AddAsync(CatalogItem item);
    }
}
