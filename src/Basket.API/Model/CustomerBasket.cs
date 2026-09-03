namespace eShop.Basket.API.Model;

public class CustomerBasket
{
    public string Id { get; set; }

    public string BuyerId { get; set; }

    public List<BasketItem> Items { get; set; } = [];

    public CustomerBasket() { }

    public CustomerBasket(string customerId)
    {
        BuyerId = customerId;
    }
}
