namespace eShop.ClientApp.UnitTests.ViewModels;

public class MarketingViewModelTests
{
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    public MarketingViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _settingsService = new MockSettingsService();

        var mockBasketService = new BasketMockService();
        var mockCampaignService = new CampaignMockService();
        var mockCatalogService = new CatalogMockService();
        var mockOrderService = new OrderMockService();
        var mockUserService = new UserMockService();

        _appEnvironmentService =
            new AppEnvironmentService(
                mockBasketService, mockBasketService,
                mockCampaignService, mockCampaignService,
                mockCatalogService, mockCatalogService,
                mockOrderService, mockOrderService,
                mockUserService, mockUserService);

        _appEnvironmentService.UpdateDependencies(true);
    }

    [Fact]
    public void GetCampaignsIsEmptyTest()
    {
        var campaignViewModel = new CampaignViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Empty(campaignViewModel.Campaigns);
    }

    [Fact]
    public async Task GetCampaignsIsNotNullTest()
    {
        var campaignViewModel = new CampaignViewModel(_appEnvironmentService, _navigationService, _settingsService);

        await campaignViewModel.InitializeAsync();

        Assert.NotNull(campaignViewModel.Campaigns);
    }

    [Fact]
    public void GetCampaignDetailsCommandIsNotNullTest()
    {
        var campaignViewModel = new CampaignViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.NotNull(campaignViewModel.GetCampaignDetailsCommand);
    }

    [Fact]
    public void GetCampaignDetailsByIdIsNullTest()
    {
        var campaignDetailsViewModel = new CampaignDetailsViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Null(campaignDetailsViewModel.Campaign);
    }

    [Fact]
    public async Task GetCampaignDetailsByIdIsNotNullTest()
    {
        var campaignDetailsViewModel = new CampaignDetailsViewModel(_appEnvironmentService, _navigationService, _settingsService)
        {
            CampaignId = 1
        };
        await campaignDetailsViewModel.InitializeAsync();

        Assert.NotNull(campaignDetailsViewModel.Campaign);
    }
}
