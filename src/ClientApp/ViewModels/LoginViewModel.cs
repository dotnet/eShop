using System.Diagnostics;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.OpenUrl;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.Validations;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly IOpenUrlService _openUrlService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private bool _isLogin;

    [ObservableProperty] private bool _isMock;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(MockSignInCommand))]
    private bool _isValid;

    [ObservableProperty] private string _loginUrl;

    [ObservableProperty] private ValidatableObject<string> _password = new();

    [ObservableProperty] private ValidatableObject<string> _userName = new();

    public LoginViewModel(
        IOpenUrlService openUrlService, IAppEnvironmentService appEnvironmentService,
        INavigationService navigationService, ISettingsService settingsService)
        : base(navigationService)
    {
        _settingsService = settingsService;
        _openUrlService = openUrlService;
        _appEnvironmentService = appEnvironmentService;

        InvalidateMock();
    }

    public override async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);

        if (query.ValueAsBool("Logout"))
        {
            await PerformLogoutAsync();
        }
    }

    public override Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(IsValid))]
    private async Task MockSignInAsync()
    {
        await IsBusyFor(
            async () =>
            {
                var isAuthenticated = false;

                try
                {
                    await Task.Delay(1000);

                    isAuthenticated = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SignIn] Error signing in: {ex}");
                }

                if (isAuthenticated)
                {
                    await NavigationService.NavigateToAsync("//Main/Catalog");
                }
            });
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        await IsBusyFor(
            async () =>
            {
                var loginSuccess = await _appEnvironmentService.IdentityService.SignInAsync();

                if (loginSuccess)
                {
                    await NavigationService.NavigateToAsync("//Main/Catalog");
                }
            });
    }

    [RelayCommand]
    private Task RegisterAsync()
    {
        return _openUrlService.OpenUrl(_settingsService.RegistrationEndpoint);
    }

    [RelayCommand]
    private async Task PerformLogoutAsync()
    {
        await _appEnvironmentService.IdentityService.SignOutAsync();

        _settingsService.UseFakeLocation = false;

        UserName.Value = string.Empty;
        Password.Value = string.Empty;
    }

    [RelayCommand]
    private Task SettingsAsync()
    {
        return NavigationService.NavigateToAsync("Settings");
    }

    [RelayCommand]
    private void Validate()
    {
        IsValid = UserName.Validate() && Password.Validate();
    }

    private void AddValidations()
    {
        UserName.Validations.Add(new IsNotNullOrEmptyRule<string> {ValidationMessage = "A username is required."});
        Password.Validations.Add(new IsNotNullOrEmptyRule<string> {ValidationMessage = "A password is required."});
    }

    public void InvalidateMock()
    {
        IsMock = false; //_settingsService.UseMocks;
    }
}
