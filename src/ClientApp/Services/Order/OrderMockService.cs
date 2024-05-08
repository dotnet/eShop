using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Models.Orders;
using eShop.ClientApp.Models.User;

namespace eShop.ClientApp.Services.Order;

public class OrderMockService : IOrderService
{
    private static readonly DateTime MockExpirationDate = DateTime.Now.AddYears(5);

    private static readonly Address MockAdress = new()
    {
        Id = Guid.NewGuid(),
        City = "Seattle, WA",
        Street = "120 E 87th Street",
        CountryCode = "98122",
        Country = "United States",
        Latitude = 40.785091,
        Longitude = -73.968285,
        State = "Seattle",
        StateCode = "WA",
        ZipCode = "98101"
    };

    private static readonly PaymentInfo MockPaymentInfo = new()
    {
        Id = Guid.NewGuid(),
        CardHolderName = "American Express",
        CardNumber = "XXXXXXXXXXXX0005",
        CardType = new CardType
        {
            Id = 3,
            Name = "MasterCard"
        },
        Expiration = MockExpirationDate.ToString(),
        ExpirationMonth = MockExpirationDate.Month,
        ExpirationYear = MockExpirationDate.Year,
        SecurityNumber = "123"
    };

    private static readonly List<OrderItem> MockOrderItems = new()
    {
        new OrderItem
        {
            OrderId = Guid.NewGuid(),
            ProductId = Common.Common.MockCatalogItemId01,
            Discount = 15,
            ProductName = ".NET Bot Blue Sweatshirt (M)",
            Quantity = 1,
            UnitPrice = 16.50M,
            PictureUrl = "fake_product_01.png"
        },
        new OrderItem
        {
            OrderId = Guid.NewGuid(),
            ProductId = Common.Common.MockCatalogItemId03,
            Discount = 0,
            ProductName = ".NET Bot Black Sweatshirt (M)",
            Quantity = 2,
            UnitPrice = 19.95M,
            PictureUrl = "fake_product_03.png"
        }
    };

    private static readonly OrderCheckout MockOrderCheckout = new()
    {
        CardExpiration = DateTime.UtcNow,
        CardHolderName = "FakeCardHolderName",
        CardNumber = "XXXXXXXXXXXX3224",
        CardSecurityNumber = "1234",
        CardTypeId = 1,
        City = "FakeCity",
        Country = "FakeCountry",
        ZipCode = "FakeZipCode",
        Street = "FakeStreet"
    };

    private readonly List<Models.Orders.Order> MockOrders = new()
    {
        new Models.Orders.Order
        {
            OrderNumber = 1,
            SequenceNumber = 123,
            OrderDate = DateTime.Now,
            OrderStatus = "Submitted",
            OrderItems = MockOrderItems,
            CardTypeId = MockPaymentInfo.CardType.Id,
            CardHolderName = MockPaymentInfo.CardHolderName,
            CardNumber = MockPaymentInfo.CardNumber,
            CardSecurityNumber = MockPaymentInfo.SecurityNumber,
            CardExpiration = new DateTime(MockPaymentInfo.ExpirationYear, MockPaymentInfo.ExpirationMonth, 1),
            ShippingCity = MockAdress.City,
            ShippingState = MockAdress.State,
            ShippingCountry = MockAdress.Country,
            ShippingStreet = MockAdress.Street,
            Total = 36.46M
        },
        new Models.Orders.Order
        {
            OrderNumber = 2,
            SequenceNumber = 132,
            OrderDate = DateTime.Now,
            OrderStatus = "Paid",
            OrderItems = MockOrderItems,
            CardTypeId = MockPaymentInfo.CardType.Id,
            CardHolderName = MockPaymentInfo.CardHolderName,
            CardNumber = MockPaymentInfo.CardNumber,
            CardSecurityNumber = MockPaymentInfo.SecurityNumber,
            CardExpiration = new DateTime(MockPaymentInfo.ExpirationYear, MockPaymentInfo.ExpirationMonth, 1),
            ShippingCity = MockAdress.City,
            ShippingState = MockAdress.State,
            ShippingCountry = MockAdress.Country,
            ShippingStreet = MockAdress.Street,
            Total = 36.46M
        },
        new Models.Orders.Order
        {
            OrderNumber = 3,
            SequenceNumber = 231,
            OrderDate = DateTime.Now,
            OrderStatus = "Cancelled",
            OrderItems = MockOrderItems,
            CardTypeId = MockPaymentInfo.CardType.Id,
            CardHolderName = MockPaymentInfo.CardHolderName,
            CardNumber = MockPaymentInfo.CardNumber,
            CardSecurityNumber = MockPaymentInfo.SecurityNumber,
            CardExpiration = new DateTime(MockPaymentInfo.ExpirationYear, MockPaymentInfo.ExpirationMonth, 1),
            ShippingCity = MockAdress.City,
            ShippingState = MockAdress.State,
            ShippingCountry = MockAdress.Country,
            ShippingStreet = MockAdress.Street,
            Total = 36.46M
        },
        new Models.Orders.Order
        {
            OrderNumber = 4,
            SequenceNumber = 131,
            OrderDate = DateTime.Now,
            OrderStatus = "Shipped",
            OrderItems = MockOrderItems,
            CardTypeId = MockPaymentInfo.CardType.Id,
            CardHolderName = MockPaymentInfo.CardHolderName,
            CardNumber = MockPaymentInfo.CardNumber,
            CardSecurityNumber = MockPaymentInfo.SecurityNumber,
            CardExpiration = new DateTime(MockPaymentInfo.ExpirationYear, MockPaymentInfo.ExpirationMonth, 1),
            ShippingCity = MockAdress.City,
            ShippingState = MockAdress.State,
            ShippingCountry = MockAdress.Country,
            ShippingStreet = MockAdress.Street,
            Total = 36.46M
        }
    };

    public async Task<IEnumerable<Models.Orders.Order>> GetOrdersAsync()
    {
        await Task.Delay(10);

        return MockOrders
            .OrderByDescending(o => o.OrderNumber)
            .ToArray();
    }

    public async Task<Models.Orders.Order> GetOrderAsync(int orderId)
    {
        await Task.Delay(10);

        return MockOrders
            .FirstOrDefault(o => o.OrderNumber.Equals(orderId));
    }

    public async Task CreateOrderAsync(Models.Orders.Order newOrder)
    {
        await Task.Delay(10);

        MockOrders.Add(newOrder);
    }

    public OrderCheckout MapOrderToBasket(Models.Orders.Order order)
    {
        return MockOrderCheckout;
    }

    public Task<bool> CancelOrderAsync(int orderId)
    {
        return Task.FromResult(true);
    }
}
