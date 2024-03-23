using eShop.ClientApp.Models.User;

namespace eShop.ClientApp.Services.Identity;

public class IdentityMockService : IIdentityService
{
    public Task<bool> SignInAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> SignOutAsync()
    {
        return Task.FromResult(true);
    }

    public Task<UserInfo> GetUserInfoAsync(string authToken)
    {
        return Task.FromResult(
            new UserInfo
            {
                Address = "Mock Address",
            });
    }
}
