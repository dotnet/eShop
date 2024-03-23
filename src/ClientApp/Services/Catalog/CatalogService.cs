using eShop.ClientApp.Models.Catalog;
using eShop.ClientApp.Services.RequestProvider;
using eShop.ClientApp.Services.FixUri;
using eShop.ClientApp.Helpers;
using eShop.ClientApp.Services.Settings;

namespace eShop.ClientApp.Services.Catalog;

public class CatalogService : ICatalogService
{
    private readonly ISettingsService _settingsService;
    private readonly IRequestProvider _requestProvider;
    private readonly IFixUriService _fixUriService;

    private const string ApiUrlBase = "api/v1/Catalog";

    public CatalogService(ISettingsService settingsService, IRequestProvider requestProvider, IFixUriService fixUriService)
    {
        _settingsService = settingsService;
        _requestProvider = requestProvider;
        _fixUriService = fixUriService;
    }

    public async Task<IEnumerable<CatalogItem>> FilterAsync(int catalogBrandId, int catalogTypeId)
    {
        var uri = UriHelper.CombineUri(_settingsService.GatewayCatalogEndpointBase, $"{ApiUrlBase}/items/type/{catalogTypeId}/brand/{catalogBrandId}");

        CatalogRoot catalog = await _requestProvider.GetAsync<CatalogRoot>(uri).ConfigureAwait(false);

        return catalog?.Data ?? Enumerable.Empty<CatalogItem>();
    }

    public async Task<IEnumerable<CatalogItem>> GetCatalogAsync()
    {
        var uri = UriHelper.CombineUri(_settingsService.GatewayCatalogEndpointBase, $"{ApiUrlBase}/items?PageSize=100");

        CatalogRoot catalog = await _requestProvider.GetAsync<CatalogRoot>(uri).ConfigureAwait(false);

        if (catalog?.Data != null)
        {
            _fixUriService.FixCatalogItemPictureUri(catalog.Data);
            return catalog.Data;
        }
        else
            return Enumerable.Empty<CatalogItem>();
    }

    public async Task<CatalogItem> GetCatalogItemAsync(int catalogItemId)
    {
        var uri = UriHelper.CombineUri(_settingsService.GatewayCatalogEndpointBase, $"{ApiUrlBase}/items/{catalogItemId}");

        var catalogItem = await _requestProvider.GetAsync<CatalogItem>(uri).ConfigureAwait(false);

        if (catalogItem != null)
        {
            _fixUriService.FixCatalogItemPictureUri(new [] { catalogItem });
            return catalogItem;
        }
        else
            return default;
    }
    
    public async Task<IEnumerable<CatalogBrand>> GetCatalogBrandAsync()
    {
        var uri = UriHelper.CombineUri(_settingsService.GatewayCatalogEndpointBase, $"{ApiUrlBase}/catalogbrands");

        IEnumerable<CatalogBrand> brands = await _requestProvider.GetAsync<IEnumerable<CatalogBrand>>(uri).ConfigureAwait(false);

        return brands?.ToArray() ?? Enumerable.Empty<CatalogBrand>();
    }

    public async Task<IEnumerable<CatalogType>> GetCatalogTypeAsync()
    {
        var uri = UriHelper.CombineUri(_settingsService.GatewayCatalogEndpointBase, $"{ApiUrlBase}/catalogtypes");

        IEnumerable<CatalogType> types = await _requestProvider.GetAsync<IEnumerable<CatalogType>>(uri).ConfigureAwait(false);

        return types?.ToArray() ?? Enumerable.Empty<CatalogType>();
    }
}
