using eShop.HybridApp.Services;
using Microsoft.Extensions.Logging;

namespace eShop.HybridApp;

public static class MauiProgram
{
    // NOTE: Must have a trailing slash on base URLs to ensure the full BaseAddress URL is used to resolve relative URLs
    internal static string MobileBffHost = "http://localhost:11632/";


    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif


        builder.Services.AddHttpClient<CatalogService>(o => o.BaseAddress = new(MobileBffHost));
        builder.Services.AddSingleton<WebAppComponents.Services.IProductImageUrlProvider, ProductImageUrlProvider>();

        return builder.Build();
    }
}
