using eShop.ClientApp.Models.Basket;

namespace eShop.ClientApp.Services.Basket;

public interface IBasketService
{
    IEnumerable<BasketItem> LocalBasketItems { get; set; }
    Task<CustomerBasket> GetBasketAsync();
    Task<CustomerBasket> UpdateBasketAsync(CustomerBasket customerBasket);
    Task ClearBasketAsync();
}
