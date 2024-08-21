using ClientApp.UnitTests.Mocks;
using CommunityToolkit.Mvvm.Messaging;
using eShop.ClientApp.Messages;
using eShop.ClientApp.Models.Catalog;
using eShop.ClientApp.Services.Identity;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class CatalogItemViewModelTests
{
    private readonly INavigationService _navigationService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    public CatalogItemViewModelTests()
    {
        _navigationService = new MockNavigationService();
        var mockCatalogService = new CatalogMockService();
        var mockOrderService = new OrderMockService();
        var mockIdentityService = new IdentityMockService();
        var mockBasketService = new BasketMockService();

        _appEnvironmentService =
            new AppEnvironmentService(
                mockBasketService, mockBasketService,
                mockCatalogService, mockCatalogService,
                mockOrderService, mockOrderService,
                mockIdentityService, mockIdentityService);

        _appEnvironmentService.UpdateDependencies(true);
    }

    [TestMethod]
    public void AddCatalogItemCommandIsNotNullTest()
    {
        var CatalogItemViewModel = new CatalogItemViewModel(_appEnvironmentService, _navigationService);
        Assert.IsNotNull(CatalogItemViewModel.AddCatalogItemCommand);
    }
    
    [TestMethod]
    public async Task AddCatalogItemCommandSendsAddProductMessageTest()
    {
        bool messageReceived = false;

        var catalogItemViewModel = new CatalogItemViewModel(_appEnvironmentService, _navigationService);

        catalogItemViewModel.CatalogItem = new CatalogItem {Id = 123, Name = "test", Price = 1.23m,};
        
        WeakReferenceMessenger.Default
            .Register<CatalogItemViewModelTests, ProductCountChangedMessage>(
                this,
                (_, message) =>
                {
                    messageReceived = true;
                });
        
        await catalogItemViewModel.AddCatalogItemCommand.ExecuteUntilComplete();

        Assert.IsTrue(messageReceived);
    }

}
