using eShop.ClientApp.Models.Marketing;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

[QueryProperty(nameof(CampaignId), "Id")]
public partial class CampaignDetailsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    [ObservableProperty]
    private CampaignItem _campaign;

    [ObservableProperty]
    private bool _isDetailsSite;

    [ObservableProperty]
    private int _campaignId;

    public CampaignDetailsViewModel(
        IAppEnvironmentService appEnvironmentService,
        INavigationService navigationService, ISettingsService settingsService)
        : base(navigationService)
    {
        _appEnvironmentService = appEnvironmentService;
        _settingsService = settingsService;
    }

    public override async Task InitializeAsync()
    {
        await IsBusyFor(
            async () =>
            {
                // Get campaign by id
                Campaign = await _appEnvironmentService.CampaignService.GetCampaignByIdAsync(CampaignId, _settingsService.AuthAccessToken);
            });
    }

    [RelayCommand]
    private void EnableDetailsSite()
    {
        IsDetailsSite = true;
    }
}
