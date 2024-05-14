using eShop.ClientApp.BasketGrpcClient;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Services.FixUri;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.Settings;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using BasketItem = eShop.ClientApp.Models.Basket.BasketItem;

namespace eShop.ClientApp.Services.Basket;

public class BasketService : IBasketService, IDisposable
{
    private readonly IFixUriService _fixUriService;
    private readonly IIdentityService _identityService;
    private readonly ISettingsService _settingsService;
    private BasketGrpcClient.Basket.BasketClient _basketClient;

    private GrpcChannel _channel;

    public BasketService(IIdentityService identityService, ISettingsService settingsService,
        IFixUriService fixUriService)
    {
        _identityService = identityService;
        _settingsService = settingsService;
        _fixUriService = fixUriService;
    }

    public IEnumerable<BasketItem> LocalBasketItems { get; set; }

    public async Task<CustomerBasket> GetBasketAsync()
    {
        CustomerBasket basket = new();

        var authToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return basket;
        }

        try
        {
            var basketResponse = await GetBasketClient()
                .GetBasketAsync(new GetBasketRequest(), CreateAuthenticationHeaders(authToken));
            
            if (basketResponse.IsInitialized() && basketResponse.Items.Any())
            {
                foreach (var item in basketResponse.Items)
                {
                    basket.AddItemToBasket(new BasketItem {ProductId = item.ProductId, Quantity = item.Quantity});
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            basket = null;
        }

        _fixUriService.FixBasketItemPictureUri(basket?.Items);
        return basket;
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket customerBasket)
    {
        var authToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return customerBasket;
        }

        var updateBasketRequest = new UpdateBasketRequest();

        updateBasketRequest.Items.Add(
            customerBasket.Items
                .Select(
                    x =>
                        new BasketGrpcClient.BasketItem {ProductId = x.ProductId, Quantity = x.Quantity}));

        var result = await GetBasketClient()
            .UpdateBasketAsync(updateBasketRequest, CreateAuthenticationHeaders(authToken)).ConfigureAwait(false);

        if (result.Items.Count > 0)
        {
            customerBasket.ClearBasket();
        }

        foreach (var item in result.Items)
        {
            customerBasket.AddItemToBasket(new BasketItem {ProductId = item.ProductId, Quantity = item.Quantity});
        }

        return customerBasket;
    }

    public async Task ClearBasketAsync()
    {
        var authToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return;
        }

        await GetBasketClient().DeleteBasketAsync(new DeleteBasketRequest(), CreateAuthenticationHeaders(authToken))
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private BasketGrpcClient.Basket.BasketClient GetBasketClient()
    {
        if (_basketClient is not null)
        {
            return _basketClient;
        }

        _channel = GrpcChannel.ForAddress(_settingsService.GatewayBasketEndpointBase);

        _basketClient = new BasketGrpcClient.Basket.BasketClient(_channel);

        return _basketClient;
    }

    private Metadata CreateAuthenticationHeaders(string token)
    {
        var headers = new Metadata();
        headers.Add("authorization", $"Bearer {token}");
        return headers;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _channel?.Dispose();
        }
    }

    ~BasketService()
    {
        Dispose(false);
    }
}
