namespace ClientApp.UnitTests.Services;

[TestClass]
public class CatalogServiceTests
{
    [TestMethod]
    public async Task GetFakeCatalogTest()
    {
        var catalogMockService = new CatalogMockService();
        var catalog = await catalogMockService.GetCatalogAsync();

        Assert.AreNotEqual(0, catalog.Count());
    }

    [TestMethod]
    public async Task GetFakeCatalogBrandTest()
    {
        var catalogMockService = new CatalogMockService();
        var catalogBrand = await catalogMockService.GetCatalogBrandAsync();

        Assert.AreNotEqual(0, catalogBrand.Count());
    }

    [TestMethod]
    public async Task GetFakeCatalogTypeTest()
    {
        var catalogMockService = new CatalogMockService();
        var catalogType = await catalogMockService.GetCatalogTypeAsync();

        Assert.AreNotEqual(0, catalogType.Count());
    }
}
