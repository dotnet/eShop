namespace eShop.ClientApp.Services.Settings;

public interface ISettingsService
{
    string AuthAccessToken { get; set; }
    
    string AuthRefreshToken { get; set; }
    
    string AuthIdToken { get; set; }
    
    bool UseMocks { get; set; }
    
    string DefaultEndpoint { get; set; }
    
    string RegistrationEndpoint { get; set; }
    
    string ClientId { get; set; }
    
    string ClientSecret { get; set; }
    
    string CallbackUri { get; set; }
    
    string IdentityEndpointBase { get; set; }
    
    string GatewayCatalogEndpointBase { get; set; }
    
    string GatewayOrdersEndpointBase { get; set; }
    
    string GatewayBasketEndpointBase { get; set; }
    
    bool UseFakeLocation { get; set; }
    
    string Latitude { get; set; }
    
    string Longitude { get; set; }
    
    bool AllowGpsLocation { get; set; }
}
