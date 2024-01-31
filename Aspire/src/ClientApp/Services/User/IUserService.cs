using eShop.ClientApp.Models.User;

namespace eShop.ClientApp.Services.User
{
    public interface IUserService
    {
        Task<UserInfo> GetUserInfoAsync(string authToken);
    }
}
