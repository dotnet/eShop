using System.Diagnostics;
using CoreImage;

namespace eShop.ClientApp.Views;

public partial class LoginView : ContentPageBase
{
    private readonly LoginViewModel _viewModel;

    private bool _animate;
    // private bool _scrollToEnd = false;
    
    public LoginView(LoginViewModel viewModel)
    {
        BindingContext = _viewModel = viewModel;
        InitializeComponent();
        BannerScroll.ScrollToAsync(0, BannerScroll.ContentSize.Height, false);
    }

    protected override void OnAppearing()
    {
        var content = Content;
        Content = null;
        Content = content;

        _viewModel.InvalidateMock();

        if (!_viewModel.IsMock)
        {
            _animate = true;
            AnimateIn().ContinueWith(x => { });
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

        await Task.Delay(10);

        /*
        while (_animate)
        {
            _scrollToEnd = !_scrollToEnd;
            if (_scrollToEnd)
            {
                await Task.WhenAll(
                    BannerScroll.TransitionTo($"animate_to_end", x => BannerScroll.ScrollToAsync(x, BannerScroll.ScrollY, false), BannerScroll.ScrollX,  BannerScroll.ContentSize.Width, length: 30_000U, easing: Easing.CubicInOut),
                    BannerScroll.TransitionTo(x => x.ScaleX, 1.75d, length: 30_000U, easing: Easing.CubicInOut));
                continue;
            }
            
            await Task.WhenAll(
                BannerScroll.TransitionTo($"animate_to_end", x => BannerScroll.ScrollToAsync(x, BannerScroll.ScrollY, false), BannerScroll.ScrollX, 0, length: 30_000U, easing: Easing.CubicInOut),
                BannerScroll.TransitionTo(x => x.ScaleX, 1.0d, length: 30_000U, easing: Easing.CubicInOut));
        }
        */
    }

    private async Task AnimateItem(View uiElement, uint duration)
    {
        try
        {
            while (_animate)
            {
                await uiElement.ScaleTo(2.05, duration, Easing.SinInOut);
                await Task.WhenAll(
                    uiElement.FadeTo(1, duration, Easing.SinInOut),
                    uiElement.LayoutTo(new Rect(new Point(0, 0), new Size(uiElement.Width, uiElement.Height))),
                    uiElement.ScaleTo(.15, duration, Easing.SinInOut)
                );
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
