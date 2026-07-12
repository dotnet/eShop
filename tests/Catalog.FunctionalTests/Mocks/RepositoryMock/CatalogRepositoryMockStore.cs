using eShop.Catalog.API.Model;
using eShop.Catalog.FunctionalTests.Infrastructure;

namespace eShop.Catalog.FunctionalTests.Mocks.RepositoryMock;

internal sealed class CatalogRepositoryMockStore
{
    public List<CatalogItem> Items { get; private set; } = [];
    public List<CatalogBrand> Brands { get; private set; } = [];
    public List<CatalogType> Types { get; private set; } = [];

    public async Task ResetAsync()
    {
        var sourceItems = await CatalogSourceData.LoadSourceEntriesAsync();

        Brands = sourceItems
            .Select(x => x.Brand)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Select((name, index) => new CatalogBrand(name!) { Id = index + 1 })
            .ToList();

        Types = sourceItems
            .Select(x => x.Type)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Select((name, index) => new CatalogType(name!) { Id = index + 1 })
            .ToList();

        var brandsByName = Brands.ToDictionary(x => x.Brand, x => x.Id);
        var typesByName = Types.ToDictionary(x => x.Type, x => x.Id);

        Items = sourceItems
            .Where(source => source.Name is not null && source.Brand is not null && source.Type is not null)
            .Select(source => CatalogSourceData.CreateCatalogItem(source, brandsByName[source.Brand!], typesByName[source.Type!]))
            .ToList();
    }

    public CatalogItem? GetItemById(int id) => Items.SingleOrDefault(item => item.Id == id);

    public void Upsert(CatalogItem item)
    {
        var existingIndex = Items.FindIndex(existing => existing.Id == item.Id);
        if (existingIndex >= 0)
        {
            Items[existingIndex] = CatalogSourceData.CloneItem(item);
        }
        else
        {
            Items.Add(CatalogSourceData.CloneItem(item));
        }
    }

    public void Remove(int id)
    {
        Items.RemoveAll(item => item.Id == id);
    }
}
