using CommunityToolkit.Maui;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.Basket;
using eShop.ClientApp.Services.Catalog;
using eShop.ClientApp.Services.FixUri;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.Location;
using eShop.ClientApp.Services.OpenUrl;
using eShop.ClientApp.Services.Order;
using eShop.ClientApp.Services.RequestProvider;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.Services.Theme;
using eShop.ClientApp.Views;
using IdentityModel.OidcClient;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using IBrowser = IdentityModel.OidcClient.Browser.IBrowser;

namespace eShop.ClientApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        return MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureEffects(
                effects =>
                {
                })
            .UseMauiCommunityToolkit()
            .ConfigureFonts(
                fonts =>
                {
                    fonts.AddFont("FontAwesomeRegular.otf", "FontAwesome-Regular");
                    fonts.AddFont("FontAwesomeSolid.otf", "FontAwesome-Solid");
                    fonts.AddFont("Montserrat-Bold.ttf", "Montserrat-Bold");
                    fonts.AddFont("Montserrat-Regular.ttf", "Montserrat-Regular");
                })
            .ConfigureEssentials(
                essentials =>
                {
                    essentials
                        .AddAppAction(AppActions.ViewProfileAction)
                        .OnAppAction(App.HandleAppActions);
                })
#if !WINDOWS
            .UseMauiMaps()
#endif
            .RegisterAppServices()
            .RegisterViewModels()
            .RegisterViews()
            .Build();
    }

    public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<ISettingsService, SettingsService>();
        mauiAppBuilder.Services.AddSingleton<INavigationService, MauiNavigationService>();
        mauiAppBuilder.Services.AddSingleton<IDialogService, DialogService>();
        mauiAppBuilder.Services.AddSingleton<IOpenUrlService, OpenUrlService>();
        mauiAppBuilder.Services.AddSingleton<IRequestProvider>(
            sp =>
            {
                var debugHttpHandler = sp.GetKeyedService<HttpMessageHandler>("DebugHttpMessageHandler");
                return new RequestProvider(debugHttpHandler);
            });
        mauiAppBuilder.Services.AddSingleton<IIdentityService, IdentityService>(
            sp =>
            {
                var browser = sp.GetRequiredService<IBrowser>();
                var settingsService = sp.GetRequiredService<ISettingsService>();
                var debugHttpHandler = sp.GetKeyedService<HttpMessageHandler>("DebugHttpMessageHandler");
                return new IdentityService(browser, settingsService, debugHttpHandler);
            });
        mauiAppBuilder.Services.AddSingleton<IFixUriService, FixUriService>();
        mauiAppBuilder.Services.AddSingleton<ILocationService, LocationService>();

        mauiAppBuilder.Services.AddSingleton<ITheme, Theme>();

        mauiAppBuilder.Services.AddSingleton<IAppEnvironmentService, AppEnvironmentService>(
            serviceProvider =>
            {
                var requestProvider = serviceProvider.GetRequiredService<IRequestProvider>();
                var fixUriService = serviceProvider.GetRequiredService<IFixUriService>();
                var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
                var identityService = serviceProvider.GetRequiredService<IIdentityService>();

                var aes =
                    new AppEnvironmentService(
                        new BasketMockService(), new BasketService(identityService, settingsService, fixUriService),
                        new CatalogMockService(), new CatalogService(settingsService, requestProvider, fixUriService),
                        new OrderMockService(), new OrderService(identityService, settingsService, requestProvider),
                        new IdentityMockService(), identityService);

                aes.UpdateDependencies(settingsService.UseMocks);
                return aes;
            });

        mauiAppBuilder.Services.AddTransient<IBrowser, MauiAuthenticationBrowser>();

#if DEBUG
        mauiAppBuilder.Services.AddKeyedSingleton<HttpMessageHandler>(
            "DebugHttpMessageHandler",
            (sp, key) =>
            {
#if ANDROID
                var handler = new Xamarin.Android.Net.AndroidMessageHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (cert != null && cert.Issuer.Equals("CN=localhost", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    
                    return errors == System.Net.Security.SslPolicyErrors.None;
                };
                return handler;
#elif IOS || MACCATALYST
                var handler = new NSUrlSessionHandler
                {
                    TrustOverrideForUrl = (sender, url, trust) => url.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase),
                };
                return handler;
#else
                return null;
#endif
            });
        
        mauiAppBuilder.Logging.AddDebug();
#endif

        return mauiAppBuilder;
    }

    public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<MainViewModel>();
        mauiAppBuilder.Services.AddSingleton<LoginViewModel>();
        mauiAppBuilder.Services.AddSingleton<BasketViewModel>();
        mauiAppBuilder.Services.AddSingleton<CatalogViewModel>();
        mauiAppBuilder.Services.AddSingleton<CatalogItemViewModel>();
        mauiAppBuilder.Services.AddSingleton<MapViewModel>();
        mauiAppBuilder.Services.AddSingleton<ProfileViewModel>();

        mauiAppBuilder.Services.AddTransient<CheckoutViewModel>();
        mauiAppBuilder.Services.AddTransient<OrderDetailViewModel>();
        mauiAppBuilder.Services.AddTransient<SettingsViewModel>();

        return mauiAppBuilder;
    }

    public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<CatalogItemView>();

        mauiAppBuilder.Services.AddTransient<BasketView>();
        mauiAppBuilder.Services.AddTransient<CatalogView>();
        mauiAppBuilder.Services.AddTransient<CheckoutView>();
        mauiAppBuilder.Services.AddTransient<FiltersView>();
        mauiAppBuilder.Services.AddTransient<LoginView>();
        mauiAppBuilder.Services.AddTransient<OrderDetailView>();
        mauiAppBuilder.Services.AddTransient<MapView>();
        mauiAppBuilder.Services.AddTransient<ProfileView>();
        mauiAppBuilder.Services.AddTransient<SettingsView>();

        return mauiAppBuilder;
    }
}
