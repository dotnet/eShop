namespace eShop.ClientApp.Services.OpenUrl;

public class OpenUrlService : IOpenUrlService
{
    public async Task OpenUrl(string url)
    {
        if (await Launcher.CanOpenAsync(url))
        {
            await Launcher.OpenAsync(url);
        }
    }
}
