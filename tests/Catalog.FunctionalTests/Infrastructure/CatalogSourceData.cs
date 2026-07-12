using System.IO;
using System.Text.Json;

using eShop.Catalog.API.Model;

namespace eShop.Catalog.FunctionalTests.Infrastructure;

internal sealed class CatalogSourceEntry
{
    public int Id { get; set; }
    public string? Type { get; set; }
    public string? Brand { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
}

internal static class CatalogSourceData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<List<CatalogSourceEntry>> LoadSourceEntriesAsync()
    {
        var catalogJsonPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Catalog.API", "Setup", "catalog.json"));
        var sourceJson = await File.ReadAllTextAsync(catalogJsonPath);
        return JsonSerializer.Deserialize<CatalogSourceEntry[]>(sourceJson, JsonOptions)?.ToList() ?? [];
    }

    public static CatalogItem CreateCatalogItem(CatalogSourceEntry source, int brandId, int typeId) => new(source.Name!)
    {
        Id = source.Id,
        Description = source.Description,
        Price = source.Price,
        CatalogBrandId = brandId,
        CatalogTypeId = typeId,
        AvailableStock = 100,
        MaxStockThreshold = 200,
        RestockThreshold = 10,
        PictureFileName = $"{source.Id}.webp",
    };

    public static CatalogItem CloneItem(CatalogItem item) => new(item.Name)
    {
        Id = item.Id,
        Description = item.Description,
        Price = item.Price,
        PictureFileName = item.PictureFileName,
        CatalogTypeId = item.CatalogTypeId,
        CatalogBrandId = item.CatalogBrandId,
        AvailableStock = item.AvailableStock,
        RestockThreshold = item.RestockThreshold,
        MaxStockThreshold = item.MaxStockThreshold,
        OnReorder = item.OnReorder,
        Embedding = item.Embedding,
    };
}
