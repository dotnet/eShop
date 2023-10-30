using eShop.ClientApp.Models.Marketing;

namespace eShop.ClientApp.Services.Marketing;

public class CampaignMockService : ICampaignService
{
    private readonly IEnumerable<CampaignItem> _mockCampaign =
        new[]
        {
            new CampaignItem
            {
                Id = Common.Common.MockCampaignId01,
                PictureUri = "fake_campaign_01.png",
                Name = ".NET Bot Black Hoodie 50% OFF",
                Description = "Campaign Description 1",
                From = DateTime.Now,
                To = DateTime.Now.AddDays(7)
            },

            new CampaignItem
            {
                Id = Common.Common.MockCampaignId02,
                PictureUri = "fake_campaign_02.png",
                Name = "Roslyn Red T-Shirt 3x2",
                Description = "Campaign Description 2",
                From = DateTime.Now.AddDays(-7),
                To = DateTime.Now.AddDays(14)
            }
        };

    public async Task<IEnumerable<CampaignItem>> GetAllCampaignsAsync(string token)
    {
        await Task.Delay(10);
        return _mockCampaign;
    }

    public async Task<CampaignItem> GetCampaignByIdAsync(int campaignId, string token)
    {
        await Task.Delay(10);
        return _mockCampaign.SingleOrDefault(c => c.Id == campaignId);
    }
}
