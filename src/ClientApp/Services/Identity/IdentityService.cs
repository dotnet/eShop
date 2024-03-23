using eShop.ClientApp.Models.User;
using eShop.ClientApp.Services.Settings;
using IdentityModel.OidcClient;

namespace eShop.ClientApp.Services.Identity;

public class IdentityService : IIdentityService
{
    private readonly IdentityModel.OidcClient.Browser.IBrowser _browser;
    private readonly ISettingsService _settingsService;

    public IdentityService(IdentityModel.OidcClient.Browser.IBrowser browser, ISettingsService settingsService)
    {
        _browser = browser;
        _settingsService = settingsService;
    }


    public async Task<bool> SignInAsync()
    {
        try
        {
            var client = GetClient();

            var response = await client.LoginAsync(new LoginRequest { });

            if (!response.IsError)
            {
                _settingsService.AuthAccessToken = response.AccessToken;
                _settingsService.AuthRefreshToken = response.RefreshToken;
            }

            return !response.IsError;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<bool> SignOutAsync()
    {
        var client = GetClient();
        var response = await client.LogoutAsync(new LogoutRequest { });

        return !response.IsError;
    }

    public async Task<UserInfo> GetUserInfoAsync(string authToken)
    {
        var client = GetClient();
        
        var refreshedToken = await client.RefreshTokenAsync(_settingsService.AuthRefreshToken);

        if (!refreshedToken.IsError)
        {
            _settingsService.AuthAccessToken = authToken = refreshedToken.AccessToken;
            _settingsService.AuthRefreshToken = refreshedToken.RefreshToken;
        }
        
        var userInfoWithClaims = await client.GetUserInfoAsync(authToken).ConfigureAwait(false);
        UserInfo userInfo =
            new()
            {
                UserId = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                Email = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                PhoneNumber = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "phone_number")?.Value,
                
                Street = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_street")?.Value,
                Address = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_city")?.Value,
                State = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_state")?.Value,
                ZipCode = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_zip_code")?.Value,
                Country = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_country")?.Value,
                
                PreferredUsername = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value,
                Name = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
                LastName = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "last_name")?.Value,
                
                CardNumber = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "card_number")?.Value,
                CardHolder = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "card_holder")?.Value,
                CardSecurityNumber = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "card_security_number")?.Value,
                
                PhoneNumberVerified = bool.Parse(userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "phone_number_verified")?.Value),
                EmailVerified = bool.Parse(userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value),
            };

        return userInfo;
    }


    private OidcClient GetClient()
    {
        return new OidcClient(
            new()
            {
                Authority = _settingsService.IdentityEndpointBase, //"http://localhost:5223/" ,
                ClientId = _settingsService.ClientId, // "maui", // GlobalSetting.Instance.ClientId"",
                ClientSecret = _settingsService.ClientSecret, // "secret", // GlobalSetting.Instance.ClientSecret,
                Scope = "openid profile basket orders offline_access",
                RedirectUri = _settingsService.CallbackUri, // "maui://authcallback", // "GlobalSetting.Instance.Callback",
                Browser = _browser,
                // RefreshDiscoveryDocumentForLogin = false,
            });
    }
}
