using eShop.ClientApp.Helpers;
using eShop.ClientApp.Services.RequestProvider;
using eShop.ClientApp.Services.Settings;

namespace eShop.ClientApp.Services.Location;

public class LocationService : ILocationService
{
    private readonly ISettingsService _settingsService;
    private readonly IRequestProvider _requestProvider;

    private const string ApiUrlBase = "l/api/v1/locations";

    public LocationService(ISettingsService settingsService, IRequestProvider requestProvider)
    {
        _settingsService = settingsService;
        _requestProvider = requestProvider;
    }

    public async Task UpdateUserLocation(Models.Location.Location newLocReq, string token)
    {
        //TODO: Determine mapped location
        await Task.Delay(10).ConfigureAwait(false);
        //await _requestProvider.PostAsync(uri, newLocReq, token).ConfigureAwait(false);
    }
}
