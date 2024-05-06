using Microsoft.Maui.Controls.Maps;

namespace eShop.ClientApp.Views;

public partial class MapView : ContentPageBase
{
    public MapView(MapViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    async void Pin_MarkerClicked(System.Object sender, Microsoft.Maui.Controls.Maps.PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;

        if (sender is not Pin pin)
        {
            return;
        }

        await pin.Location.OpenMapsAsync();
    }
}
