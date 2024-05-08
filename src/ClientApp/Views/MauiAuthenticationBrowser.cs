using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;
using IBrowser = IdentityModel.OidcClient.Browser.IBrowser;

namespace eShop.ClientApp.Views;

public class MauiAuthenticationBrowser : IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(options.StartUrl),
                new Uri(options.EndUrl));

            var url = new RequestUrl("maui://authcallback")
                .Create(new Parameters(result.Properties));

            return new BrowserResult {Response = url, ResultType = BrowserResultType.Success};
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult {ResultType = BrowserResultType.UserCancel};
        }
    }
}
