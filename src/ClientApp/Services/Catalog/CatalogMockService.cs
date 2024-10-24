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
                CatalogType = MockCatalogTypes[1],
                Description = "Navigate with confidence using the Adventurer GPS Watch by Adventurer. This rugged and durable watch features a built-in GPS, altimeter, and compass, allowing you to track your progress and find your way in any terrain. With its sleek black design and easy-to-read display, this watch is both stylish and practical. The Adventurer GPS Watch is a must-have for every adventurer."
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
                CatalogType = MockCatalogTypes[1],
                Description = "Stay safe on your cycling adventures with the Trailblazer Bike Helmet by Green Equipment. This lightweight and durable helmet features an adjustable fit system and ventilation for added comfort. With its vibrant green color and sleek design, you'll stand out on the road. The Trailblazer Bike Helmet is perfect for all types of cycling, from mountain biking to road cycling."
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
                CatalogType = MockCatalogTypes[1],
                Description = "The AlpinePack backpack by Green Equipment is your ultimate companion for outdoor adventures. This versatile and durable backpack features a sleek navy design with reinforced straps. With a capacity of 45 liters, multiple compartments, and a hydration pack sleeve, it offers ample storage and organization. The ergonomic back panel ensures maximum comfort, even on the most challenging treks."
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
                CatalogType = MockCatalogTypes[0],
                Description = "Enhance your skiing experience with the Alpine Fusion Goggles from WildRunner. These goggles offer full UV protection and anti-fog lenses to keep your vision clear on the slopes. With their stylish silver frame and orange lenses, you'll stand out from the crowd. Adjustable straps ensure a secure fit, while the soft foam padding provides comfort all day long."
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
                CatalogType = MockCatalogTypes[1],
                Description = "The Solstix Alpine Peak Down Jacket is crafted for extreme cold conditions. With its bold red color and sleek design, this jacket combines style with functionality. Made with high-quality goose down insulation, the Alpine Peak Jacket provides exceptional warmth and comfort. The jacket features a removable hood, adjustable cuffs, and multiple zippered pockets for storage. Conquer the harshest weather with the Solstix Alpine Peak Down Jacket."
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
