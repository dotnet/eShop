using CommunityToolkit.Mvvm.Messaging;

namespace eShop.ClientApp.Views;

public partial class CatalogView : ContentPageBase
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
            .Register<CatalogView, Messages.AddProductMessage>(
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

        WeakReferenceMessenger.Default.Unregister<Messages.AddProductMessage>(this);
    }
}
