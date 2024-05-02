using eShop.ClientApp.Models.User;
using eShop.ClientApp.Services.AppEnvironment;

namespace eShop.ClientApp.Services;

public class MauiNavigationService : INavigationService
{
    private readonly IAppEnvironmentService _appEnvironmentService;

    public MauiNavigationService(IAppEnvironmentService appEnvironmentService)
    {
        _appEnvironmentService = appEnvironmentService;
    }

    public async Task InitializeAsync()
    {
        var user = await _appEnvironmentService.IdentityService.GetUserInfoAsync();

        await NavigateToAsync(user == UserInfo.Default ? "//Login" : "//Main/Catalog");
    }

    public Task NavigateToAsync(string route, IDictionary<string, object> routeParameters = null)
    {
        var shellNavigation = new ShellNavigationState(route);

        return routeParameters != null
            ? Shell.Current.GoToAsync(shellNavigation, routeParameters)
            : Shell.Current.GoToAsync(shellNavigation);
    }

    public Task PopAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
