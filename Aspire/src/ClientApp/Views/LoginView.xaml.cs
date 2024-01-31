using System.Diagnostics;

namespace eShop.ClientApp.Views;

public partial class LoginView : ContentPageBase
{
    private readonly LoginViewModel _viewModel;

    private bool _animate;

    public LoginView(LoginViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        var content = Content;
        Content = null;
        Content = content;

        _viewModel.InvalidateMock();

        if (!_viewModel.IsMock)
        {
            _animate = true;
            await AnimateIn();
        }
    }

    protected override void OnDisappearing()
    {
        _animate = false;
    }

    public async Task AnimateIn()
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            return;
        }

        await AnimateItem(Banner, 10500);
    }

    private async Task AnimateItem(View uiElement, uint duration)
    {
        try
        {
            while (_animate)
            {
                await uiElement.ScaleTo(1.05, duration, Easing.SinInOut);
                await Task.WhenAll(
                    uiElement.FadeTo(1, duration, Easing.SinInOut),
                    uiElement.LayoutTo(new Rect(new Point(0, 0), new Size(uiElement.Width, uiElement.Height))),
                    uiElement.FadeTo(.9, duration, Easing.SinInOut),
                    uiElement.ScaleTo(1.15, duration, Easing.SinInOut)
                );
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
