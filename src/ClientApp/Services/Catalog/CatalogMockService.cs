using eShop.ClientApp.Models.Catalog;

namespace eShop.ClientApp.Services.Catalog;

public class CatalogMockService : ICatalogService
{
    private static readonly List<CatalogBrand> MockCatalogBrands =
        new() {new CatalogBrand {Id = 1, Brand = "Azure"}, new CatalogBrand {Id = 2, Brand = "Visual Studio"}};

    private static readonly List<CatalogType> MockCatalogTypes =
        new() {new CatalogType {Id = 1, Type = "Mug"}, new CatalogType {Id = 2, Type = "T-Shirt"}};

    private static readonly List<CatalogItem> MockCatalog =
        new()
        {
            new CatalogItem
            {
                Id = Common.Common.MockCatalogItemId01,
                PictureUri = "fake_product_01.png",
                Name = "Adventurer GPS Watch",
                Price = 199.99M,
                CatalogBrandId = 2,
                CatalogBrand = MockCatalogBrands[1],
                CatalogTypeId = 2,
                CatalogType = MockCatalogTypes[1]
            },
            new CatalogItem
            {
                Id = Common.Common.MockCatalogItemId02,
                PictureUri = "fake_product_02.png",
                Name = "AeroLite Cycling Helmet",
                Price = 129.99M,
                CatalogBrandId = 2,
                CatalogBrand = MockCatalogBrands[1],
                CatalogTypeId = 2,
                CatalogType = MockCatalogTypes[1]
            },
            new CatalogItem
            {
                Id = Common.Common.MockCatalogItemId03,
                PictureUri = "fake_product_03.png",
                Name = "Alpine AlpinePack Backpack",
                Price = 129.00M,
                CatalogBrandId = 2,
                CatalogBrand = MockCatalogBrands[1],
                CatalogTypeId = 2,
                CatalogType = MockCatalogTypes[1]
            },
            new CatalogItem
            {
                Id = Common.Common.MockCatalogItemId04,
                PictureUri = "fake_product_04.png",
                Name = "Alpine Fusion Goggles",
                Price = 79.99M,
                CatalogBrandId = 2,
                CatalogBrand = MockCatalogBrands[1],
                CatalogTypeId = 1,
                CatalogType = MockCatalogTypes[0]
            },
            new CatalogItem
            {
                Id = Common.Common.MockCatalogItemId05,
                PictureUri = "fake_product_05.png",
                Name = "Alpine PeakDown Jacket",
                Price = 249.99M,
                CatalogBrandId = 1,
                CatalogBrand = MockCatalogBrands[0],
                CatalogTypeId = 2,
                CatalogType = MockCatalogTypes[1]
            }
        };

    public async Task<IEnumerable<CatalogItem>> GetCatalogAsync()
    {
        await Task.Delay(10);

        return MockCatalog;
    }

    public async Task<CatalogItem> GetCatalogItemAsync(int catalogItemId)
    {
        await Task.Delay(10);

        return MockCatalog.FirstOrDefault(x => x.Id == catalogItemId);
    }

    public async Task<IEnumerable<CatalogItem>> FilterAsync(int catalogBrandId, int catalogTypeId)
    {
        await Task.Delay(10);

        return MockCatalog
            .Where(
                c => c.CatalogBrandId == catalogBrandId &&
                     c.CatalogTypeId == catalogTypeId)
            .ToArray();
    }

    public async Task<IEnumerable<CatalogBrand>> GetCatalogBrandAsync()
    {
        await Task.Delay(10);

        return MockCatalogBrands;
    }

    public async Task<IEnumerable<CatalogType>> GetCatalogTypeAsync()
    {
        await Task.Delay(10);

        return MockCatalogTypes;
    }
}
