using eShop.ClientApp.Services.Basket;
using eShop.ClientApp.Services.Catalog;
using eShop.ClientApp.Services.Marketing;
using eShop.ClientApp.Services.Order;
using eShop.ClientApp.Services.User;

namespace eShop.ClientApp.Services.AppEnvironment;

public class AppEnvironmentService : IAppEnvironmentService
{
    private readonly IBasketService _mockBasketService;
    private readonly IBasketService _basketService;

    private readonly ICampaignService _mockCampaignService;
    private readonly ICampaignService _campaignService;

    private readonly ICatalogService _mockCatalogService;
    private readonly ICatalogService _catalogService;

    private readonly IOrderService _mockOrderService;
    private readonly IOrderService _orderService;

    private readonly IUserService _mockUserService;
    private readonly IUserService _userService;

    public IBasketService BasketService { get; private set; }

    public ICampaignService CampaignService { get; private set; }

    public ICatalogService CatalogService { get; private set; }

    public IOrderService OrderService { get; private set; }

    public IUserService UserService { get; private set; }

    public AppEnvironmentService(
        IBasketService mockBasketService, IBasketService basketService,
        ICampaignService mockCampaignService, ICampaignService campaignService,
        ICatalogService mockCatalogService, ICatalogService catalogService,
        IOrderService mockOrderService, IOrderService orderService,
        IUserService mockUserService, IUserService userService)
    {
        _mockBasketService = mockBasketService;
        _basketService = basketService;

        _mockCampaignService = mockCampaignService;
        _campaignService = campaignService;

        _mockCatalogService = mockCatalogService;
        _catalogService = catalogService;

        _mockOrderService = mockOrderService;
        _orderService = orderService;

        _mockUserService = mockUserService;
        _userService = userService;
    }

    public void UpdateDependencies(bool useMockServices)
    {
        if (useMockServices)
        {
            BasketService = _mockBasketService;
            CampaignService = _mockCampaignService;
            CatalogService = _mockCatalogService;
            OrderService = _mockOrderService;
            UserService = _mockUserService;
        }
        else
        {
            BasketService = _basketService;
            CampaignService = _campaignService;
            CatalogService = _catalogService;
            OrderService = _orderService;
            UserService = _userService;
        }
    }
}

