using System.Diagnostics;
using System.Text.RegularExpressions;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Models.Catalog;
using eShop.ClientApp.Models.Marketing;
using eShop.ClientApp.Services.Settings;

namespace eShop.ClientApp.Services.FixUri;

public class FixUriService : IFixUriService
{
    private const string ApiVersion = "api-version=1.0";
    
    private readonly ISettingsService _settingsService;

    private readonly Regex IpRegex = new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");

    public FixUriService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void FixCatalogItemPictureUri(IEnumerable<CatalogItem> catalogItems)
    {
        if (catalogItems is null)
        {
            return;
        }

        try
        {
            if (!_settingsService.UseMocks && _settingsService.GatewayCatalogEndpointBase != _settingsService.DefaultEndpoint)
            {
                foreach (var catalogItem in catalogItems)
                {
                    catalogItem.PictureUri = Path.Combine(_settingsService.GatewayCatalogEndpointBase, $"api/catalog/items/{catalogItem.Id}/pic?{ApiVersion}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public void FixBasketItemPictureUri(IEnumerable<BasketItem> basketItems)
    {
        if (basketItems is null)
        {
            return;
        }

        try
        {
            if (!_settingsService.UseMocks && _settingsService.IdentityEndpointBase != _settingsService.DefaultEndpoint)
            {
                foreach (var basketItem in basketItems)
                {
                    var serverResult = IpRegex.Matches(basketItem.PictureUrl);
                    var localResult = IpRegex.Matches(_settingsService.IdentityEndpointBase);

                    if (serverResult.Count != -1 && localResult.Count != -1)
                    {
                        var serviceIp = serverResult[0].Value;
                        var localIp = localResult[0].Value;
                        basketItem.PictureUrl = basketItem.PictureUrl.Replace(serviceIp, localIp);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public void FixCampaignItemPictureUri(IEnumerable<CampaignItem> campaignItems)
    {
        if (campaignItems is null)
        {
            return;
        }

        try
        {
            if (!_settingsService.UseMocks && _settingsService.IdentityEndpointBase != _settingsService.DefaultEndpoint)
            {
                foreach (var campaignItem in campaignItems)
                {
                    var serverResult = IpRegex.Matches(campaignItem.PictureUri);
                    var localResult = IpRegex.Matches(_settingsService.IdentityEndpointBase);

                    if (serverResult.Count != -1 && localResult.Count != -1)
                    {
                        var serviceIp = serverResult[0].Value;
                        var localIp = localResult[0].Value;

                        campaignItem.PictureUri = campaignItem.PictureUri.Replace(serviceIp, localIp);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
