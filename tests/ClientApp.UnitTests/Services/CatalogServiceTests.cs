namespace ClientApp.UnitTests.Services;

[TestClass]
public class CatalogServiceTests
{
    [TestMethod]
    public async Task GetFakeCatalogTest()
    {
        var catalogMockService = new CatalogMockService();
        var catalog = await catalogMockService.GetCatalogAsync();

        Assert.AreNotEqual(catalog.Count(), 0);
    }

    [TestMethod]
    public async Task GetFakeCatalogBrandTest()
    {
        var catalogMockService = new CatalogMockService();
        var catalogBrand = await catalogMockService.GetCatalogBrandAsync();

        Assert.AreNotEqual(catalogBrand.Count(), 0);
    }

    [TestMethod]
    public async Task GetFakeCatalogTypeTest()
    {
        var catalogMockService = new CatalogMockService();
        var catalogType = await catalogMockService.GetCatalogTypeAsync();

        Assert.AreNotEqual(catalogType.Count(), 0);
    }
}
