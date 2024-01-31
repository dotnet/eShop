namespace eShop.ClientApp.UnitTests;

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
        var mockCampaignService = new CampaignMockService();
        var mockCatalogService = new CatalogMockService();
        var mockOrderService = new OrderMockService();
        var mockUserService = new UserMockService();

        _appEnvironmentService =
            new AppEnvironmentService(
                mockBasketService, mockBasketService,
                mockCampaignService, mockCampaignService,
                mockCatalogService, mockCatalogService,
                mockOrderService, mockOrderService,
                mockUserService, mockUserService);

        _appEnvironmentService.UpdateDependencies(true);
    }

    [Fact]
    public void OrderPropertyIsNullWhenViewModelInstantiatedTest()
    {
        var orderViewModel = new OrderDetailViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Null(orderViewModel.Order);
    }

    [Fact]
    public async Task OrderPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var orderViewModel = new OrderDetailViewModel(_appEnvironmentService, _navigationService, _settingsService);

        var order = await _appEnvironmentService.OrderService.GetOrderAsync(1, GlobalSetting.Instance.AuthToken);

        orderViewModel.OrderNumber = order.OrderNumber;
        await orderViewModel.InitializeAsync();

        Assert.NotNull(orderViewModel.Order);
    }

    [Fact]
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
        var order = await _appEnvironmentService.OrderService.GetOrderAsync(1, GlobalSetting.Instance.AuthToken);
        orderViewModel.OrderNumber = order.OrderNumber;
        await orderViewModel.InitializeAsync();

        Assert.True(invoked);
    }
}
