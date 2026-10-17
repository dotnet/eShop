namespace eShop.Basket.API.Model;

/// <summary>
/// A shopper's basket, stored in Redis and keyed by the buyer id.
/// Used by the cart and checkout journeys covered by the Playwright e2e suite.
/// </summary>
public class CustomerBasket
{
    public string BuyerId { get; set; }

    public List<BasketItem> Items { get; set; } = [];

    public CustomerBasket() { }

    public CustomerBasket(string customerId)
    {
        BuyerId = customerId;
    }
}
