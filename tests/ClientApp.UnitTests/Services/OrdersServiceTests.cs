using ClientApp.UnitTests.Mocks;

namespace ClientApp.UnitTests.Services;

[TestClass]
public class OrdersServiceTests
{
    private readonly ISettingsService _settingsService;

    public OrdersServiceTests()
    {
        _settingsService = new MockSettingsService();
    }
    
    [TestMethod]
    public async Task GetFakeOrderTest()
    {
        var ordersMockService = new OrderMockService();
        var order = await ordersMockService.GetOrderAsync(1);

        Assert.IsNotNull(order);
    }

    [TestMethod]
    public async Task GetFakeOrdersTest()
    {
        var ordersMockService = new OrderMockService();
        var result = await ordersMockService.GetOrdersAsync();

        Assert.AreNotEqual(0, result.Count());
    }
}
