using ClientApp.UnitTests.Mocks;
using eShop.ClientApp.Services.Identity;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class OrderViewModelTests
{
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    public OrderViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _settingsService = new MockSettingsService();

        var mockBasketService = new BasketMockService();
        var mockCatalogService = new CatalogMockService();
        var mockOrderService = new OrderMockService();
        var mockIdentityService = new IdentityMockService();

        _appEnvironmentService =
            new AppEnvironmentService(
                mockBasketService, mockBasketService,
                mockCatalogService, mockCatalogService,
                mockOrderService, mockOrderService,
                mockIdentityService, mockIdentityService);

        _appEnvironmentService.UpdateDependencies(true);
    }

    [TestMethod]
    public void OrderPropertyIsNullWhenViewModelInstantiatedTest()
    {
        var orderViewModel = new OrderDetailViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.IsNull(orderViewModel.Order);
    }

    [TestMethod]
    public async Task OrderPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var orderViewModel = new OrderDetailViewModel(_appEnvironmentService, _navigationService, _settingsService);

        var order = await _appEnvironmentService.OrderService.GetOrderAsync(1);

        orderViewModel.OrderNumber = order.OrderNumber;
        await orderViewModel.InitializeAsync();

        Assert.IsNotNull(orderViewModel.Order);
    }

    [TestMethod]
    public async Task SettingOrderPropertyShouldRaisePropertyChanged()
    {
        bool invoked = false;

        var orderViewModel = new OrderDetailViewModel(_appEnvironmentService, _navigationService, _settingsService);

        orderViewModel.PropertyChanged += (_, e) =>
        {
            if (e?.PropertyName?.Equals(nameof(OrderDetailViewModel.Order)) ?? false)
            {
                invoked = true;
            }
        };
        var order = await _appEnvironmentService.OrderService.GetOrderAsync(1);
        orderViewModel.OrderNumber = order.OrderNumber;
        await orderViewModel.InitializeAsync();

        Assert.IsTrue(invoked);
    }
}
