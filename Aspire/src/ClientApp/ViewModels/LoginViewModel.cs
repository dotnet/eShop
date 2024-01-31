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

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);

        if (query.ValueAsBool("Logout") == true)
        {
            PerformLogout();
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
                    _settingsService.AuthAccessToken = GlobalSetting.Instance.AuthToken;

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
                await Task.Delay(10);

                LoginUrl = _identityService.CreateAuthorizationRequest();

                IsValid = true;
                IsLogin = true;
            });
    }

    [RelayCommand]
    private Task RegisterAsync()
    {
        return _openUrlService.OpenUrl(GlobalSetting.Instance.RegisterWebsite);
    }

    [RelayCommand]
    private void PerformLogout()
    {
        var authIdToken = _settingsService.AuthIdToken;
        var logoutRequest = _identityService.CreateLogoutRequest(authIdToken);

        if (!string.IsNullOrEmpty(logoutRequest))
        {
            // Logout
            LoginUrl = logoutRequest;
        }

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
    private async Task NavigateAsync(string url)
    {
        var unescapedUrl = System.Net.WebUtility.UrlDecode(url);

        if (unescapedUrl.Equals(GlobalSetting.Instance.LogoutCallback, StringComparison.OrdinalIgnoreCase))
        {
            _settingsService.AuthAccessToken = string.Empty;
            _settingsService.AuthIdToken = string.Empty;
            IsLogin = false;
            LoginUrl = _identityService.CreateAuthorizationRequest();
        }
        else if (unescapedUrl.Contains(GlobalSetting.Instance.Callback, StringComparison.OrdinalIgnoreCase))
        {
            var authResponse = new AuthorizeResponse(url);
            if (!string.IsNullOrWhiteSpace(authResponse.Code))
            {
                var userToken = await _identityService.GetTokenAsync(authResponse.Code);
                string accessToken = userToken.AccessToken;

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    _settingsService.AuthAccessToken = accessToken;
                    _settingsService.AuthIdToken = authResponse.IdentityToken;
                    await NavigationService.NavigateToAsync("//Main/Catalog");
                }
            }
        }
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
        IsMock = _settingsService.UseMocks;
    }
}
