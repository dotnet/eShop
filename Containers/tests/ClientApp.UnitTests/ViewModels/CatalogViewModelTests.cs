using CommunityToolkit.Mvvm.Messaging;
using eShop.ClientApp.Models.Catalog;

namespace eShop.ClientApp.UnitTests;

public class CatalogViewModelTests
{
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    public CatalogViewModelTests()
    {
        _dialogService = new MockDialogService();
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
    public void AddCatalogItemCommandIsNotNullTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.NotNull(catalogViewModel.AddCatalogItemCommand);
    }

    [Fact]
    public void FilterCommandIsNotNullTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.NotNull(catalogViewModel.FilterCommand);
    }

    [Fact]
    public void ClearFilterCommandIsNotNullTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.NotNull(catalogViewModel.ClearFilterCommand);
    }

    [Fact]
    public void ProductsPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Empty(catalogViewModel.Products);
    }

    [Fact]
    public void BrandsPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Empty(catalogViewModel.Brands);
    }

    [Fact]
    public void BrandPropertyIsNullWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Null(catalogViewModel.Brand);
    }

    [Fact]
    public void TypesPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Empty(catalogViewModel.Types);
    }

    [Fact]
    public void TypePropertyIsNullWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.Null(catalogViewModel.Type);
    }

    [Fact]
    public void IsFilterPropertyIsFalseWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);
        Assert.False(catalogViewModel.IsFilter);
    }

    [Fact]
    public async Task ProductsPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);

        await catalogViewModel.InitializeAsync();

        Assert.NotNull(catalogViewModel.Products);
    }

    [Fact]
    public async Task BrandsPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);

        await catalogViewModel.InitializeAsync();

        Assert.NotNull(catalogViewModel.Brands);
    }

    [Fact]
    public async Task TypesPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);

        await catalogViewModel.InitializeAsync();

        Assert.NotNull(catalogViewModel.Types);
    }

    [Fact]
    public async Task SettingBadgeCountPropertyShouldRaisePropertyChanged()
    {
        bool invoked = false;

        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);

        catalogViewModel.PropertyChanged += (_, e) =>
        {
            if (e?.PropertyName?.Equals(nameof(CatalogViewModel.BadgeCount)) ?? false)
            {
                invoked = true;
            }
        };
        await catalogViewModel.InitializeAsync();

        Assert.True(invoked);
    }

    [Fact]
    public async Task AddCatalogItemCommandSendsAddProductMessageTest()
    {
        bool messageReceived = false;

        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);

        WeakReferenceMessenger.Default
            .Register<Messages.AddProductMessage>(
                this,
                (r, m) =>
                {
                    messageReceived = true;
                });

        await catalogViewModel.AddCatalogItemCommand
            .ExecuteUntilComplete(
                new CatalogItem
                {
                    Id = "id",
                    Name = "test",
                    Price = 1.23m,
                });

        Assert.True(messageReceived);
    }

    [Fact]
    public async Task ClearFilterCommandResetsPropertiesTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService, _settingsService);

        await catalogViewModel.InitializeAsync();
        await catalogViewModel.ClearFilterCommand.ExecuteUntilComplete(null);

        Assert.Null(catalogViewModel.Brand);
        Assert.Null(catalogViewModel.Type);
        Assert.NotNull(catalogViewModel.Products);
    }
}
