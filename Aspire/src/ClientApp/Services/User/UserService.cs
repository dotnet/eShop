using eShop.ClientApp.Helpers;
using eShop.ClientApp.Models.User;
using eShop.ClientApp.Services.RequestProvider;

namespace eShop.ClientApp.Services.User;

public class UserService : IUserService
{
    private readonly IRequestProvider _requestProvider;

    public UserService(IRequestProvider requestProvider)
    {
        _requestProvider = requestProvider;
    }

    public async Task<UserInfo> GetUserInfoAsync(string authToken)
    {
        var uri = UriHelper.CombineUri(GlobalSetting.Instance.UserInfoEndpoint);

        var userInfo = await _requestProvider.GetAsync<UserInfo>(uri, authToken);
        return userInfo;
    }
}
