using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.RequestProvider;
using eShop.ClientApp.Services.Settings;

namespace eShop.ClientApp.Services.Location;

public class LocationService : ILocationService
{
    private const string ApiUrlBase = "l/api/v1/locations";
    private readonly IIdentityService _identityService;

    public LocationService(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task UpdateUserLocation(Models.Location.Location newLocReq)
    {
        var accessToken = await _identityService.GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        //TODO: Determine mapped location
        await Task.Delay(10).ConfigureAwait(false);
        //await _requestProvider.PostAsync(uri, newLocReq, token).ConfigureAwait(false);
    }
}
