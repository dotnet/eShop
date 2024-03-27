using eShop.ClientApp.Models.Basket;

namespace eShop.ClientApp.Services.Basket;

public class BasketMockService : IBasketService
{
    private CustomerBasket _mockCustomBasket;

    public BasketMockService()
    {
        _mockCustomBasket = new CustomerBasket {BuyerId = "9245fe4a-d402-451c-b9ed-9c1a04247482"};
        _mockCustomBasket.AddItemToBasket(new BasketItem
        {
            Id = "1",
            PictureUrl = "fake_product_01.png",
            ProductId = Common.Common.MockCatalogItemId01,
            ProductName = ".NET Bot Blue Sweatshirt (M)",
            Quantity = 1,
            UnitPrice = 19.50M
        });

        _mockCustomBasket.AddItemToBasket(new BasketItem
        {
            Id = "2",
            PictureUrl = "fake_product_04.png",
            ProductId = Common.Common.MockCatalogItemId04,
            ProductName = ".NET Black Cup",
            Quantity = 1,
            UnitPrice = 17.00M
        });
    }

    public IEnumerable<BasketItem> LocalBasketItems { get; set; }

    public async Task<CustomerBasket> GetBasketAsync()
    {
        await Task.Delay(10);

        return _mockCustomBasket;
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket customerBasket)
    {
        await Task.Delay(10);

        _mockCustomBasket = customerBasket;

        return _mockCustomBasket;
    }

    public async Task ClearBasketAsync()
    {
        await Task.Delay(10);

        _mockCustomBasket.ClearBasket();

        LocalBasketItems = null;
    }
}
