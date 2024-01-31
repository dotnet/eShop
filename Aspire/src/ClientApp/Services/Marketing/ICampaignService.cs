using eShop.ClientApp.Models.Marketing;

namespace eShop.ClientApp.Services.Marketing;

public interface ICampaignService
{
    Task<IEnumerable<CampaignItem>> GetAllCampaignsAsync(string token);
    Task<CampaignItem> GetCampaignByIdAsync(int id, string token);
}
