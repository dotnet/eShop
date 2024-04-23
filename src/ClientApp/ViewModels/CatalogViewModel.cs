using CommunityToolkit.Mvvm.Messaging;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Models.Catalog;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class CatalogViewModel : ViewModelBase
{
    private readonly IAppEnvironmentService _appEnvironmentService;

    private readonly ObservableCollectionEx<CatalogItem> _products = new();
    private readonly ObservableCollectionEx<CatalogBrand> _brands = new();
    private readonly ObservableCollectionEx<CatalogType> _types = new();

    [ObservableProperty]
    private CatalogItem _selectedProduct;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilter))]
    [NotifyCanExecuteChangedFor(nameof(FilterCommand))]
    private CatalogBrand _brand;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilter))]
    [NotifyCanExecuteChangedFor(nameof(FilterCommand))]
    private CatalogType _type;

    [ObservableProperty]
    private int _badgeCount;

    private bool _initialized;

    public IReadOnlyList<CatalogItem> Products => _products;

    public IReadOnlyList<CatalogBrand> Brands => _brands;

    public IReadOnlyList<CatalogType> Types => _types;

    public bool IsFilter => Brand is not null && Type is not null;

    public CatalogViewModel(
        IAppEnvironmentService appEnvironmentService,
        INavigationService navigationService)
        : base(navigationService)
    {
        _appEnvironmentService = appEnvironmentService;

        _products = new ObservableCollectionEx<CatalogItem>();
        _brands = new ObservableCollectionEx<CatalogBrand>();
        _types = new ObservableCollectionEx<CatalogType>();
    }

    public override async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        await IsBusyFor(
            async () =>
            {
                // Get Catalog, Brands and Types
                var products = await _appEnvironmentService.CatalogService.GetCatalogAsync();
                var brands = await _appEnvironmentService.CatalogService.GetCatalogBrandAsync();
                var types = await _appEnvironmentService.CatalogService.GetCatalogTypeAsync();
                var basket = await _appEnvironmentService.BasketService.GetBasketAsync();

                BadgeCount = basket?.Items?.Count ?? 0;

                _products.ReloadData(products);
                _brands.ReloadData(brands);
                _types.ReloadData(types);
                
            });
    }

    [RelayCommand]
    private async Task AddCatalogItemAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }
        
        var basket = await _appEnvironmentService.BasketService.GetBasketAsync();
        if (basket is not null)
        {
            if (basket.Items is null)
            {
                basket.Items = new List<BasketItem>();
            }

            basket.Items.Add(
                new BasketItem
                {
                    ProductId = SelectedProduct.Id,
                    ProductName = SelectedProduct.Name,
                    PictureUrl = SelectedProduct.PictureUri,
                    UnitPrice = SelectedProduct.Price,
                    Quantity = 1,
                });

            await _appEnvironmentService.BasketService.UpdateBasketAsync(basket);
            BadgeCount = basket.Items.Count;

            WeakReferenceMessenger.Default
                .Send(new Messages.AddProductMessage(BadgeCount));
        }

        SelectedProduct = null;
    }

    [RelayCommand]
    private async Task ShowFilterAsync()
    {
        await NavigationService.NavigateToAsync("Filter");
    }

    [RelayCommand(CanExecute = nameof(IsFilter))]
    private async Task FilterAsync()
    {
        await IsBusyFor(
            async () =>
            {
                if (Brand != null || Type != null)
                {
                    var filteredProducts = await _appEnvironmentService.CatalogService.FilterAsync(Brand.Id, Type.Id);
                    _products.ReloadData(filteredProducts);
                }

                await NavigationService.PopAsync();
            });
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        await IsBusyFor(
            async () =>
            {
                Brand = null;
                Type = null;
                var allProducts = await _appEnvironmentService.CatalogService.GetCatalogAsync();
                _products.ReloadData(allProducts);

                await NavigationService.PopAsync();
            });
    }

    [RelayCommand]
    private async Task ViewBasket()
    {
        await NavigationService.NavigateToAsync("Basket");
    }
}
