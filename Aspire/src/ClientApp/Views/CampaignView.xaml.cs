namespace eShop.ClientApp.Views;

public partial class CampaignView : ContentPageBase
{
    public CampaignView(CampaignDetailsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
