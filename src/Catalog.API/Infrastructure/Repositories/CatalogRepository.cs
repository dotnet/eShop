using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace eShop.Catalog.API.Infrastructure.Repositories;

public sealed class CatalogRepository(CatalogContext context) : ICatalogRepository
{
    public async Task<PaginatedItems<CatalogItem>> GetItemsAsync(int pageIndex, int pageSize, string? name, int? type, int? brand, CancellationToken cancellationToken = default)
    {
        var root = ApplyFilters(context.CatalogItems.AsQueryable(), name, type, brand);

        var totalItems = await root.LongCountAsync(cancellationToken);
        var itemsOnPage = await root
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage);
    }

    public Task<List<CatalogItem>> GetItemsByIdsAsync(int[] ids, CancellationToken cancellationToken = default) =>
        context.CatalogItems
            .Where(item => ids.Contains(item.Id))
            .ToListAsync(cancellationToken);

    public Task<CatalogItem?> GetItemByIdAsync(int id, bool includeBrand = false, CancellationToken cancellationToken = default)
    {
        IQueryable<CatalogItem> query = context.CatalogItems;

        if (includeBrand)
        {
            query = query.Include(ci => ci.CatalogBrand);
        }

        return query.SingleOrDefaultAsync(ci => ci.Id == id, cancellationToken);
    }

    public Task<List<CatalogType>> GetTypesAsync(CancellationToken cancellationToken = default) =>
        context.CatalogTypes.OrderBy(x => x.Type).ToListAsync(cancellationToken);

    public Task<List<CatalogBrand>> GetBrandsAsync(CancellationToken cancellationToken = default) =>
        context.CatalogBrands.OrderBy(x => x.Brand).ToListAsync(cancellationToken);

    public Task<List<CatalogItem>> GetItemsBySemanticRelevanceAsync(Vector vector, int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
        context.CatalogItems
            .Where(c => c.Embedding != null)
            .OrderBy(c => c.Embedding!.CosineDistance(vector))
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<List<(CatalogItem Item, double Distance)>> GetItemsBySemanticRelevanceWithDistanceAsync(Vector vector, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = await context.CatalogItems
            .Where(c => c.Embedding != null)
            .Select(c => new { Item = c, Distance = c.Embedding!.CosineDistance(vector) })
            .OrderBy(c => c.Distance)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items.Select(i => (i.Item, i.Distance)).ToList();
    }

    public Task<long> GetItemsCountAsync(CancellationToken cancellationToken = default) =>
        context.CatalogItems.LongCountAsync(cancellationToken);

    public Task AddAsync(CatalogItem item, CancellationToken cancellationToken = default) =>
        context.CatalogItems.AddAsync(item, cancellationToken).AsTask();

    public void Remove(CatalogItem item) => context.CatalogItems.Remove(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    private static IQueryable<CatalogItem> ApplyFilters(IQueryable<CatalogItem> query, string? name, int? type, int? brand)
    {
        if (name is not null)
        {
            query = query.Where(c => c.Name.StartsWith(name));
        }

        if (type is not null)
        {
            query = query.Where(c => c.CatalogTypeId == type);
        }

        if (brand is not null)
        {
            query = query.Where(c => c.CatalogBrandId == brand);
        }

        return query;
    }
}
