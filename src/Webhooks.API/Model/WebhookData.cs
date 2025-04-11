namespace Webhooks.API.Model;

public class WebhookData
{
    public WebhookData(WebhookType hookType, object data)
    {
        When = DateTime.UtcNow;
        Type = hookType.ToString();
        Payload = JsonSerializer.Serialize(data);
    }

    public DateTime When { get; }

    public string Payload { get; }

    public string Type { get; }
}
