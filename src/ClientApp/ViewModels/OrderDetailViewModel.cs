using eShop.ClientApp.Models.Orders;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

[QueryProperty(nameof(OrderNumber), "OrderNumber")]
public partial class OrderDetailViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAppEnvironmentService _appEnvironmentService;

    [ObservableProperty]
    private Order _order;

    [ObservableProperty]
    private bool _isSubmittedOrder;

    [ObservableProperty]
    private string _orderStatusText;

    [ObservableProperty]
    private int _orderNumber;

    public OrderDetailViewModel(
        IAppEnvironmentService appEnvironmentService,
        INavigationService navigationService, ISettingsService settingsService)
        : base(navigationService)
    {
        _appEnvironmentService = appEnvironmentService;
        _settingsService = settingsService;
    }

    public override async Task InitializeAsync()
    {
        await IsBusyFor(
            async () =>
            {
                // Get order detail info
                Order = await _appEnvironmentService.OrderService.GetOrderAsync(OrderNumber);
                IsSubmittedOrder = Order.OrderStatus == OrderStatus.Submitted;
                OrderStatusText = Order.OrderStatus.ToString().ToUpper();
            });
    }

    [RelayCommand]
    private async Task ToggleCancelOrderAsync()
    {
        var result = await _appEnvironmentService.OrderService.CancelOrderAsync(Order.OrderNumber);

        if (result)
        {
            OrderStatusText = OrderStatus.Cancelled.ToString().ToUpper();
        }
        else
        {
            Order = await _appEnvironmentService.OrderService.GetOrderAsync(Order.OrderNumber);
            OrderStatusText = Order.OrderStatus.ToString().ToUpper();
        }

        IsSubmittedOrder = false;
    }
}
