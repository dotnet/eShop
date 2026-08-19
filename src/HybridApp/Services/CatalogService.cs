using System.Net.Http.Json;
using System.Web;
using eShop.WebAppComponents.Catalog;
using eShop.WebAppComponents.Services;

namespace eShop.HybridApp.Services;

public class CatalogService(HttpClient httpClient) : ICatalogService
{
    private readonly string remoteServiceBaseUrl = "api/catalog/";


    public Task<CatalogItem?> GetCatalogItem(int id)
    {
        var uri = $"{remoteServiceBaseUrl}items/{id}?api-version=2.0";
        return httpClient.GetFromJsonAsync(uri, CatalogJsonContext.Default.CatalogItem);
    }

    public async Task<CatalogResult> GetCatalogItems(int pageIndex, int pageSize, int[]? brands, int[]? types)
    {
        var uri = GetAllCatalogItemsUri(remoteServiceBaseUrl, pageIndex, pageSize, brands, types);
        var result = await httpClient.GetFromJsonAsync($"{uri}&api-version=2.0", CatalogJsonContext.Default.CatalogResult);
        return result!;
    }

    public async Task<List<CatalogItem>> GetCatalogItems(IEnumerable<int> ids)
    {
        var uri = $"{remoteServiceBaseUrl}items/by?ids={string.Join("&ids=", ids)}&api-version=2.0";
        var result = await httpClient.GetFromJsonAsync(uri, CatalogJsonContext.Default.ListCatalogItem);
        return result!;
    }

    public Task<CatalogResult> GetCatalogItemsWithSemanticRelevance(int page, int take, string text)
    {
        var url = $"{remoteServiceBaseUrl}items/withsemanticrelevance?text={HttpUtility.UrlEncode(text)}&pageIndex={page}&pageSize={take}&api-version=2.0";
        var result = httpClient.GetFromJsonAsync(url, CatalogJsonContext.Default.CatalogResult);
        return result!;
    }

    public async Task<IEnumerable<CatalogBrand>> GetBrands()
    {
        var uri = $"{remoteServiceBaseUrl}catalogBrands?api-version=2.0";
        var result = await httpClient.GetFromJsonAsync(uri, CatalogJsonContext.Default.CatalogBrandArray);
        return result!;
    }

    public async Task<IEnumerable<CatalogItemType>> GetTypes()
    {
        var uri = $"{remoteServiceBaseUrl}catalogTypes?api-version=2.0";
        var result = await httpClient.GetFromJsonAsync(uri, CatalogJsonContext.Default.CatalogItemTypeArray);
        return result!;
    }

    private static string GetAllCatalogItemsUri(string baseUri, int pageIndex, int pageSize, int[]? brands, int[]? types)
    {
        string filterQs = string.Empty;

        if (types is { Length: > 0 })
        {
            filterQs += string.Join("&", types.Select(t => $"type={t}")) + "&";
        }
        if (brands is { Length: > 0 })
        {
            filterQs += string.Join("&", brands.Select(b => $"brand={b}")) + "&";
        }

        return $"{baseUri}items?{filterQs}pageIndex={pageIndex}&pageSize={pageSize}&api-version=2.0";
    }
}
