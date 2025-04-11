using Inked.WebAppComponents.Catalog;

namespace Inked.WebAppComponents.Services;

public interface ICatalogService
{
    Task<CatalogItem?> GetCatalogItem(int id);
    Task<CatalogResult> GetCatalogItems(int pageIndex, int pageSize, int? brand, int? type);
    Task<List<CatalogItem>> GetCatalogItems(IEnumerable<int> ids);
    Task<CatalogResult> GetCatalogItemsWithSemanticRelevance(int page, int take, string text);
    Task<IEnumerable<CatalogBrand>> GetBrands();
    Task<IEnumerable<CatalogItemType>> GetTypes();
}
