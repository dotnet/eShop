using eShop.ClientApp.Models.Basket;

namespace eShop.ClientApp.Services.Basket;

public class BasketMockService : IBasketService
{
    public IEnumerable<BasketItem> LocalBasketItems { get; set; }

    private CustomerBasket MockCustomBasket = new()
    {
        BuyerId = "9245fe4a-d402-451c-b9ed-9c1a04247482",
        Items = new List<BasketItem>
            {
                new BasketItem { Id = "1", PictureUrl = "fake_product_01.png", ProductId = Common.Common.MockCatalogItemId01, ProductName = ".NET Bot Blue Sweatshirt (M)", Quantity = 1, UnitPrice = 19.50M },
                new BasketItem { Id = "2", PictureUrl = "fake_product_04.png", ProductId = Common.Common.MockCatalogItemId04, ProductName = ".NET Black Cup", Quantity = 1, UnitPrice = 17.00M }
            }
    };

    public async Task<CustomerBasket> GetBasketAsync(string guidUser, string token)
    {
        await Task.Delay(10);

        if (string.IsNullOrEmpty(guidUser) || string.IsNullOrEmpty(token))
        {
            return new();
        }

        return MockCustomBasket;
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket customerBasket, string token)
    {
        await Task.Delay(10);

        if (string.IsNullOrEmpty(token))
        {
            return new();
        }

        MockCustomBasket = customerBasket;

        return MockCustomBasket;
    }

    public async Task ClearBasketAsync(string guidUser, string token)
    {
        await Task.Delay(10);

        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        if (!string.IsNullOrEmpty(guidUser))
        {
            MockCustomBasket.Items.Clear();

            LocalBasketItems = null;
        }
    }

    public Task CheckoutAsync(BasketCheckout basketCheckout, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(0);
        }

        if (basketCheckout != null)
        {
            MockCustomBasket.Items.Clear();
        }

        return Task.FromResult(0);
    }
}
