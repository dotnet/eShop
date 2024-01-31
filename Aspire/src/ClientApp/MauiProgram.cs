using CommunityToolkit.Maui;
using eShop.ClientApp.Services;
using eShop.ClientApp.Services.AppEnvironment;
using eShop.ClientApp.Services.Basket;
using eShop.ClientApp.Services.Catalog;
using eShop.ClientApp.Services.FixUri;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.Location;
using eShop.ClientApp.Services.Marketing;
using eShop.ClientApp.Services.OpenUrl;
using eShop.ClientApp.Services.Order;
using eShop.ClientApp.Services.RequestProvider;
using eShop.ClientApp.Services.Settings;
using eShop.ClientApp.Services.Theme;
using eShop.ClientApp.Services.User;
using eShop.ClientApp.Views;
using Microsoft.Extensions.Logging;

namespace eShop.ClientApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
        => MauiApp
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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Font_Awesome_5_Free-Regular-400.otf", "FontAwesome-Regular");
                    fonts.AddFont("Font_Awesome_5_Free-Solid-900.otf", "FontAwesome-Solid");
                    fonts.AddFont("Montserrat-Bold.ttf", "Montserrat-Bold");
                    fonts.AddFont("Montserrat-Regular.ttf", "Montserrat-Regular");
                    fonts.AddFont("SourceSansPro-Regular.ttf", "SourceSansPro-Regular");
                    fonts.AddFont("SourceSansPro-Solid.ttf", "SourceSansPro-Solid");
                })
            .ConfigureEssentials(
                essentials =>
                {
                    essentials
                        .AddAppAction(AppActions.ViewProfileAction)
                        .OnAppAction(App.HandleAppActions);
                })
            .UseMauiMaps()
            .RegisterAppServices()
            .RegisterViewModels()
            .RegisterViews()
            .Build();

    public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddSingleton<ISettingsService, SettingsService>();
        mauiAppBuilder.Services.AddSingleton<INavigationService, MauiNavigationService>();
        mauiAppBuilder.Services.AddSingleton<IDialogService, DialogService>();
        mauiAppBuilder.Services.AddSingleton<IOpenUrlService, OpenUrlService>();
        mauiAppBuilder.Services.AddSingleton<IRequestProvider, RequestProvider>();
        mauiAppBuilder.Services.AddSingleton<IIdentityService, IdentityService>();
        mauiAppBuilder.Services.AddSingleton<IFixUriService, FixUriService>();
        mauiAppBuilder.Services.AddSingleton<ILocationService, LocationService>();

        mauiAppBuilder.Services.AddSingleton<ITheme, Theme>();

        mauiAppBuilder.Services.AddSingleton<IAppEnvironmentService, AppEnvironmentService>(
            serviceProvider =>
            {
                var requestProvider = serviceProvider.GetService<IRequestProvider>();
                var fixUriService = serviceProvider.GetService<IFixUriService>();
                var settingsService = serviceProvider.GetService<ISettingsService>();

                var aes =
                    new AppEnvironmentService(
                        new BasketMockService(), new BasketService(requestProvider, fixUriService),
                        new CampaignMockService(), new CampaignService(requestProvider, fixUriService),
                        new CatalogMockService(), new CatalogService(requestProvider, fixUriService),
                        new OrderMockService(), new OrderService(requestProvider),
                        new UserMockService(), new UserService(requestProvider));

                aes.UpdateDependencies(settingsService.UseMocks);
                return aes;
            });

#if DEBUG
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
        mauiAppBuilder.Services.AddSingleton<MapViewModel>();
        mauiAppBuilder.Services.AddSingleton<ProfileViewModel>();

        mauiAppBuilder.Services.AddTransient<CheckoutViewModel>();
        mauiAppBuilder.Services.AddTransient<OrderDetailViewModel>();
        mauiAppBuilder.Services.AddTransient<SettingsViewModel>();
        mauiAppBuilder.Services.AddTransient<CampaignViewModel>();
        mauiAppBuilder.Services.AddTransient<CampaignDetailsViewModel>();

        return mauiAppBuilder;
    }

    public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddTransient<BasketView>();
        mauiAppBuilder.Services.AddTransient<CampaignDetailsView>();
        mauiAppBuilder.Services.AddTransient<CampaignView>();
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
