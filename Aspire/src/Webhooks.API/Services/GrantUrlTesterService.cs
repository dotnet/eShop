namespace Webhooks.API.Services;

class GrantUrlTesterService(IHttpClientFactory factory, ILogger<IGrantUrlTesterService> logger) : IGrantUrlTesterService
{
    public async Task<bool> TestGrantUrl(string urlHook, string url, string token)
    {
        if (!CheckSameOrigin(urlHook, url))
        {
            logger.LogWarning("Url of the hook ({UrlHook} and the grant url ({Url} do not belong to same origin)", urlHook, url);
            return false;
        }

        var client = factory.CreateClient();
        var msg = new HttpRequestMessage(HttpMethod.Options, url);
        msg.Headers.Add("X-eshop-whtoken", token);

        logger.LogInformation("Sending the OPTIONS message to {Url} with token \"{Token}\"", url, token ?? string.Empty);

        try
        {
            var response = await client.SendAsync(msg);
            var tokenReceived = response.Headers.TryGetValues("X-eshop-whtoken", out var tokenValues) ? tokenValues.FirstOrDefault() : null;
            var tokenExpected = string.IsNullOrWhiteSpace(token) ? null : token;

            logger.LogInformation("Response code is {StatusCode} for url {Url} and token in header was {TokenReceived} (expected token was {TokenExpected})", response.StatusCode, url, tokenReceived, tokenExpected);

            return response.IsSuccessStatusCode && tokenReceived == tokenExpected;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Exception {TypeName} when sending OPTIONS request. Url can't be granted.", ex.GetType().Name);

            return false;
        }
    }

    private static bool CheckSameOrigin(string urlHook, string url)
    {
        var firstUrl = new Uri(urlHook, UriKind.Absolute);
        var secondUrl = new Uri(url, UriKind.Absolute);

        return firstUrl.Scheme == secondUrl.Scheme &&
            firstUrl.Port == secondUrl.Port &&
            firstUrl.Host == secondUrl.Host;
    }
}
