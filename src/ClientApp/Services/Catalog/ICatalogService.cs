using eShop.ClientApp.Models.Catalog;

namespace eShop.ClientApp.Services.Catalog;

public interface ICatalogService
{
    Task<IEnumerable<CatalogBrand>> GetCatalogBrandAsync();
    Task<IEnumerable<CatalogItem>> FilterAsync(int catalogBrandId, int catalogTypeId);
    Task<IEnumerable<CatalogType>> GetCatalogTypeAsync();
    Task<IEnumerable<CatalogItem>> GetCatalogAsync();

    Task<CatalogItem> GetCatalogItemAsync(int catalogItemId);
}
