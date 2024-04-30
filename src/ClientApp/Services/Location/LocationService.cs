using eShop.ClientApp.Helpers;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.RequestProvider;
using eShop.ClientApp.Services.Settings;

namespace eShop.ClientApp.Services.Location;

public class LocationService : ILocationService
{
    private readonly IIdentityService _identityService;
    private readonly ISettingsService _settingsService;
    private readonly IRequestProvider _requestProvider;

    private const string ApiUrlBase = "l/api/v1/locations";

    public LocationService(IIdentityService identityService, ISettingsService settingsService, IRequestProvider requestProvider)
    {
        _identityService = identityService;
        _settingsService = settingsService;
        _requestProvider = requestProvider;
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
