namespace eShop.ClientApp.Views;

public partial class OrderDetailView
{
    public OrderDetailView(OrderDetailViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
