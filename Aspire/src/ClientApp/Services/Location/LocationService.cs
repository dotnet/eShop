using eShop.ClientApp.Helpers;
using eShop.ClientApp.Services.RequestProvider;

namespace eShop.ClientApp.Services.Location;

public class LocationService : ILocationService
{
    private readonly IRequestProvider _requestProvider;

    private const string ApiUrlBase = "l/api/v1/locations";

    public LocationService(IRequestProvider requestProvider)
    {
        _requestProvider = requestProvider;
    }

    public async Task UpdateUserLocation(Models.Location.Location newLocReq, string token)
    {
        var uri = UriHelper.CombineUri(GlobalSetting.Instance.GatewayMarketingEndpoint, ApiUrlBase);

        await _requestProvider.PostAsync(uri, newLocReq, token);
    }
}
