using eShop.ClientApp.Models.Basket;

namespace eShop.ClientApp.Services.Order;

public interface IOrderService
{
    Task CreateOrderAsync(Models.Orders.Order newOrder, string token);
    Task<IEnumerable<Models.Orders.Order>> GetOrdersAsync(string token);
    Task<Models.Orders.Order> GetOrderAsync(int orderId, string token);
    Task<bool> CancelOrderAsync(int orderId, string token);
    BasketCheckout MapOrderToBasket(Models.Orders.Order order);
}
