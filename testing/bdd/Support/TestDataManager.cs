using eShop.Catalog.API.Model;
using eShop.Basket.API.Model;

namespace eShop.BddTests.Support;

public interface ITestDataManager
{
    Task SeedAllTestDataAsync();
    Task ClearAllTestDataAsync();
    Task ClearScenarioDataAsync();
    Task<List<CatalogItem>> GetSeededProductsAsync();
    Task<CustomerBasket> GetSeededBasketAsync(string customerId);
}

public class TestDataManager : ITestDataManager
{
    private readonly IInMemoryCatalogRepository _catalogRepository;
    private readonly IInMemoryBasketRepository _basketRepository;
    private readonly TestConfiguration _config;

    public TestDataManager(
        IInMemoryCatalogRepository catalogRepository,
        IInMemoryBasketRepository basketRepository,
        TestConfiguration config)
    {
        _catalogRepository = catalogRepository;
        _basketRepository = basketRepository;
        _config = config;
    }

    public async Task SeedAllTestDataAsync()
    {
        await SeedCatalogDataAsync();
        await SeedBasketDataAsync();
    }

    public async Task ClearAllTestDataAsync()
    {
        await _catalogRepository.ClearAllAsync();
        await _basketRepository.ClearAllAsync();
    }

    public async Task ClearScenarioDataAsync()
    {
        await _catalogRepository.ClearScenarioDataAsync();
        await _basketRepository.ClearScenarioDataAsync();
    }

    public async Task<List<CatalogItem>> GetSeededProductsAsync()
    {
        return await _catalogRepository.GetAllProductsAsync();
    }

    public async Task<CustomerBasket> GetSeededBasketAsync(string customerId)
    {
        return await _basketRepository.GetBasketAsync(customerId) ?? new CustomerBasket { BuyerId = customerId };
    }

    private async Task SeedCatalogDataAsync()
    {
        var catalogTypes = CreateCatalogTypes();
        var catalogBrands = CreateCatalogBrands();
        var catalogItems = CreateCatalogItems();

        foreach (var type in catalogTypes)
        {
            await _catalogRepository.AddCatalogTypeAsync(type);
        }

        foreach (var brand in catalogBrands)
        {
            await _catalogRepository.AddCatalogBrandAsync(brand);
        }

        foreach (var item in catalogItems)
        {
            await _catalogRepository.AddProductAsync(item);
        }
    }

    private async Task SeedBasketDataAsync()
    {
        // Seed some test baskets for scenarios
        var testBaskets = CreateTestBaskets();
        
        foreach (var basket in testBaskets)
        {
            await _basketRepository.UpdateBasketAsync(basket);
            _basketRepository.TrackSeededBasket(basket.BuyerId);
        }
    }

    private List<CatalogType> CreateCatalogTypes()
    {
        return new List<CatalogType>
        {
            new() { Id = 1, Type = "Jackets" },
            new() { Id = 2, Type = "Boots" },
            new() { Id = 3, Type = "Backpacks" },
            new() { Id = 4, Type = "Accessories" },
            new() { Id = 5, Type = "Tents" }
        };
    }

    private List<CatalogBrand> CreateCatalogBrands()
    {
        return new List<CatalogBrand>
        {
            new() { Id = 1, Brand = "AdventureWorks" },
            new() { Id = 2, Brand = "Mountain Peak" },
            new() { Id = 3, Brand = "Trail Blazer" },
            new() { Id = 4, Brand = "Outdoor Pro" }
        };
    }

    private List<CatalogItem> CreateCatalogItems()
    {
        return new List<CatalogItem>
        {
            new(1, 1, "Waterproof hiking jacket perfect for outdoor adventures", "Hiking Jacket", 199.99m, "jacket1.jpg")
            {
                Id = 1,
                AvailableStock = 50,
                RestockThreshold = 10,
                MaxStockThreshold = 100
            },
            new(2, 2, "Lightweight trail running shoes for all terrains", "Trail Running Shoes", 149.99m, "shoes1.jpg")
            {
                Id = 2,
                AvailableStock = 75,
                RestockThreshold = 15,
                MaxStockThreshold = 150
            },
            new(3, 1, "40L camping backpack with multiple compartments", "Camping Backpack", 89.99m, "backpack1.jpg")
            {
                Id = 3,
                AvailableStock = 30,
                RestockThreshold = 5,
                MaxStockThreshold = 60
            },
            new(5, 3, "3-season sleeping bag rated for comfort", "Sleeping Bag", 129.99m, "sleeping1.jpg")
            {
                Id = 4,
                AvailableStock = 25,
                RestockThreshold = 8,
                MaxStockThreshold = 50
            },
            new(2, 1, "Waterproof hiking boots with ankle support", "Hiking Boots", 179.99m, "boots1.jpg")
            {
                Id = 5,
                AvailableStock = 40,
                RestockThreshold = 12,
                MaxStockThreshold = 80
            },
            new(1, 2, "Lightweight rain poncho for emergency weather", "Rain Poncho", 39.99m, "poncho1.jpg")
            {
                Id = 6,
                AvailableStock = 100,
                RestockThreshold = 20,
                MaxStockThreshold = 200
            },
            new(4, 4, "Adjustable trekking poles with ergonomic grips", "Trekking Poles", 69.99m, "poles1.jpg")
            {
                Id = 7,
                AvailableStock = 60,
                RestockThreshold = 15,
                MaxStockThreshold = 120
            },
            new(5, 3, "2-person camping tent with easy setup", "Camping Tent", 249.99m, "tent1.jpg")
            {
                Id = 8,
                AvailableStock = 20,
                RestockThreshold = 5,
                MaxStockThreshold = 40
            },
            new(1, 2, "Warm fleece jacket for layering", "Fleece Jacket", 79.99m, "fleece1.jpg")
            {
                Id = 9,
                AvailableStock = 80,
                RestockThreshold = 18,
                MaxStockThreshold = 160
            },
            new(4, 4, "Merino wool hiking socks for comfort", "Hiking Socks", 24.99m, "socks1.jpg")
            {
                Id = 10,
                AvailableStock = 200,
                RestockThreshold = 50,
                MaxStockThreshold = 400
            }
        };
    }

    private List<CustomerBasket> CreateTestBaskets()
    {
        return new List<CustomerBasket>
        {
            new()
            {
                BuyerId = "test-customer-1",
                Items = new List<BasketItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = 1,
                        ProductName = "Hiking Jacket",
                        UnitPrice = 199.99m,
                        Quantity = 1,
                        PictureUrl = "jacket1.jpg"
                    }
                }
            },
            new()
            {
                BuyerId = "test-customer-2",
                Items = new List<BasketItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = 2,
                        ProductName = "Trail Running Shoes",
                        UnitPrice = 149.99m,
                        Quantity = 1,
                        PictureUrl = "shoes1.jpg"
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProductId = 3,
                        ProductName = "Camping Backpack",
                        UnitPrice = 89.99m,
                        Quantity = 1,
                        PictureUrl = "backpack1.jpg"
                    }
                }
            }
        };
    }
}