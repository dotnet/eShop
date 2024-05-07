using CommunityToolkit.Mvvm.Messaging;
using eShop.ClientApp.Messages;

namespace eShop.ClientApp.Views;

public partial class CatalogView
{
    public CatalogView(CatalogViewModel viewModel)
    {
        BindingContext = viewModel;

        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        WeakReferenceMessenger.Default
            .Register<CatalogView, ProductCountChangedMessage>(
                this,
                async (recipient, message) =>
                {
                    await recipient.Dispatcher.DispatchAsync(
                        async () =>
                        {
                            await recipient.badge.ScaleTo(1.2);
                            await recipient.badge.ScaleTo(1.0);
                        });
                });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        WeakReferenceMessenger.Default.Unregister<ProductCountChangedMessage>(this);
    }

    private void Products_OnScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        if (e.VerticalOffset < 0)
        {
            return;
        }

        if (e.VerticalOffset > 200)
        {
            HeaderImage.Opacity = 0d;
            HeaderImage.Scale = 2.0;
            return;
        }

        HeaderImage.Opacity = 1.0d - (e.VerticalOffset / 200d);
        HeaderImage.Scale = 1.0d + (1.0d * (e.VerticalOffset / 200d));
    }
}
