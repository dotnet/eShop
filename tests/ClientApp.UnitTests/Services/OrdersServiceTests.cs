using ClientApp.UnitTests.Mocks;

namespace ClientApp.UnitTests.Services;

public class OrdersServiceTests
{
    private readonly ISettingsService _settingsService;

    public OrdersServiceTests()
    {
        _settingsService = new MockSettingsService();
    }
    
    [Fact]
    public async Task GetFakeOrderTest()
    {
        var ordersMockService = new OrderMockService();
        var order = await ordersMockService.GetOrderAsync(1);

        Assert.NotNull(order);
    }

    [Fact]
    public async Task GetFakeOrdersTest()
    {
        var ordersMockService = new OrderMockService();
        var result = await ordersMockService.GetOrdersAsync();

        Assert.NotEmpty(result);
    }
}
