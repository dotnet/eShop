using eShop.Catalog.API.Model;

namespace eShop.BddTests.Support;

public class InMemoryCatalogRepository
{
    private readonly Dictionary<int, CatalogItem> _products = new();
    private readonly List<int> _seededIds = new();

    public async Task<CatalogItem?> GetProductByIdAsync(int productId)
    {
        _products.TryGetValue(productId, out var product);
        return await Task.FromResult(product);
    }

    public async Task<IEnumerable<CatalogItem>> GetAllProductsAsync()
    {
        return await Task.FromResult(_products.Values.AsEnumerable());
    }

    public async Task SaveProductAsync(CatalogItem product)
    {
        _products[product.Id] = product;
        await Task.CompletedTask;
    }

    public async Task SeedTestDataAsync()
    {
        var testProducts = new[]
        {
            CreateTestProduct(1, "Alpine Hiking Jacket", "Waterproof hiking jacket for alpine conditions", 199.99m, 1, 1),
            CreateTestProduct(2, "Trail Running Boots", "Lightweight boots for trail running", 149.99m, 2, 1),
            CreateTestProduct(3, "Expedition Backpack", "Large capacity backpack for multi-day expeditions", 299.99m, 3, 2),
            CreateTestProduct(4, "Camping Tent", "4-person waterproof camping tent", 399.99m, 4, 2),
            CreateTestProduct(5, "Hiking Poles", "Adjustable carbon fiber hiking poles", 89.99m, 5, 3),
            CreateTestProduct(6, "Outdoor Jacket", "All-weather outdoor jacket", 179.99m, 1, 1),
            CreateTestProduct(7, "Mountain Boot", "Heavy-duty mountain climbing boot", 249.99m, 2, 1),
            CreateTestProduct(8, "Day Backpack", "Compact backpack for day hikes", 79.99m, 3, 2),
            CreateTestProduct(9, "Sleeping Tent", "Lightweight 2-person tent", 199.99m, 4, 2),
            CreateTestProduct(10, "Trekking Poles", "Aluminum trekking poles with shock absorption", 59.99m, 5, 3)
        };

        foreach (var product in testProducts)
        {
            await SaveProductAsync(product);
            TrackSeededId(product.Id);
        }
    }

    public async Task ClearSeededDataAsync()
    {
        foreach (var id in _seededIds.ToList())
        {
            if (_products.ContainsKey(id))
            {
                _products.Remove(id);
                _seededIds.Remove(id);
            }
        }
        await Task.CompletedTask;
    }

    public async Task ClearScenarioDataAsync()
    {
        // Only clear scenario-specific data, preserve seeded data
        await Task.CompletedTask;
    }

    public void TrackSeededId(int id)
    {
        if (!_seededIds.Contains(id))
        {
            _seededIds.Add(id);
        }
    }

    private static CatalogItem CreateTestProduct(
        int id,
        string name,
        string description,
        decimal price,
        int catalogTypeId,
        int catalogBrandId)
    {
        return new CatalogItem
        {
            Id = id,
            Name = name,
            Description = description,
            Price = price,
            PictureFileName = $"product{id}.jpg",
            CatalogTypeId = catalogTypeId,
            CatalogBrandId = catalogBrandId,
            AvailableStock = 100,
            RestockThreshold = 10,
            MaxStockThreshold = 200
        };
    }
}