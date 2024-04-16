namespace ClientApp.UnitTests.Services;

public class BasketServiceTests
{
    [Fact]
    public async Task GetFakeBasketTest()
    {
        var catalogMockService = new CatalogMockService();
        var result = await catalogMockService.GetCatalogAsync();
        Assert.NotEmpty(result);
    }
}
