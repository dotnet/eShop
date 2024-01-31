namespace eShop.ClientApp.Views;

public partial class CampaignDetailsView : ContentPage
{
    public CampaignDetailsView(CampaignDetailsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
