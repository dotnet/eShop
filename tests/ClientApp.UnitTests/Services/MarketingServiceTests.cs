namespace eShop.ClientApp.UnitTests.Services;

public class MarketingServiceTests
{
    private readonly ISettingsService _settingsService;

    public MarketingServiceTests()
    {
        _settingsService = new MockSettingsService();
    }
    
    [Fact]
    public async Task GetFakeCampaigTest()
    {
        var campaignMockService = new CampaignMockService();
        var order = await campaignMockService.GetCampaignByIdAsync(1, _settingsService.AuthAccessToken);

        Assert.NotNull(order);
    }

    [Fact]
    public async Task GetFakeCampaignsTest()
    {
        var campaignMockService = new CampaignMockService();
        var result = await campaignMockService.GetAllCampaignsAsync(_settingsService.AuthAccessToken);

        Assert.NotEmpty(result);
    }
}
