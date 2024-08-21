using System.Net;
using eShop.ClientApp.Helpers;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Models.Orders;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.RequestProvider;
using eShop.ClientApp.Services.Settings;

namespace eShop.ClientApp.Services.Order;

public class OrderService : IOrderService
{
    private const string ApiUrlBase = "api/orders";
    private const string ApiVersion = "api-version=1.0";
    
    private readonly IIdentityService _identityService;
    private readonly IRequestProvider _requestProvider;
    private readonly ISettingsService _settingsService;

    public OrderService(IIdentityService identityService, ISettingsService settingsService,
        IRequestProvider requestProvider)
    {
        _identityService = identityService;
        _settingsService = settingsService;
        _requestProvider = requestProvider;
    }

    public async Task CreateOrderAsync(Models.Orders.Order newOrder)
    {
        var authToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return;
        }

        var uri = $"{UriHelper.CombineUri(_settingsService.GatewayOrdersEndpointBase, ApiUrlBase)}?{ApiVersion}";

        var success = await _requestProvider.PostAsync(uri, newOrder, authToken, "x-requestid").ConfigureAwait(false);
    }

    public async Task<IEnumerable<Models.Orders.Order>> GetOrdersAsync()
    {
        var authToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return Enumerable.Empty<Models.Orders.Order>();
        }

        var uri = $"{UriHelper.CombineUri(_settingsService.GatewayOrdersEndpointBase, ApiUrlBase)}?{ApiVersion}";

        var orders =
            await _requestProvider.GetAsync<IEnumerable<Models.Orders.Order>>(uri, authToken).ConfigureAwait(false);

        return orders ?? Enumerable.Empty<Models.Orders.Order>();
    }

    public async Task<Models.Orders.Order> GetOrderAsync(int orderId)
    {
        var authToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return new Models.Orders.Order();
        }

        try
        {
            var uri = $"{UriHelper.CombineUri(_settingsService.GatewayOrdersEndpointBase, $"{ApiUrlBase}/{orderId}")}?{ApiVersion}";

            var order =
                await _requestProvider.GetAsync<Models.Orders.Order>(uri, authToken).ConfigureAwait(false);

            return order;
        }
        catch
        {
            return new Models.Orders.Order();
        }
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        var authToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return false;
        }

        var uri = $"{UriHelper.CombineUri(_settingsService.GatewayOrdersEndpointBase, $"{ApiUrlBase}/cancel")}?{ApiVersion}";

        var cancelOrderCommand = new CancelOrderCommand(orderId);

        var header = "x-requestid";

        try
        {
            await _requestProvider.PutAsync(uri, cancelOrderCommand, authToken, header).ConfigureAwait(false);
        }
        //If the status of the order has changed before to click cancel button, we will get
        //a BadRequest HttpStatus
        catch (HttpRequestExceptionEx ex) when (ex.HttpCode == HttpStatusCode.BadRequest)
        {
            return false;
        }

        return true;
    }

    public OrderCheckout MapOrderToBasket(Models.Orders.Order order)
    {
        return new OrderCheckout
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
}
