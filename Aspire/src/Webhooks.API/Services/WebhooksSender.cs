namespace Webhooks.API.Services;

public class WebhooksSender(IHttpClientFactory httpClientFactory, ILogger<WebhooksSender> logger) : IWebhooksSender
{
    public async Task SendAll(IEnumerable<WebhookSubscription> receivers, WebhookData data)
    {
        var client = httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(data);
        var tasks = receivers.Select(r => OnSendData(r, json, client));
        await Task.WhenAll(tasks);
    }

    private Task OnSendData(WebhookSubscription subs, string jsonData, HttpClient client)
    {
        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(subs.DestUrl, UriKind.Absolute),
            Method = HttpMethod.Post,
            Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(subs.Token))
        {
            request.Headers.Add("X-eshop-whtoken", subs.Token);
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Sending hook to {DestUrl} of type {Type}", subs.DestUrl, subs.Type);
        }

        return client.SendAsync(request);
    }

}
