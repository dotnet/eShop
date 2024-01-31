using eShop.ClientApp.Models.Token;

namespace eShop.ClientApp.Services.Identity;

public interface IIdentityService
{
    string CreateAuthorizationRequest();
    string CreateLogoutRequest(string token);
    Task<UserToken> GetTokenAsync(string code);
}
