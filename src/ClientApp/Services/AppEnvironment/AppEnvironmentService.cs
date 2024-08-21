using eShop.ClientApp.Services.Basket;
using eShop.ClientApp.Services.Catalog;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.Order;

namespace eShop.ClientApp.Services.AppEnvironment;

public class AppEnvironmentService : IAppEnvironmentService
{
    private readonly IBasketService _basketService;
    private readonly ICatalogService _catalogService;
    private readonly IIdentityService _identityService;
    private readonly IBasketService _mockBasketService;

    private readonly ICatalogService _mockCatalogService;

    private readonly IIdentityService _mockIdentityService;

    private readonly IOrderService _mockOrderService;
    private readonly IOrderService _orderService;

    public AppEnvironmentService(
        IBasketService mockBasketService, IBasketService basketService,
        ICatalogService mockCatalogService, ICatalogService catalogService,
        IOrderService mockOrderService, IOrderService orderService,
        IIdentityService mockIdentityService, IIdentityService identityService)
    {
        _mockBasketService = mockBasketService;
        _basketService = basketService;

        _mockCatalogService = mockCatalogService;
        _catalogService = catalogService;

        _mockOrderService = mockOrderService;
        _orderService = orderService;

        _mockIdentityService = mockIdentityService;
        _identityService = identityService;
    }

    public IBasketService BasketService { get; private set; }

    public ICatalogService CatalogService { get; private set; }

    public IOrderService OrderService { get; private set; }

    public IIdentityService IdentityService { get; private set; }

    public void UpdateDependencies(bool useMockServices)
    {
        if (useMockServices)
        {
            BasketService = _mockBasketService;
            CatalogService = _mockCatalogService;
            OrderService = _mockOrderService;
            IdentityService = _mockIdentityService;
        }
        else
        {
            BasketService = _basketService;
            CatalogService = _catalogService;
            OrderService = _orderService;
            IdentityService = _identityService;
        }
    }
}
