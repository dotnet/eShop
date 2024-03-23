namespace eShop.ClientApp.UnitTests;

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
        var order = await ordersMockService.GetOrderAsync(1, _settingsService.AuthAccessToken);

        Assert.NotNull(order);
    }

    [Fact]
    public async Task GetFakeOrdersTest()
    {
        var ordersMockService = new OrderMockService();
        var result = await ordersMockService.GetOrdersAsync(_settingsService.AuthAccessToken);

        Assert.NotEmpty(result);
    }
}
