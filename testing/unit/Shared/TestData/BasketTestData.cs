using eShop.Basket.API.Model;

namespace eShop.UnitTests.Shared.TestData;

public static class BasketTestData
{
    public static CustomerBasket CreateValidCustomerBasket(
        string buyerId = "test-buyer-123",
        List<BasketItem>? items = null)
    {
        return new CustomerBasket
        {
            BuyerId = buyerId,
            Items = items ?? CreateBasketItems()
        };
    }

    public static List<BasketItem> CreateBasketItems(int count = 3)
    {
        var items = new List<BasketItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(CreateBasketItem(
                id: $"item-{i}",
                productId: i,
                productName: $"Product {i}",
                unitPrice: 10.00m * i,
                quantity: i
            ));
        }
        return items;
    }

    public static BasketItem CreateBasketItem(
        string id = "test-item-1",
        int productId = 1,
        string productName = "Test Product",
        decimal unitPrice = 99.99m,
        decimal oldUnitPrice = 0,
        int quantity = 1,
        string pictureUrl = "test.jpg")
    {
        return new BasketItem
        {
            Id = id,
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            OldUnitPrice = oldUnitPrice,
            Quantity = quantity,
            PictureUrl = pictureUrl
        };
    }

    public static BasketCheckout CreateBasketCheckout(
        string city = "Seattle",
        string street = "123 Test St",
        string state = "WA",
        string country = "USA",
        string zipCode = "98101",
        string cardNumber = "4111111111111111",
        string cardHolderName = "Test User",
        DateTime? cardExpiration = null,
        string cardSecurityNumber = "123",
        int cardTypeId = 1,
        string buyer = "test-buyer-123",
        Guid? requestId = null)
    {
        return new BasketCheckout
        {
            City = city,
            Street = street,
            State = state,
            Country = country,
            ZipCode = zipCode,
            CardNumber = cardNumber,
            CardHolderName = cardHolderName,
            CardExpiration = cardExpiration ?? DateTime.Now.AddYears(2),
            CardSecurityNumber = cardSecurityNumber,
            CardTypeId = cardTypeId,
            Buyer = buyer,
            RequestId = requestId ?? Guid.NewGuid()
        };
    }
}