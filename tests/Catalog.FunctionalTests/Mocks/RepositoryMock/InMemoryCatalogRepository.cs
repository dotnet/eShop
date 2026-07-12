using System.Threading;

using eShop.Catalog.API.Infrastructure.Repositories;
using eShop.Catalog.API.Model;
using eShop.Catalog.FunctionalTests.Infrastructure;

using Pgvector;

namespace eShop.Catalog.FunctionalTests.Mocks.RepositoryMock;

internal sealed class InMemoryCatalogRepository(CatalogRepositoryMockStore store) : ICatalogRepository
{
    public Task<PaginatedItems<CatalogItem>> GetItemsAsync(int pageIndex, int pageSize, string? name, int? type, int? brand, CancellationToken cancellationToken = default)
    {
        var query = store.Items.AsEnumerable();

        if (name is not null)
        {
            query = query.Where(item => item.Name.StartsWith(name, StringComparison.Ordinal));
        }

        if (type is not null)
        {
            query = query.Where(item => item.CatalogTypeId == type);
        }

        if (brand is not null)
        {
            query = query.Where(item => item.CatalogBrandId == brand);
        }

        var ordered = query.OrderBy(item => item.Name).ToList();
        return Task.FromResult(new PaginatedItems<CatalogItem>(
            pageIndex,
            pageSize,
            ordered.LongCount(),
            ordered.Skip(pageSize * pageIndex).Take(pageSize).Select(CatalogSourceData.CloneItem).ToList()));
    }

    public Task<List<CatalogItem>> GetItemsByIdsAsync(int[] ids, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Items.Where(item => ids.Contains(item.Id)).Select(CatalogSourceData.CloneItem).ToList());

    public Task<CatalogItem?> GetItemByIdAsync(int id, bool includeBrand = false, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetItemById(id));

    public Task<List<CatalogType>> GetTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Types.Select(type => new CatalogType(type.Type) { Id = type.Id }).ToList());

    public Task<List<CatalogBrand>> GetBrandsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Brands.Select(brand => new CatalogBrand(brand.Brand) { Id = brand.Id }).ToList());

    public Task<List<CatalogItem>> GetItemsBySemanticRelevanceAsync(Vector vector, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = store.Items.OrderBy(item => item.Name).Skip(pageSize * pageIndex).Take(pageSize).Select(CatalogSourceData.CloneItem).ToList();
        return Task.FromResult(items);
    }

    public Task<List<(CatalogItem Item, double Distance)>> GetItemsBySemanticRelevanceWithDistanceAsync(Vector vector, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = store.Items
            .OrderBy(item => item.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .Select(item => (CatalogSourceData.CloneItem(item), 0d))
            .ToList();

        return Task.FromResult(items);
    }

    public Task<long> GetItemsCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((long)store.Items.Count);

    public Task AddAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        store.Upsert(item);
        return Task.CompletedTask;
    }

    public void Remove(CatalogItem item) => store.Remove(item.Id);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
