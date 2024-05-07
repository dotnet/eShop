namespace eShop.ClientApp.Models.Basket;

public class CustomerBasket
{
    private readonly List<BasketItem> _items = new();
    public string BuyerId { get; set; }
    public IReadOnlyList<BasketItem> Items => _items;

    public int ItemCount => _items.Sum(x => x.Quantity);

    public void AddItemToBasket(BasketItem basketItem)
    {
        foreach (var item in _items)
        {
            if (item.ProductId == basketItem.ProductId)
            {
                item.Quantity++;
                return;
            }
        }

        _items.Add(basketItem);
    }

    public void RemoveItemFromBasket(BasketItem basketItem)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i].ProductId == basketItem.ProductId)
            {
                _items.RemoveAt(i);
                return;
            }
        }
    }

    public void ClearBasket()
    {
        _items.Clear();
    }
}
