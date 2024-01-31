using eShop.ClientApp.Helpers;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Models.Orders;
using eShop.ClientApp.Services.RequestProvider;

namespace eShop.ClientApp.Services.Order;

public class OrderService : IOrderService
{
    private readonly IRequestProvider _requestProvider;

    private const string ApiUrlBase = "o/api/v1/orders";

    public OrderService(IRequestProvider requestProvider)
    {
        _requestProvider = requestProvider;
    }

    public Task CreateOrderAsync(Models.Orders.Order newOrder, string token)
    {
        throw new Exception("Only available in Mock Services!");
    }

    public async Task<IEnumerable<Models.Orders.Order>> GetOrdersAsync(string token)
    {
        var uri = UriHelper.CombineUri(GlobalSetting.Instance.GatewayShoppingEndpoint, ApiUrlBase);

        var orders =
            await _requestProvider.GetAsync<IEnumerable<Models.Orders.Order>>(uri, token).ConfigureAwait(false);

        return orders ?? Enumerable.Empty<Models.Orders.Order>();

    }

    public async Task<Models.Orders.Order> GetOrderAsync(int orderId, string token)
    {
        try
        {
            var uri = UriHelper.CombineUri(GlobalSetting.Instance.GatewayShoppingEndpoint, $"{ApiUrlBase}/{orderId}");

            Models.Orders.Order order =
                await _requestProvider.GetAsync<Models.Orders.Order>(uri, token).ConfigureAwait(false);

            return order;
        }
        catch
        {
            return new Models.Orders.Order();
        }
    }

    public BasketCheckout MapOrderToBasket(Models.Orders.Order order)
    {
        return new BasketCheckout()
        {
            CardExpiration = order.CardExpiration,
            CardHolderName = order.CardHolderName,
            CardNumber = order.CardNumber,
            CardSecurityNumber = order.CardSecurityNumber,
            CardTypeId = order.CardTypeId,
            City = order.ShippingCity,
            State = order.ShippingState,
            Country = order.ShippingCountry,
            ZipCode = order.ShippingZipCode,
            Street = order.ShippingStreet
        };
    }

    public async Task<bool> CancelOrderAsync(int orderId, string token)
    {
        var uri = UriHelper.CombineUri(GlobalSetting.Instance.GatewayShoppingEndpoint, $"{ApiUrlBase}/cancel");

        var cancelOrderCommand = new CancelOrderCommand(orderId);

        var header = "x-requestid";

        try
        {
            await _requestProvider.PutAsync(uri, cancelOrderCommand, token, header).ConfigureAwait(false);
        }
        //If the status of the order has changed before to click cancel button, we will get
        //a BadRequest HttpStatus
        catch (HttpRequestExceptionEx ex) when (ex.HttpCode == System.Net.HttpStatusCode.BadRequest)
        {
            return false;
        }

        return true;
    }
}
