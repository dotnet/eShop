using eShop.Basket.API.Model;

namespace eShop.Basket.API.Repositories;

public interface IBasketRepository
{
    Task<CustomerBasket> GetBasketAsync(string customerId, string basketId);
    Task<CustomerBasket> UpdateBasketAsync(string customerId, CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string customerId, string basketId);
    Task<CustomerBasket> CreateBasketAsync(string customerId);
    Task<IReadOnlyList<CustomerBasket>> ListBasketsAsync(string customerId);
}
