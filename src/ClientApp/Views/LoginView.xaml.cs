using System.Diagnostics;

namespace eShop.ClientApp.Views;

public partial class LoginView : ContentPageBase
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        var content = Content;
        Content = null;
        Content = content;

        _viewModel.InvalidateMock();
    }
}
