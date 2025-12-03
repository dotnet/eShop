using ClientApp.UnitTests.Mocks;
using eShop.ClientApp.Services.Identity;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
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
    
    [TestMethod]
    public void FilterCommandIsNotNullTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsNotNull(catalogViewModel.FilterCommand);
    }

    [TestMethod]
    public void ClearFilterCommandIsNotNullTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsNotNull(catalogViewModel.ClearFilterCommand);
    }

    [TestMethod]
    public void ProductsPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsEmpty(catalogViewModel.Products);
    }

    [TestMethod]
    public void BrandsPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsEmpty(catalogViewModel.Brands);
    }

    [TestMethod]
    public void BrandPropertyIsNullWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsNull(catalogViewModel.SelectedBrand);
    }

    [TestMethod]
    public void TypesPropertyIsEmptyWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsEmpty(catalogViewModel.Types);
    }

    [TestMethod]
    public void TypePropertyIsNullWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsNull(catalogViewModel.SelectedType);
    }

    [TestMethod]
    public void IsFilterPropertyIsFalseWhenViewModelInstantiatedTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);
        Assert.IsFalse(catalogViewModel.IsFiltering);
    }

    [TestMethod]
    public async Task ProductsPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();

        Assert.IsNotNull(catalogViewModel.Products);
    }

    [TestMethod]
    public async Task BrandsPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();

        Assert.IsNotNull(catalogViewModel.Brands);
    }

    [TestMethod]
    public async Task TypesPropertyIsNotNullAfterViewModelInitializationTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();

        Assert.IsNotNull(catalogViewModel.Types);
    }

    [TestMethod]
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

        Assert.IsTrue(invoked);
    }

    [TestMethod]
    public async Task ClearFilterCommandResetsPropertiesTest()
    {
        var catalogViewModel = new CatalogViewModel(_appEnvironmentService, _navigationService);

        await catalogViewModel.InitializeAsync();
        await catalogViewModel.ClearFilterCommand.ExecuteUntilComplete(null);

        Assert.IsNull(catalogViewModel.SelectedBrand);
        Assert.IsNull(catalogViewModel.SelectedType);
        Assert.IsNotNull(catalogViewModel.Products);
    }
}
