using System.Collections.Generic;
using System.Threading.Tasks;
using eShop.WebAppComponents.Catalog;

namespace eShop.WebAppComponents.Services
{
    public interface ICatalogService
    {
        Task<CatalogItem?> GetCatalogItem(int id, CancellationToken cancellationToken = default);
        Task<CatalogResult> GetCatalogItems(int pageIndex, int pageSize, int? brand, int? type, CancellationToken cancellationToken = default);
        Task<List<CatalogItem>> GetCatalogItems(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<CatalogResult> GetCatalogItemsWithSemanticRelevance(int page, int take, string text, CancellationToken cancellationToken = default);
        Task<IEnumerable<CatalogBrand>> GetBrands(CancellationToken cancellationToken = default);
        Task<IEnumerable<CatalogItemType>> GetTypes(CancellationToken cancellationToken = default);
    }
}
