using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Models.Orders;

namespace eShop.ClientApp.Services.Order;

public class OrderMockService : IOrderService
{
    private const string MockPaymentMethodId = "pm_mock_mobile";

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
        PaymentMethodId = MockPaymentMethodId,
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
            PaymentMethodId = MockPaymentMethodId,
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
            PaymentMethodId = MockPaymentMethodId,
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
            PaymentMethodId = MockPaymentMethodId,
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
            PaymentMethodId = MockPaymentMethodId,
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
