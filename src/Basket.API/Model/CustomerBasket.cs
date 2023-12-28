namespace eShop.Basket.API.Model;

public class CustomerBasket
{
    public string BuyerId { get; set; } = null!;

    public List<BasketItem> Items { get; set; } = [];

    public CustomerBasket() { }

    public CustomerBasket(string customerId)
    {
        BuyerId = customerId;
    }
}
