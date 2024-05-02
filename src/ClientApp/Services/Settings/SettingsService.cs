using System.Globalization;
using System.Text.Json;
using eShop.ClientApp.Models.Token;

namespace eShop.ClientApp.Services.Settings;

public class SettingsService : ISettingsService
{
    #region Setting Constants

    private const string UserAccessToken = "user_token";
    private const string IdUseMocks = "use_mocks";
    private const string IdIdentityBase = "url_base";
    private const string DefaultClientId = "maui";
    private const string DefaultClientSecret = "secret";
    private const string DefaultCallbackUri = "maui://authcallback";
    private const string IdGatewayMarketingBase = "url_marketing";
    private const string IdGatewayShoppingBase = "url_shopping";
    private const string IdGatewayOrdersBase = "url_orders";
    private const string IdGatewayBasketBase = "url_basket";
    private const string IdUseFakeLocation = "use_fake_location";
    private const string IdLatitude = "latitude";
    private const string IdLongitude = "longitude";
    private const string IdAllowGpsLocation = "allow_gps_location";
    private readonly bool UseMocksDefault = true;
    private readonly bool UseFakeLocationDefault = false;
    private readonly bool AllowGpsLocationDefault = false;
    private readonly double FakeLatitudeDefault = 47.604610d;
    private readonly double FakeLongitudeDefault = -122.315752d;

    #endregion

    #region Settings Properties

    public async Task SetUserTokenAsync(UserToken userToken)
    {
        await SecureStorage
            .SetAsync(UserAccessToken, userToken is not null ? JsonSerializer.Serialize(userToken) : string.Empty)
            .ConfigureAwait(false);
    }

    public async Task<UserToken> GetUserTokenAsync()
    {
        var userToken = await SecureStorage.GetAsync(UserAccessToken).ConfigureAwait(false);

        return string.IsNullOrEmpty(userToken) ? default : JsonSerializer.Deserialize<UserToken>(userToken);
    }

    public bool UseMocks
    {
        get => Preferences.Get(IdUseMocks, UseMocksDefault);
        set => Preferences.Set(IdUseMocks, value);
    }

    public string DefaultEndpoint
    {
        get => Preferences.Get(nameof(DefaultEndpoint), string.Empty);
        set => Preferences.Set(nameof(DefaultEndpoint), value);
    }

    public string RegistrationEndpoint
    {
        get => Preferences.Get(nameof(RegistrationEndpoint), string.Empty);
        set => Preferences.Set(nameof(RegistrationEndpoint), value);
    }

    public string AuthorizeEndpoint
    {
        get => Preferences.Get(nameof(AuthorizeEndpoint), string.Empty);
        set => Preferences.Set(nameof(AuthorizeEndpoint), value);
    }

    public string UserInfoEndpoint
    {
        get => Preferences.Get(nameof(UserInfoEndpoint), string.Empty);
        set => Preferences.Set(nameof(UserInfoEndpoint), value);
    }

    public string ClientId
    {
        get => Preferences.Get(nameof(ClientId), DefaultClientId);
        set => Preferences.Set(nameof(ClientId), value);
    }

    public string ClientSecret
    {
        get => Preferences.Get(nameof(ClientSecret), DefaultClientSecret);
        set => Preferences.Set(nameof(ClientSecret), value);
    }

    public string CallbackUri
    {
        get => Preferences.Get(nameof(CallbackUri), DefaultCallbackUri);
        set => Preferences.Set(nameof(CallbackUri), value);
    }

    public string IdentityEndpointBase
    {
        get => Preferences.Get(IdIdentityBase, string.Empty);
        set => Preferences.Set(IdIdentityBase, value);
    }

    public string GatewayCatalogEndpointBase
    {
        get => Preferences.Get(IdGatewayShoppingBase, string.Empty);
        set => Preferences.Set(IdGatewayShoppingBase, value);
    }

    public string GatewayMarketingEndpointBase
    {
        get => Preferences.Get(IdGatewayMarketingBase, string.Empty);
        set => Preferences.Set(IdGatewayMarketingBase, value);
    }
    
    public string GatewayOrdersEndpointBase
    {
        get => Preferences.Get(IdGatewayOrdersBase, string.Empty);
        set => Preferences.Set(IdGatewayOrdersBase, value);
    }

    public string GatewayBasketEndpointBase
    {
        get => Preferences.Get(IdGatewayBasketBase, string.Empty);
        set => Preferences.Set(IdGatewayBasketBase, value);
    }

    public bool UseFakeLocation
    {
        get => Preferences.Get(IdUseFakeLocation, UseFakeLocationDefault);
        set => Preferences.Set(IdUseFakeLocation, value);
    }

    public string Latitude
    {
        get => Preferences.Get(IdLatitude, FakeLatitudeDefault.ToString(CultureInfo.InvariantCulture));
        set => Preferences.Set(IdLatitude, value);
    }

    public string Longitude
    {
        get => Preferences.Get(IdLongitude, FakeLongitudeDefault.ToString(CultureInfo.InvariantCulture));
        set => Preferences.Set(IdLongitude, value);
    }

    public bool AllowGpsLocation
    {
        get => Preferences.Get(IdAllowGpsLocation, AllowGpsLocationDefault);
        set => Preferences.Set(IdAllowGpsLocation, value);
    }

    #endregion
}
