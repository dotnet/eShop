using System.Diagnostics;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.OpenUrl;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.Validations;
using eShop.ClientApp.ViewModels.Base;
using IdentityModel.Client;

namespace eShop.ClientApp.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IOpenUrlService _openUrlService;
    private readonly IIdentityService _identityService;

    [ObservableProperty]
    private ValidatableObject<string> _userName = new();

    [ObservableProperty]
    ValidatableObject<string> _password = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MockSignInCommand))]
    private bool _isValid;

    [ObservableProperty]
    private bool _isMock;

    [ObservableProperty]
    private bool _isLogin;

    [ObservableProperty]
    private string _loginUrl;

    public LoginViewModel(
        IOpenUrlService openUrlService, IIdentityService identityService,
        INavigationService navigationService, ISettingsService settingsService)
        : base(navigationService)
    {
        _settingsService = settingsService;
        _openUrlService = openUrlService;
        _identityService = identityService;

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
                bool isAuthenticated = false;

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
                var loginSuccess = await _identityService.SignInAsync();

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
        var logoutRequest = await _identityService.SignOutAsync();
        
        if (_settingsService.UseMocks)
        {
            _settingsService.AuthAccessToken = string.Empty;
            _settingsService.AuthIdToken = string.Empty;
        }

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
        UserName.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "A username is required." });
        Password.Validations.Add(new IsNotNullOrEmptyRule<string> { ValidationMessage = "A password is required." });
    }

    public void InvalidateMock()
    {
        IsMock = false; //_settingsService.UseMocks;
    }
}
