namespace eShop.ClientApp.Views;

public partial class CheckoutView : ContentPageBase
{
    public CheckoutView(CheckoutViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
