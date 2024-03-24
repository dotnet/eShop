using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class SelectionViewModel<T> : ObservableObject
{
    [ObservableProperty] private T _value;

    [ObservableProperty] private bool _selected;
}
