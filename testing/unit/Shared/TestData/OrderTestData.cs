using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;

namespace eShop.UnitTests.Shared.TestData;

public static class OrderTestData
{
    public static Order CreateValidOrder(
        string userId = "test-user-123",
        string userName = "testuser@example.com",
        Address? address = null,
        int cardTypeId = 1,
        string cardNumber = "4111111111111111",
        string cardSecurityNumber = "123",
        string cardHolderName = "Test User",
        DateTime? cardExpiration = null,
        int? buyerId = null,
        int? paymentMethodId = null)
    {
        var orderAddress = address ?? CreateAddress();
        var expiration = cardExpiration ?? DateTime.Now.AddYears(2);

        return new Order(
            userId,
            userName,
            orderAddress,
            cardTypeId,
            cardNumber,
            cardSecurityNumber,
            cardHolderName,
            expiration,
            buyerId,
            paymentMethodId
        );
    }

    public static Address CreateAddress(
        string street = "123 Test Street",
        string city = "Seattle",
        string state = "WA",
        string country = "USA",
        string zipCode = "98101")
    {
        return new Address(street, city, state, country, zipCode);
    }

    public static OrderItem CreateOrderItem(
        int productId = 1,
        string productName = "Test Product",
        decimal unitPrice = 99.99m,
        decimal discount = 0,
        string pictureUrl = "test.jpg",
        int units = 1)
    {
        return new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);
    }

    public static List<OrderItem> CreateOrderItems(int count = 3)
    {
        var items = new List<OrderItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(CreateOrderItem(
                productId: i,
                productName: $"Product {i}",
                unitPrice: 10.00m * i,
                units: i
            ));
        }
        return items;
    }

    public static Buyer CreateBuyer(
        string identityGuid = "test-identity-123",
        string name = "Test Buyer")
    {
        return new Buyer(identityGuid, name);
    }

    public static PaymentMethod CreatePaymentMethod(
        int cardTypeId = 1,
        string alias = "Test Card",
        string cardNumber = "4111111111111111",
        string securityNumber = "123",
        string cardHolderName = "Test User",
        DateTime? expiration = null)
    {
        return new PaymentMethod(
            cardTypeId,
            alias,
            cardNumber,
            securityNumber,
            cardHolderName,
            expiration ?? DateTime.Now.AddYears(2)
        );
    }
}