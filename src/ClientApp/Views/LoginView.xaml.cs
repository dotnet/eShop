namespace eShop.ClientApp.Views;

public partial class LoginView
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
        //BannerScroll.ScrollToAsync(0, BannerScroll.ContentSize.Height, false);
    }

    protected override void OnAppearing()
    {
        var content = Content;
        Content = null;
        Content = content;

        _viewModel.InvalidateMock();
    }
}
