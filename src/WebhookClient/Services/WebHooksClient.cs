namespace WebhookClient.Services;

public class WebhooksClient(HttpClient client)
{
    public Task<HttpResponseMessage> AddWebHookAsync(WebhookSubscriptionRequest payload)
    {
        return client.PostAsJsonAsync("/api/v1/webhooks", payload);
    }

    public async Task<IEnumerable<WebhookResponse>> LoadWebhooks()
    {
        return await client.GetFromJsonAsync<IEnumerable<WebhookResponse>>("/api/v1/webhooks") ?? [];
    }
}
