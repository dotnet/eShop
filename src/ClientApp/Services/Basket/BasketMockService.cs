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

    public async Task<CustomerBasket> GetBasketAsync()
    {
        await Task.Delay(10);
        
        return MockCustomBasket;
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket customerBasket)
    {
        await Task.Delay(10);
        
        MockCustomBasket = customerBasket;

        return MockCustomBasket;
    }

    public async Task ClearBasketAsync()
    {
        await Task.Delay(10);

        MockCustomBasket.Items.Clear();

        LocalBasketItems = null;
    }
}
