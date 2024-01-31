namespace eShop.ClientApp.Services.Settings;

public interface ISettingsService
{
    string AuthAccessToken { get; set; }
    string AuthIdToken { get; set; }
    bool UseMocks { get; set; }
    string IdentityEndpointBase { get; set; }
    string GatewayShoppingEndpointBase { get; set; }
    string GatewayMarketingEndpointBase { get; set; }
    bool UseFakeLocation { get; set; }
    string Latitude { get; set; }
    string Longitude { get; set; }
    bool AllowGpsLocation { get; set; }
}
