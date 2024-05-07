using ClientApp.UnitTests.Mocks;
using eShop.ClientApp.Services.Identity;

namespace ClientApp.UnitTests.ViewModels;

public class CatalogViewModelTests
{
    private readonly INavigationService _navigationService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    public CatalogViewModelTests()
    {
        _navigationService = new MockNavigationService();

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
    
    [Fact]
    public void FilterCommandIsNotNullTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.NotNull(catalogViewModel.FilterCommand);
    }

    [Fact]
    public void ClearFilterCommandIsNotNullTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.NotNull(catalogViewModel.ClearFilterCommand);
    }

    [Fact]
    public void ProductsPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.Empty(catalogViewModel.Products);
    }

    [Fact]
    public void BrandsPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.Empty(catalogViewModel.Brands);
    }

    [Fact]
    public void BrandPropertyIsNullWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.Null(catalogViewModel.SelectedBrand);
    }

    [Fact]
    public void TypesPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.Empty(catalogViewModel.Types);
    }

    [Fact]
    public void TypePropertyIsNullWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.Null(catalogViewModel.SelectedType);
    }

    [Fact]
    public void IsFilterPropertyIsFalseWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.False(catalogViewModel.IsFiltering);
    }

    [Fact]
    public async Task ProductsPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();

        Assert.NotNull(catalogViewModel.Products);
    }

    [Fact]
    public async Task BrandsPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();

        Assert.NotNull(catalogViewModel.Brands);
    }

    [Fact]
    public async Task TypesPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();

        Assert.NotNull(catalogViewModel.Types);
    }

    [Fact]
    public async Task SettingBadgeCountPropertyShouldRaisePropertyChanged()
    {
        bool invoked = false;

        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

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
    public async Task ClearFilterCommandResetsPropertiesTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();
        await catalogViewModel.ClearFilterCommand.ExecuteUntilComplete(null);

        Assert.Null(catalogViewModel.SelectedBrand);
        Assert.Null(catalogViewModel.SelectedType);
        Assert.NotNull(catalogViewModel.Products);
    }
}
