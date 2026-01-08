using eShop.Catalog.API.Model;

namespace eShop.UnitTests.Shared.TestData;

public static class ProductTestData
{
    public static CatalogItem CreateValidCatalogItem(
        int id = 1,
        string name = "Test Product",
        string description = "Test product description",
        decimal price = 99.99m,
        string pictureFileName = "test.jpg",
        int catalogTypeId = 1,
        int catalogBrandId = 1,
        int availableStock = 100,
        int restockThreshold = 10,
        int maxStockThreshold = 200,
        bool onReorder = false)
    {
        return new CatalogItem(
            catalogTypeId,
            catalogBrandId,
            description,
            name,
            price,
            pictureFileName)
        {
            Id = id,
            AvailableStock = availableStock,
            RestockThreshold = restockThreshold,
            MaxStockThreshold = maxStockThreshold,
            OnReorder = onReorder
        };
    }

    public static List<CatalogItem> CreateCatalogItemList(int count = 10)
    {
        var items = new List<CatalogItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(CreateValidCatalogItem(
                id: i,
                name: $"Product {i}",
                description: $"Description for product {i}",
                price: 10.00m * i,
                catalogTypeId: (i % 3) + 1, // Distribute across 3 types
                catalogBrandId: (i % 2) + 1, // Distribute across 2 brands
                availableStock: 50 + (i * 10)
            ));
        }
        return items;
    }

    public static CatalogType CreateCatalogType(
        int id = 1,
        string type = "Outdoor Gear")
    {
        return new CatalogType
        {
            Id = id,
            Type = type
        };
    }

    public static List<CatalogType> CreateCatalogTypes()
    {
        return new List<CatalogType>
        {
            CreateCatalogType(1, "Jackets"),
            CreateCatalogType(2, "Boots"),
            CreateCatalogType(3, "Backpacks"),
            CreateCatalogType(4, "Accessories")
        };
    }

    public static CatalogBrand CreateCatalogBrand(
        int id = 1,
        string brand = "AdventureWorks")
    {
        return new CatalogBrand
        {
            Id = id,
            Brand = brand
        };
    }

    public static List<CatalogBrand> CreateCatalogBrands()
    {
        return new List<CatalogBrand>
        {
            CreateCatalogBrand(1, "AdventureWorks"),
            CreateCatalogBrand(2, "Mountain Peak"),
            CreateCatalogBrand(3, "Trail Blazer"),
            CreateCatalogBrand(4, "Outdoor Pro")
        };
    }

    public static List<CatalogItem> CreateOutdoorGearProducts()
    {
        return new List<CatalogItem>
        {
            CreateValidCatalogItem(1, "Hiking Jacket", "Waterproof hiking jacket", 199.99m, "jacket1.jpg", 1, 1),
            CreateValidCatalogItem(2, "Trail Running Shoes", "Lightweight trail running shoes", 149.99m, "shoes1.jpg", 2, 2),
            CreateValidCatalogItem(3, "Camping Backpack", "40L camping backpack", 89.99m, "backpack1.jpg", 3, 1),
            CreateValidCatalogItem(4, "Sleeping Bag", "3-season sleeping bag", 129.99m, "sleeping1.jpg", 4, 3),
            CreateValidCatalogItem(5, "Hiking Boots", "Waterproof hiking boots", 179.99m, "boots1.jpg", 2, 1),
            CreateValidCatalogItem(6, "Rain Poncho", "Lightweight rain poncho", 39.99m, "poncho1.jpg", 1, 2),
            CreateValidCatalogItem(7, "Trekking Poles", "Adjustable trekking poles", 69.99m, "poles1.jpg", 4, 4),
            CreateValidCatalogItem(8, "Camping Tent", "2-person camping tent", 249.99m, "tent1.jpg", 4, 3),
            CreateValidCatalogItem(9, "Fleece Jacket", "Warm fleece jacket", 79.99m, "fleece1.jpg", 1, 2),
            CreateValidCatalogItem(10, "Hiking Socks", "Merino wool hiking socks", 24.99m, "socks1.jpg", 4, 4)
        };
    }

    public static CatalogItem CreateProductWithLowStock(
        int id = 999,
        int availableStock = 2,
        int restockThreshold = 10)
    {
        return CreateValidCatalogItem(
            id: id,
            name: "Low Stock Product",
            availableStock: availableStock,
            restockThreshold: restockThreshold,
            onReorder: true
        );
    }

    public static CatalogItem CreateOutOfStockProduct(int id = 998)
    {
        return CreateValidCatalogItem(
            id: id,
            name: "Out of Stock Product",
            availableStock: 0,
            restockThreshold: 10,
            onReorder: true
        );
    }

    public static CatalogItem CreateDiscountedProduct(
        int id = 997,
        decimal originalPrice = 199.99m,
        decimal discountedPrice = 149.99m)
    {
        var product = CreateValidCatalogItem(
            id: id,
            name: "Discounted Product",
            price: discountedPrice
        );
        
        // In a real implementation, there might be a separate discount mechanism
        // For testing purposes, we'll use the price field
        return product;
    }
}