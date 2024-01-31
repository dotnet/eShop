namespace Webhooks.API.Services;

public class WebhooksRetriever(WebhooksContext db) : IWebhooksRetriever
{
    public async Task<IEnumerable<WebhookSubscription>> GetSubscriptionsOfType(WebhookType type)
    {
        return await db.Subscriptions.Where(s => s.Type == type).ToListAsync();
    }
}
