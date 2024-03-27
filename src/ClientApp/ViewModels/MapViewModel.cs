using eShop.ClientApp.Services;
using eShop.ClientApp.ViewModels.Base;

namespace eShop.ClientApp.ViewModels;

public partial class MapViewModel : ViewModelBase
{
    [ObservableProperty] private IEnumerable<Store> _stores;

    public MapViewModel(INavigationService navigationService)
        : base(navigationService)
    {
    }

    public override Task InitializeAsync()
    {
        Stores =
            new[]
            {
                new Store
                {
                    Address = "Building 92, Redmond, WA",
                    Description = "Microsoft Visitor Center",
                    Location = new Location(47.6423109, -122.1368406)
                }
            };

        return Task.CompletedTask;
    }
}

public record Store
{
    public Location Location { get; set; }
    public string Address { get; set; }
    public string Description { get; set; }
}
