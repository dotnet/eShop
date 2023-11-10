namespace WebhookClient.Services;

public interface IWebhooksClient
{
    Task<HttpResponseMessage> AddWebHookAsync(WebhookSubscriptionRequest payload);
    Task<IEnumerable<WebhookResponse>> LoadWebhooks();
}
