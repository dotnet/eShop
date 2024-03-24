#nullable enable
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
    private readonly ObservableCollectionEx<CatalogBrandSelectionViewModel> _brands = new();
    private readonly ObservableCollectionEx<CatalogTypeSelectionViewModel> _types = new();

    [ObservableProperty]
    private CatalogItem? _selectedProduct;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFilter))]
    [NotifyCanExecuteChangedFor(nameof(ApplyFilterCommand))]
    private CatalogBrand? _selectedBrand;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFilter))]
    [NotifyCanExecuteChangedFor(nameof(ApplyFilterCommand))]
    private CatalogType? _selectedType;

    [ObservableProperty] 
    private bool _isFiltering;
    
    [ObservableProperty]
    private int _badgeCount;

    private bool _initialized;

    public bool CanFilter => SelectedBrand is not null && SelectedType is not null;
    
    public IReadOnlyList<CatalogItem> Products => _products;

    public IReadOnlyList<CatalogBrandSelectionViewModel> Brands => _brands;

    public IReadOnlyList<CatalogTypeSelectionViewModel> Types => _types;

    public CatalogViewModel(
        IAppEnvironmentService appEnvironmentService,
        INavigationService navigationService)
        : base(navigationService)
    {
        _appEnvironmentService = appEnvironmentService;

        _products = new ObservableCollectionEx<CatalogItem>();
        _brands = new ObservableCollectionEx<CatalogBrandSelectionViewModel>();
        _types = new ObservableCollectionEx<CatalogTypeSelectionViewModel>();
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
                _brands.ReloadData(brands.Select(x => new CatalogBrandSelectionViewModel{ Value = x }));
                _types.ReloadData(types.Select(x => new CatalogTypeSelectionViewModel{ Value = x }));
            });
    }

    [RelayCommand]
    private async Task AddCatalogItemAsync(CatalogItem catalogItem)
    {
        if (catalogItem is null)
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
                    ProductId = catalogItem.Id,
                    ProductName = catalogItem.Name,
                    PictureUrl = catalogItem.PictureUri,
                    UnitPrice = catalogItem.Price,
                    Quantity = 1
                });

            await _appEnvironmentService.BasketService.UpdateBasketAsync(basket);
            BadgeCount = basket.Items.Count;

            WeakReferenceMessenger.Default
                .Send(new Messages.AddProductMessage(BadgeCount));
        }

        SelectedProduct = null;
    }
    
    [RelayCommand]
    private void Filter()
    {
        IsFiltering = !IsFiltering;
    }

    [RelayCommand]
    public void SelectCatalogBrand(CatalogBrand? selectedItem)
    {
        foreach (var brand in Brands)
        {
            var isSelection = brand.Value == selectedItem;

            if (!isSelection)
            {
                brand.Selected = false;
                continue;
            }

            if (brand.Selected)
            {
                SelectedBrand = null;
                brand.Selected = false;
                continue;
            }

            SelectedBrand = selectedItem;
            brand.Selected = true;
        }
    }
    
    [RelayCommand]
    public void SelectCatalogType(CatalogType? selectedItem)
    {
        foreach (var type in Types)
        {
            var isSelection = type.Value == selectedItem;

            if (!isSelection)
            {
                type.Selected = false;
                continue;
            }

            if (type.Selected)
            {
                SelectedType = null;
                type.Selected = false;
                continue;
            }

            SelectedType = selectedItem;
            type.Selected = true;
        }
    }
    
    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        await IsBusyFor(
            async () =>
            {
                if (SelectedBrand is not null && SelectedType is not null)
                {
                    var filteredProducts = await _appEnvironmentService.CatalogService.FilterAsync(SelectedBrand.Id, SelectedType.Id);
                    _products.ReloadData(filteredProducts);
                }

                IsFiltering = false;
            });
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        await IsBusyFor(
            async () =>
            {
                SelectCatalogBrand(default);
                SelectCatalogType(default);
                var allProducts = await _appEnvironmentService.CatalogService.GetCatalogAsync();
                _products.ReloadData(allProducts);
                IsFiltering = false;
            });
    }

    [RelayCommand]
    private async Task ViewBasket()
    {
        await NavigationService.NavigateToAsync("Basket");
    }
}

public class CatalogBrandSelectionViewModel : SelectionViewModel<CatalogBrand>
{
}

public class CatalogTypeSelectionViewModel : SelectionViewModel<CatalogType>
{
}
