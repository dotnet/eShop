using eShop.Basket.API.Model;

namespace eShop.Basket.API.Repositories;

public interface IBasketRepository
{
    Task<CustomerBasket> GetBasketAsync(string customerId);
    Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string id);
}
