namespace ClientApp.UnitTests.Services;

public class OrdersServiceTests
{
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
