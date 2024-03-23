using eShop.ClientApp.Models.Token;
using eShop.ClientApp.Models.User;

namespace eShop.ClientApp.Services.Identity;

public interface IIdentityService
{
    public Task<bool> SignInAsync();

    public Task<bool> SignOutAsync();
    
    Task<UserInfo> GetUserInfoAsync(string authToken);
}
