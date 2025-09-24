using eShop.ClientApp.Services;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class MainViewModel(INavigationService navigationService) : ViewModelBase(navigationService)
{
    [RelayCommand]
    private async Task SettingsAsync()
    {
        await NavigationService.NavigateToAsync("Settings");
    }
}
