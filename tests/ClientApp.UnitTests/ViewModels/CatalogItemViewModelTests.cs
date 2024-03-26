using CommunityToolkit.Mvvm.Messaging;
using eShop.ClientApp.Models.Catalog;
using eShop.ClientApp.Services.Identity;

namespace eShop.ClientApp.UnitTests;

public class CatalogItemViewModelTests
{
    private readonly INavigationService _navigationService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    public CatalogItemViewModelTests()
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
    public void AddCatalogItemCommandIsNotNullTest()
    {
        var CatalogItemViewModel = new CatalogItemViewModel(_appEnvironmentService, _navigationService);
        Assert.NotNull(CatalogItemViewModel.AddCatalogItemCommand);
    }
    
    [Fact]
    public async Task AddCatalogItemCommandSendsAddProductMessageTest()
    {
        bool messageReceived = false;

        var CatalogItemViewModel = new CatalogItemViewModel(_appEnvironmentService, _navigationService);

        WeakReferenceMessenger.Default
            .Register<Messages.AddProductMessage>(
                this,
                (r, m) =>
                {
                    messageReceived = true;
                });

        await CatalogItemViewModel.AddCatalogItemCommand
            .ExecuteUntilComplete(
                new CatalogItem
                {
                    Id = 123,
                    Name = "test",
                    Price = 1.23m,
                });

        Assert.True(messageReceived);
    }

}
