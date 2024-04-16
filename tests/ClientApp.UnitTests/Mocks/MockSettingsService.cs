using eShop.ClientApp.Models.Token;

namespace ClientApp.UnitTests.Mocks;

public class MockSettingsService : ISettingsService
{
    private const string AccessToken = "access_token";
    private const string IdToken = "id_token";
    private const string IdUseMocks = "use_mocks";
    private const string IdIdentityBase = "url_base";
    private const string IdGatewayMarketingBase = "url_marketing";
    private const string IdGatewayShoppingBase = "url_shopping";
    private const string IdUseFakeLocation = "use_fake_location";
    private const string IdLatitude = "latitude";
    private const string IdLongitude = "longitude";
    private const string IdAllowGpsLocation = "allow_gps_location";
    
    private const string AccessTokenDefault = "default_access_token";
    private const string IdTokenDefault = "";
    private const bool UseMocksDefault = true;
    private const bool UseFakeLocationDefault = false;
    private const bool AllowGpsLocationDefault = false;
    private const double FakeLatitudeDefault = 47.604610d;
    private const double FakeLongitudeDefault = -122.315752d;
    private const string UrlIdentityDefault = "https://13.88.8.119";
    private const string UrlGatewayMarketingDefault = "https://13.88.8.119";
    private const string UrlGatewayShoppingDefault = "https://13.88.8.119";
    
    private readonly IDictionary<string, object> _settings = new Dictionary<string, object>();
    private UserToken? _userToken;

    public string AuthAccessToken
    {
        get => GetValueOrDefault(AccessToken, AccessTokenDefault);
        set => AddOrUpdateValue(AccessToken, value);
    }

    public string AuthIdToken
    {
        get => GetValueOrDefault(IdToken, IdTokenDefault);
        set => AddOrUpdateValue(IdToken, value);
    }

    public Task<UserToken?> GetUserTokenAsync()
    {
        return Task.FromResult(_userToken);
    }

    public Task SetUserTokenAsync(UserToken? userToken)
    {
        _userToken = userToken;

        return Task.CompletedTask;
    }

    public bool UseMocks
    {
        get => GetValueOrDefault(IdUseMocks, UseMocksDefault);
        set => AddOrUpdateValue(IdUseMocks, value);
    }

    public string DefaultEndpoint
    {
        get => GetValueOrDefault(nameof(DefaultEndpoint), string.Empty);
        set => AddOrUpdateValue(nameof(DefaultEndpoint), value);
    }
    
    public string RegistrationEndpoint
    {
        get => GetValueOrDefault(nameof(RegistrationEndpoint), string.Empty);
        set => AddOrUpdateValue(nameof(RegistrationEndpoint), value);
    }
    
    public string AuthorizeEndpoint
    {
        get => GetValueOrDefault(nameof(AuthorizeEndpoint), string.Empty);
        set => AddOrUpdateValue(nameof(AuthorizeEndpoint), value);
    }
    
    public string UserInfoEndpoint
    {
        get => GetValueOrDefault(nameof(UserInfoEndpoint), string.Empty);
        set => AddOrUpdateValue(nameof(UserInfoEndpoint), value);
    }
    
    public string ClientId
    {
        get => GetValueOrDefault(nameof(ClientId), string.Empty);
        set => AddOrUpdateValue(nameof(ClientId), value);
    }
    
    public string ClientSecret
    {
        get => GetValueOrDefault(nameof(ClientSecret), string.Empty);
        set => AddOrUpdateValue(nameof(ClientSecret), value);
    }
    
    public string CallbackUri
    {
        get => GetValueOrDefault(nameof(CallbackUri), string.Empty);
        set => AddOrUpdateValue(nameof(CallbackUri), value);
    }

    public string IdentityEndpointBase
    {
        get => GetValueOrDefault(IdIdentityBase, UrlIdentityDefault);
        set => AddOrUpdateValue(IdIdentityBase, value);
    }

    public string GatewayCatalogEndpointBase
    {
        get => GetValueOrDefault(nameof(GatewayCatalogEndpointBase), string.Empty);
        set => AddOrUpdateValue(nameof(GatewayCatalogEndpointBase), value);
    }
    
    public string GatewayOrdersEndpointBase
    {
        get => GetValueOrDefault(nameof(GatewayOrdersEndpointBase), string.Empty);
        set => AddOrUpdateValue(nameof(GatewayOrdersEndpointBase), value);
    }
    
    public string GatewayBasketEndpointBase
    {
        get => GetValueOrDefault(nameof(GatewayBasketEndpointBase), string.Empty);
        set => AddOrUpdateValue(nameof(GatewayBasketEndpointBase), value);
    }

    public string GatewayShoppingEndpointBase
    {
        get => GetValueOrDefault(IdGatewayShoppingBase, UrlGatewayShoppingDefault);
        set => AddOrUpdateValue(IdGatewayShoppingBase, value);
    }

    public string GatewayMarketingEndpointBase
    {
        get => GetValueOrDefault(IdGatewayMarketingBase, UrlGatewayMarketingDefault);
        set => AddOrUpdateValue(IdGatewayMarketingBase, value);
    }

    public bool UseFakeLocation
    {
        get => GetValueOrDefault(IdUseFakeLocation, UseFakeLocationDefault);
        set => AddOrUpdateValue(IdUseFakeLocation, value);
    }

    public string Latitude
    {
        get => GetValueOrDefault(IdLatitude, FakeLatitudeDefault.ToString());
        set => AddOrUpdateValue(IdLatitude, value);
    }

    public string Longitude
    {
        get => GetValueOrDefault(IdLongitude, FakeLongitudeDefault.ToString());
        set => AddOrUpdateValue(IdLongitude, value);
    }

    public bool AllowGpsLocation
    {
        get => GetValueOrDefault(IdAllowGpsLocation, AllowGpsLocationDefault);
        set => AddOrUpdateValue(IdAllowGpsLocation, value);
    }

    public void AddOrUpdateValue(string key, bool value) => AddOrUpdateValueInternal(key, value);
    public void AddOrUpdateValue(string key, string value) => AddOrUpdateValueInternal(key, value);
    public bool GetValueOrDefault(string key, bool defaultValue) => GetValueOrDefaultInternal(key, defaultValue);
    public string GetValueOrDefault(string key, string defaultValue) => GetValueOrDefaultInternal(key, defaultValue);

    void AddOrUpdateValueInternal<T>(string key, T value)
    {
        if (value is null)
        {
            Remove(key);
        }
        else
        {
            _settings[key] = value;
        }
    }

    T GetValueOrDefaultInternal<T>(string key, T defaultValue = default!) =>
        _settings.TryGetValue(key, out object? value)
            ? null != value ? (T)value : defaultValue : defaultValue;

    void Remove(string key)
    {
        if (_settings.ContainsKey(key))
        {
            _settings.Remove(key);
        }
    }
}
