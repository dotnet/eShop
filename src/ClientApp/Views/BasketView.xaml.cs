namespace eShop.ClientApp.Views
{
    public partial class BasketView : ContentPageBase
    {
        public BasketView(BasketViewModel viewModel)
        {
            BindingContext = viewModel;
            InitializeComponent();
        }
    }
}
