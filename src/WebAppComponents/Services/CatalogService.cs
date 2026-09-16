using Catalog.SDK;
using eShop.WebAppComponents.Catalog;
using BrandsApiVersion = Catalog.SDK.Api.Catalog.Catalogbrands.GetApiVersionQueryParameterType;
using ItemApiVersion = Catalog.SDK.Api.Catalog.Items.Item.GetApiVersionQueryParameterType;
using ItemsApiVersion = Catalog.SDK.Api.Catalog.Items.GetApiVersionQueryParameterType;
using ItemsByApiVersion = Catalog.SDK.Api.Catalog.Items.By.GetApiVersionQueryParameterType;
using SemanticRelevanceApiVersion = Catalog.SDK.Api.Catalog.Items.Withsemanticrelevance.GetApiVersionQueryParameterType;
using SdkCatalogItem = Catalog.SDK.Models.CatalogItem;
using TypesApiVersion = Catalog.SDK.Api.Catalog.Catalogtypes.GetApiVersionQueryParameterType;

namespace eShop.WebAppComponents.Services;

public class CatalogService(CatalogClient catalogClient) : ICatalogService
{
    public async Task<CatalogItem?> GetCatalogItem(int id, CancellationToken cancellationToken = default)
    {
        var item = await catalogClient.Api.Catalog.Items[id].GetAsync(request =>
            request.QueryParameters.ApiVersionAsGetApiVersionQueryParameterType = ItemApiVersion.TwoZero,
            cancellationToken);

        return item is null ? null : Map(item);
    }

    public async Task<CatalogResult> GetCatalogItems(int pageIndex, int pageSize, int? brand, int? type, CancellationToken cancellationToken = default)
    {
        var result = await catalogClient.Api.Catalog.Items.GetAsync(request =>
        {
            request.QueryParameters.ApiVersionAsGetApiVersionQueryParameterType = ItemsApiVersion.TwoZero;
            request.QueryParameters.PageIndex = pageIndex;
            request.QueryParameters.PageSize = pageSize;
            request.QueryParameters.Brand = brand;
            request.QueryParameters.Type = type;
        }, cancellationToken);

        return Map(result);
    }

    public async Task<List<CatalogItem>> GetCatalogItems(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var items = await catalogClient.Api.Catalog.Items.By.GetAsync(request =>
        {
            request.QueryParameters.ApiVersionAsGetApiVersionQueryParameterType = ItemsByApiVersion.TwoZero;
            request.QueryParameters.Ids = ids.Select(id => (int?)id).ToArray();
        }, cancellationToken);

        return items?.Select(Map).ToList() ?? [];
    }

    public async Task<CatalogResult> GetCatalogItemsWithSemanticRelevance(int page, int take, string text, CancellationToken cancellationToken = default)
    {
        var result = await catalogClient.Api.Catalog.Items.Withsemanticrelevance.GetAsync(request =>
        {
            request.QueryParameters.ApiVersionAsGetApiVersionQueryParameterType = SemanticRelevanceApiVersion.TwoZero;
            request.QueryParameters.PageIndex = page;
            request.QueryParameters.PageSize = take;
            request.QueryParameters.Text = text;
        }, cancellationToken);

        return Map(result);
    }

    public async Task<IEnumerable<CatalogBrand>> GetBrands(CancellationToken cancellationToken = default)
    {
        var brands = await catalogClient.Api.Catalog.Catalogbrands.GetAsync(request =>
            request.QueryParameters.ApiVersionAsGetApiVersionQueryParameterType = BrandsApiVersion.TwoZero,
            cancellationToken);

        return brands?.Select(brand => new CatalogBrand(brand.Id ?? 0, brand.Brand ?? string.Empty)) ?? [];
    }

    public async Task<IEnumerable<CatalogItemType>> GetTypes(CancellationToken cancellationToken = default)
    {
        var types = await catalogClient.Api.Catalog.Catalogtypes.GetAsync(request =>
            request.QueryParameters.ApiVersionAsGetApiVersionQueryParameterType = TypesApiVersion.TwoZero,
            cancellationToken);

        return types?.Select(type => new CatalogItemType(type.Id ?? 0, type.Type ?? string.Empty)) ?? [];
    }

    private static CatalogResult Map(global::Catalog.SDK.Models.PaginatedItemsOfCatalogItem? result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var items = result.Data?.Select(Map).ToList() ?? [];
        return new CatalogResult(
            result.PageIndex ?? 0,
            result.PageSize ?? items.Count,
            checked((int)(result.Count ?? items.Count)),
            items);
    }

    private static CatalogItem Map(SdkCatalogItem item)
    {
        return new CatalogItem(
            item.Id ?? 0,
            item.Name ?? string.Empty,
            item.Description ?? string.Empty,
            Convert.ToDecimal(item.Price),
            item.PictureFileName ?? string.Empty,
            item.CatalogBrandId ?? 0,
            new CatalogBrand(item.CatalogBrand?.Id ?? item.CatalogBrandId ?? 0, item.CatalogBrand?.Brand ?? string.Empty),
            item.CatalogTypeId ?? 0,
            new CatalogItemType(item.CatalogType?.Id ?? item.CatalogTypeId ?? 0, item.CatalogType?.Type ?? string.Empty));
    }
}
