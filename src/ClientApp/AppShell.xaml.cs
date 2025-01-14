using eShop.ClientApp.Services;
using eShop.ClientApp.Views;

namespace eShop.ClientApp;

public partial class AppShell : Shell
{
    private readonly INavigationService _navigationService;

    public AppShell(INavigationService navigationService)
    {
        _navigationService = navigationService;

        InitializeRouting();
        InitializeComponent();
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is not null)
        {
            await _navigationService.InitializeAsync();
        }
    }

    private static void InitializeRouting()
    {
        //Routing.RegisterRoute("Login", typeof(LoginView));
        Routing.RegisterRoute("Filter", typeof(FiltersView));
        Routing.RegisterRoute("ViewCatalogItem", typeof(CatalogItemView));
        Routing.RegisterRoute("Basket", typeof(BasketView));
        Routing.RegisterRoute("Settings", typeof(SettingsView));
        Routing.RegisterRoute("OrderDetail", typeof(OrderDetailView));
        Routing.RegisterRoute("Checkout", typeof(CheckoutView));
    }
}
