namespace eShop.ClientApp.ViewModels;

public partial class SelectionViewModel<T> : ObservableObject
{
    [ObservableProperty] private bool _selected;
    [ObservableProperty] private T _value;
}
