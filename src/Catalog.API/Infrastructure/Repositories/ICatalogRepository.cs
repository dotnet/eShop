using Pgvector;

namespace eShop.Catalog.API.Infrastructure.Repositories;

public interface ICatalogRepository
{
    Task<PaginatedItems<CatalogItem>> GetItemsAsync(int pageIndex, int pageSize, string? name, int? type, int? brand, CancellationToken cancellationToken = default);
    Task<List<CatalogItem>> GetItemsByIdsAsync(int[] ids, CancellationToken cancellationToken = default);
    Task<CatalogItem?> GetItemByIdAsync(int id, bool includeBrand = false, CancellationToken cancellationToken = default);
    Task<List<CatalogType>> GetTypesAsync(CancellationToken cancellationToken = default);
    Task<List<CatalogBrand>> GetBrandsAsync(CancellationToken cancellationToken = default);
    Task<List<CatalogItem>> GetItemsBySemanticRelevanceAsync(Vector vector, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<List<(CatalogItem Item, double Distance)>> GetItemsBySemanticRelevanceWithDistanceAsync(Vector vector, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<long> GetItemsCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CatalogItem item, CancellationToken cancellationToken = default);
    void Remove(CatalogItem item);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
