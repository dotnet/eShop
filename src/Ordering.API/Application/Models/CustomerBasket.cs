namespace Inked.Ordering.API.Application.Models;

public class CustomerBasket
{
    public CustomerBasket(string buyerId, List<BasketItem> items)
    {
        BuyerId = buyerId;
        Items = items;
    }

    public string BuyerId { get; set; }
    public List<BasketItem> Items { get; set; }
}
