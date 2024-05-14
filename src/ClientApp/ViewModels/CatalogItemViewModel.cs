using CommunityToolkit.Mvvm.Messaging;
using eShop.ClientApp.Messages;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Models.Catalog;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class CatalogItemViewModel : ViewModelBase
{
    private readonly IAppEnvironmentService _appEnvironmentService;

    [ObservableProperty] private CatalogItem _catalogItem;

    public CatalogItemViewModel(IAppEnvironmentService appEnvironmentService, INavigationService navigationService) :
        base(navigationService)
    {
        _appEnvironmentService = appEnvironmentService;
    }

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);

        CatalogItem = query.ValueAs<CatalogItem>("CatalogItem");
    }

    [RelayCommand]
    private async Task AddCatalogItemAsync()
    {
        if (CatalogItem is null)
        {
            return;
        }

        var basket = await _appEnvironmentService.BasketService.GetBasketAsync();
        if (basket is not null)
        {
            basket.AddItemToBasket(
                new BasketItem
                {
                    ProductId = CatalogItem.Id,
                    ProductName = CatalogItem.Name,
                    PictureUrl = CatalogItem.PictureUri,
                    UnitPrice = CatalogItem.Price,
                    Quantity = 1
                });

            var basketUpdate = await _appEnvironmentService.BasketService.UpdateBasketAsync(basket);

            WeakReferenceMessenger.Default
                .Send(new ProductCountChangedMessage(basketUpdate.ItemCount));

            await NavigationService.PopAsync();
        }
    }

    [RelayCommand]
    private async Task DismissAsync()
    {
        await NavigationService.PopAsync();
    }
}
