using eShop.ClientApp.Services.Basket;
using eShop.ClientApp.Services.Catalog;
using eShop.ClientApp.Services.Marketing;
using eShop.ClientApp.Services.Order;
using eShop.ClientApp.Services.User;

namespace eShop.ClientApp.Services.AppEnvironment;

public interface IAppEnvironmentService
{
    IBasketService BasketService { get; }
    ICampaignService CampaignService { get; }
    ICatalogService CatalogService { get; }
    IOrderService OrderService { get; }
    IUserService UserService { get; }

    void UpdateDependencies(bool useMockServices);
}
