using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace eShop.ClientApp.Views;

public partial class MapView
{
    public MapView(MapViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();

        var map = new Map(new MapSpan(new Location(0, 0), 0, 0));
    }

    private async void Pin_MarkerClicked(Object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;

        if (sender is not Pin pin)
        {
            return;
        }

        await pin.Location.OpenMapsAsync();
    }
}
