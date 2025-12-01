namespace ClientApp.UnitTests.Services;

[TestClass]
public class BasketServiceTests
{
    [TestMethod]
    public async Task GetFakeBasketTest()
    {
        var catalogMockService = new CatalogMockService();
        var result = await catalogMockService.GetCatalogAsync();
        Assert.AreNotEqual(0, result.Count());
    }
}
