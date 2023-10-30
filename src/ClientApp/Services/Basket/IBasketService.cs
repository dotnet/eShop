using eShop.ClientApp.Models.Basket;

namespace eShop.ClientApp.Services.Basket;

public interface IBasketService
{
    IEnumerable<BasketItem> LocalBasketItems { get; set; }
    Task<CustomerBasket> GetBasketAsync(string guidUser, string token);
    Task<CustomerBasket> UpdateBasketAsync(CustomerBasket customerBasket, string token);
    Task CheckoutAsync(BasketCheckout basketCheckout, string token);
    Task ClearBasketAsync(string guidUser, string token);
}
