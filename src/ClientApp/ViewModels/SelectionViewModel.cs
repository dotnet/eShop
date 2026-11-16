namespace eShop.ClientApp.ViewModels;

public partial class SelectionViewModel<T> : ObservableObject
{
    [ObservableProperty] public partial bool Selected { get; set; }
    [ObservableProperty] public partial T Value { get; set; }
}
