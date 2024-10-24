using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.Location;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.ViewModels.Base;
using Location = eShop.ClientApp.Models.Location.Location;

namespace eShop.ClientApp.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    //Needed if using Android Emulator Locally. See https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-8.0#android
    private static string _baseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "10.0.2.2" : "localhost";
    
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly ILocationService _locationService;
    private readonly ISettingsService _settingsService;
    private bool _allowGpsLocation;
    private string _gatewayBasketEndpoint;
    private string _gatewayCatalogEndpoint;
    private string _gatewayOrdersEndpoint;
    private string _gpsWarningMessage;
    private string _identityEndpoint;
    private double _latitude;
    private double _longitude;

    private bool _useAzureServices;
    private bool _useFakeLocation;

    public SettingsViewModel(
        ILocationService locationService, IAppEnvironmentService appEnvironmentService,
        INavigationService navigationService, ISettingsService settingsService)
        : base(navigationService)
    {
        _settingsService = settingsService;
        _locationService = locationService;
        _appEnvironmentService = appEnvironmentService;

        _useAzureServices = !_settingsService.UseMocks;
        _identityEndpoint = _settingsService.IdentityEndpointBase;
        _latitude = double.Parse(_settingsService.Latitude, CultureInfo.CurrentCulture);
        _longitude = double.Parse(_settingsService.Longitude, CultureInfo.CurrentCulture);
        _useFakeLocation = _settingsService.UseFakeLocation;
        _allowGpsLocation = _settingsService.AllowGpsLocation;
        _gpsWarningMessage = string.Empty;

        IdentityEndpoint =
            !string.IsNullOrEmpty(_settingsService.IdentityEndpointBase)
                ? _settingsService.IdentityEndpointBase
                : $"https://{_baseAddress}:5243";

        GatewayCatalogEndpoint =
            !string.IsNullOrEmpty(_settingsService.GatewayCatalogEndpointBase)
                ? _settingsService.GatewayCatalogEndpointBase
                : $"http://{_baseAddress}:11632";

        GatewayBasketEndpoint =
            !string.IsNullOrEmpty(_settingsService.GatewayBasketEndpointBase)
                ? _settingsService.GatewayBasketEndpointBase
                : $"http://{_baseAddress}:5221";

        GatewayOrdersEndpoint =
            !string.IsNullOrEmpty(_settingsService.GatewayOrdersEndpointBase)
                ? _settingsService.GatewayOrdersEndpointBase
                : $"http://{_baseAddress}:11632";

        ToggleMockServicesCommand = new RelayCommand(ToggleMockServices);

        ToggleFakeLocationCommand = new RelayCommand(ToggleFakeLocation);

        ToggleSendLocationCommand = new AsyncRelayCommand(ToggleSendLocationAsync);

        ToggleAllowGpsLocationCommand = new RelayCommand(ToggleAllowGpsLocation);

        UseAzureServices = !_settingsService.UseMocks;
    }

    public string TitleUseAzureServices => "Use Microservices/Containers from eShop";

    public string DescriptionUseAzureServices => !UseAzureServices
        ? "Currently using mock services that are simulated objects that mimic the behavior of real services using a controlled approach. Toggle on to configure the use of microserivces/containers."
        : "When enabling the use of microservices/containers, the app will attempt to use real services deployed as Docker/Kubernetes containers at the specified base endpoint, which will must be reachable through the network.";

    public bool UseAzureServices
    {
        get => _useAzureServices;
        set
        {
            SetProperty(ref _useAzureServices, value);
            UpdateUseAzureServices();
        }
    }

    public string TitleUseFakeLocation => !UseFakeLocation
        ? "Use Real Location"
        : "Use Fake Location";

    public string DescriptionUseFakeLocation => !UseFakeLocation
        ? "When enabling location, the app will attempt to use the location from the device."
        : "Fake Location data is added for marketing campaign testing.";

    public bool UseFakeLocation
    {
        get => _useFakeLocation;
        set
        {
            SetProperty(ref _useFakeLocation, value);
            UpdateFakeLocation();
        }
    }

    public string TitleAllowGpsLocation => !AllowGpsLocation
        ? "GPS Location Disabled"
        : "GPS Location Enabled";

    public string DescriptionAllowGpsLocation => !AllowGpsLocation
        ? "When disabling location, you won't receive location campaigns based upon your location."
        : "When enabling location, you'll receive location campaigns based upon your location.";

    public string GpsWarningMessage
    {
        get => _gpsWarningMessage;
        set => SetProperty(ref _gpsWarningMessage, value);
    }

    public string IdentityEndpoint
    {
        get => _identityEndpoint;
        set
        {
            SetProperty(ref _identityEndpoint, value);
            if (!string.IsNullOrEmpty(value))
            {
                UpdateIdentityEndpoint();
            }
        }
    }

    public string GatewayCatalogEndpoint
    {
        get => _gatewayCatalogEndpoint;
        set
        {
            SetProperty(ref _gatewayCatalogEndpoint, value);
            if (!string.IsNullOrEmpty(value))
            {
                UpdateGatewayShoppingEndpoint();
            }
        }
    }

    public string GatewayOrdersEndpoint
    {
        get => _gatewayOrdersEndpoint;
        set
        {
            SetProperty(ref _gatewayOrdersEndpoint, value);
            if (!string.IsNullOrEmpty(value))
            {
                UpdateGatewayOrdersEndpoint();
            }
        }
    }

    public string GatewayBasketEndpoint
    {
        get => _gatewayBasketEndpoint;
        set
        {
            SetProperty(ref _gatewayBasketEndpoint, value);
            if (!string.IsNullOrEmpty(value))
            {
                UpdateGatewayBasketEndpoint();
            }
        }
    }

    public double Latitude
    {
        get => _latitude;
        set
        {
            SetProperty(ref _latitude, value);
            UpdateLatitude();
        }
    }

    public double Longitude
    {
        get => _longitude;
        set
        {
            SetProperty(ref _longitude, value);
            UpdateLongitude();
        }
    }

    public bool AllowGpsLocation
    {
        get => _allowGpsLocation;
        set => SetProperty(ref _allowGpsLocation, value);
    }

    public ICommand ToggleMockServicesCommand { get; }

    public ICommand ToggleFakeLocationCommand { get; }

    public ICommand ToggleSendLocationCommand { get; }

    public ICommand ToggleAllowGpsLocationCommand { get; }

    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(AllowGpsLocation))
        {
            await UpdateAllowGpsLocation();
        }
    }

    private void ToggleMockServices()
    {
        _appEnvironmentService.UpdateDependencies(!UseAzureServices);

        OnPropertyChanged(nameof(TitleUseAzureServices));
        OnPropertyChanged(nameof(DescriptionUseAzureServices));
    }

    private void ToggleFakeLocation()
    {
        _appEnvironmentService.UpdateDependencies(!UseAzureServices);
        OnPropertyChanged(nameof(TitleUseFakeLocation));
        OnPropertyChanged(nameof(DescriptionUseFakeLocation));
    }

    private async Task ToggleSendLocationAsync()
    {
        if (!_settingsService.UseMocks)
        {
            var locationRequest = new Location {Latitude = _latitude, Longitude = _longitude};

            await _locationService.UpdateUserLocation(locationRequest);
        }
    }

    private void ToggleAllowGpsLocation()
    {
        OnPropertyChanged(nameof(TitleAllowGpsLocation));
        OnPropertyChanged(nameof(DescriptionAllowGpsLocation));
    }

    private void UpdateUseAzureServices()
    {
        // Save use mocks services to local storage
        _settingsService.UseMocks = !UseAzureServices;
    }

    private void UpdateIdentityEndpoint()
    {
        // Update remote endpoint (save to local storage)
        _settingsService.IdentityEndpointBase = _identityEndpoint;
    }

    private void UpdateGatewayShoppingEndpoint()
    {
        _settingsService.GatewayCatalogEndpointBase = _gatewayCatalogEndpoint;
    }
    
    private void UpdateGatewayOrdersEndpoint()
    {
        _settingsService.GatewayOrdersEndpointBase = _gatewayOrdersEndpoint;
    }
    
    private void UpdateGatewayBasketEndpoint()
    {
        _settingsService.GatewayBasketEndpointBase = _gatewayBasketEndpoint;
    }

    private void UpdateFakeLocation()
    {
        _settingsService.UseFakeLocation = _useFakeLocation;
    }

    private void UpdateLatitude()
    {
        // Update fake latitude (save to local storage)
        _settingsService.Latitude = _latitude.ToString();
    }

    private void UpdateLongitude()
    {
        // Update fake longitude (save to local storage)
        _settingsService.Longitude = _longitude.ToString();
    }

    private async Task UpdateAllowGpsLocation()
    {
        if (_allowGpsLocation)
        {
            bool hasWhenInUseLocationPermissions;
            bool hasBackgroundLocationPermissions;

            if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
            {
                hasWhenInUseLocationPermissions = await Permissions.RequestAsync<Permissions.LocationWhenInUse>() ==
                                                  PermissionStatus.Granted;
            }
            else
            {
                hasWhenInUseLocationPermissions = true;
            }

            if (await Permissions.CheckStatusAsync<Permissions.LocationAlways>() != PermissionStatus.Granted)
            {
                hasBackgroundLocationPermissions = await Permissions.RequestAsync<Permissions.LocationAlways>() ==
                                                   PermissionStatus.Granted;
            }
            else
            {
                hasBackgroundLocationPermissions = true;
            }


            if (!hasWhenInUseLocationPermissions || !hasBackgroundLocationPermissions)
            {
                _allowGpsLocation = false;
                GpsWarningMessage = "Enable the GPS sensor on your device";
            }
            else
            {
                _settingsService.AllowGpsLocation = _allowGpsLocation;
                GpsWarningMessage = string.Empty;
            }
        }
        else
        {
            _settingsService.AllowGpsLocation = _allowGpsLocation;
        }
    }
}
