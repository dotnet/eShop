#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using eShop.ClientApp.Messages;
using eShop.ClientApp.Models.Catalog;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class CatalogViewModel : ViewModelBase
{
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly ObservableCollectionEx<CatalogBrandSelectionViewModel> _brands = new();

    private readonly ObservableCollectionEx<CatalogItem> _products = new();
    private readonly ObservableCollectionEx<CatalogTypeSelectionViewModel> _types = new();

    [ObservableProperty] private int _badgeCount;

    private bool _initialized;

    [ObservableProperty] private bool _isFiltering;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFilter))]
    [NotifyCanExecuteChangedFor(nameof(ApplyFilterCommand))]
    private CatalogBrand? _selectedBrand;

    [ObservableProperty] private CatalogItem? _selectedProduct;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFilter))]
    [NotifyCanExecuteChangedFor(nameof(ApplyFilterCommand))]
    private CatalogType? _selectedType;

    public CatalogViewModel(
        IAppEnvironmentService appEnvironmentService,
        INavigationService navigationService)
        : base(navigationService)
    {
        _appEnvironmentService = appEnvironmentService;

        _products = new ObservableCollectionEx<CatalogItem>();
        _brands = new ObservableCollectionEx<CatalogBrandSelectionViewModel>();
        _types = new ObservableCollectionEx<CatalogTypeSelectionViewModel>();

        WeakReferenceMessenger.Default
            .Register<CatalogViewModel, ProductCountChangedMessage>(
                this,
                (_, message) =>
                {
                    BadgeCount = message.Value;
                });
    }

    public bool CanFilter => SelectedBrand is not null && SelectedType is not null;

    public IReadOnlyList<CatalogItem> Products => _products;

    public IReadOnlyList<CatalogBrandSelectionViewModel> Brands => _brands;

    public IReadOnlyList<CatalogTypeSelectionViewModel> Types => _types;

    public override async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await IsBusyFor(
            async () =>
            {
                // Get Catalog, Brands and Types
                var products = await _appEnvironmentService.CatalogService.GetCatalogAsync();
                var brands = await _appEnvironmentService.CatalogService.GetCatalogBrandAsync();
                var types = await _appEnvironmentService.CatalogService.GetCatalogTypeAsync();
                var basket = await _appEnvironmentService.BasketService.GetBasketAsync();

                BadgeCount = basket.ItemCount;

                _products.ReloadData(products);
                _brands.ReloadData(brands.Select(x => new CatalogBrandSelectionViewModel {Value = x}));
                _types.ReloadData(types.Select(x => new CatalogTypeSelectionViewModel {Value = x}));
            });
    }

    [RelayCommand]
    private async Task ViewCatalogItemAsync(CatalogItem catalogItem)
    {
        SelectedProduct = null;

        if (catalogItem is null)
        {
            return;
        }

        await NavigationService.NavigateToAsync(
            "ViewCatalogItem",
            new Dictionary<string, object> {["CatalogItem"] = catalogItem});
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
                    var filteredProducts =
                        await _appEnvironmentService.CatalogService.FilterAsync(SelectedBrand.Id, SelectedType.Id);
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
