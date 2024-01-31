using eShop.ClientApp.Models.Marketing;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class CampaignViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly ObservableCollectionEx<CampaignItem> _campaigns = new ();

    public IReadOnlyList<CampaignItem> Campaigns => _campaigns;

    public CampaignViewModel(
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
                // Get campaigns by user
                var campaigns = await _appEnvironmentService.CampaignService.GetAllCampaignsAsync(_settingsService.AuthAccessToken);
                _campaigns.ReloadData(campaigns);
            });
    }

    [RelayCommand]
    private async Task GetCampaignDetailsAsync(CampaignItem campaign)
    {
        if (campaign is null)
        {
            return;
        }

        await NavigationService.NavigateToAsync(
            "CampaignDetails",
            new Dictionary<string, object> { { nameof(Campaign.Id), campaign.Id } });
    }
}
