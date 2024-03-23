namespace eShop.ClientApp.UnitTests.Mocks;

public class MockSettingsService : ISettingsService
{
    readonly IDictionary<string, object> _settings = new Dictionary<string, object>();

    const string AccessToken = "access_token";
    const string IdToken = "id_token";
    const string IdUseMocks = "use_mocks";
    const string IdIdentityBase = "url_base";
    const string IdGatewayMarketingBase = "url_marketing";
    const string IdGatewayShoppingBase = "url_shopping";
    const string IdUseFakeLocation = "use_fake_location";
    const string IdLatitude = "latitude";
    const string IdLongitude = "longitude";
    const string IdAllowGpsLocation = "allow_gps_location";
    
    const string AccessTokenDefault = "default_access_token";
    private const string IdTokenDefault = "";
    const bool UseMocksDefault = true;
    const bool UseFakeLocationDefault = false;
    const bool AllowGpsLocationDefault = false;
    const double FakeLatitudeDefault = 47.604610d;
    const double FakeLongitudeDefault = -122.315752d;
    const string UrlIdentityDefault = "https://13.88.8.119";
    const string UrlGatewayMarketingDefault = "https://13.88.8.119";
    const string UrlGatewayShoppingDefault = "https://13.88.8.119";

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
