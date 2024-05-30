using eShop.ClientApp.Models.Token;
using eShop.ClientApp.Models.User;
using eShop.ClientApp.Services.Settings;
using IdentityModel.OidcClient;
using IBrowser = IdentityModel.OidcClient.Browser.IBrowser;

namespace eShop.ClientApp.Services.Identity;

public class IdentityService : IIdentityService
{
    private readonly IBrowser _browser;
    private readonly ISettingsService _settingsService;
    private readonly HttpMessageHandler _httpMessageHandler;

    public IdentityService(IBrowser browser, ISettingsService settingsService, HttpMessageHandler httpMessageHandler)
    {
        _browser = browser;
        _settingsService = settingsService;
        _httpMessageHandler = httpMessageHandler;
    }

    public async Task<bool> SignInAsync()
    {
        var response = await GetClient().LoginAsync(new LoginRequest()).ConfigureAwait(false);

        if (response.IsError)
        {
            return false;
        }

        await _settingsService
            .SetUserTokenAsync(
                new UserToken
                {
                    AccessToken = response.AccessToken,
                    IdToken = response.IdentityToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresAt = response.AccessTokenExpiration
                })
            .ConfigureAwait(false);

        return !response.IsError;
    }

    public async Task<bool> SignOutAsync()
    {
        var response = await GetClient().LogoutAsync(new LogoutRequest()).ConfigureAwait(false);

        if (response.IsError)
        {
            return false;
        }

        await _settingsService.SetUserTokenAsync(default);

        return !response.IsError;
    }

    public async Task<UserInfo> GetUserInfoAsync()
    {
        var authToken = await GetAuthTokenAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(authToken))
        {
            return UserInfo.Default;
        }

        var userInfoWithClaims = await GetClient().GetUserInfoAsync(authToken).ConfigureAwait(false);

        return
            new UserInfo
            {
                UserId = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                Email = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                PhoneNumber = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "phone_number")?.Value,
                Street = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_street")?.Value,
                Address = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_city")?.Value,
                State = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_state")?.Value,
                ZipCode = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_zip_code")?.Value,
                Country = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "address_country")?.Value,
                PreferredUsername =
                    userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value,
                Name = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
                LastName = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "last_name")?.Value,
                CardNumber = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "card_number")?.Value,
                CardHolder = userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "card_holder")?.Value,
                CardSecurityNumber =
                    userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "card_security_number")?.Value,
                PhoneNumberVerified =
                    bool.Parse(userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "phone_number_verified")
                        ?.Value ?? "false"),
                EmailVerified =
                    bool.Parse(userInfoWithClaims.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value ??
                               "false")
            };
    }
    
    public async Task<string> GetAuthTokenAsync()
    {
        var userToken = await _settingsService.GetUserTokenAsync().ConfigureAwait(false);

        if (userToken is null)
        {
            return string.Empty;
        }

        if (userToken.ExpiresAt.Subtract(DateTimeOffset.Now).TotalMinutes > 5)
        {
            return userToken.AccessToken;
        }

        var response = await GetClient().RefreshTokenAsync(userToken.RefreshToken).ConfigureAwait(false);

        if (response.IsError)
        {
            return string.Empty;
        }

        await _settingsService
            .SetUserTokenAsync(
                new UserToken
                {
                    AccessToken = response.AccessToken,
                    IdToken = response.IdentityToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresAt = response.AccessTokenExpiration
                })
            .ConfigureAwait(false);

        return response.AccessToken;
    }

    private OidcClient GetClient()
    {
        var options = new OidcClientOptions
        {
            Authority = _settingsService.IdentityEndpointBase,
            ClientId = _settingsService.ClientId,
            ClientSecret = _settingsService.ClientSecret,
            Scope = "openid profile basket orders offline_access",
            RedirectUri = _settingsService.CallbackUri,
            PostLogoutRedirectUri = _settingsService.CallbackUri,
            Browser = _browser,
        };

        if (_httpMessageHandler is not null)
        {
            options.BackchannelHandler = _httpMessageHandler;
        }
        
        return new OidcClient(options);
    }
}
