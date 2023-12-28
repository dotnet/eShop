namespace Webhooks.API.Model;

public class WebhookSubscription
{
    public int Id { get; set; }

    public WebhookType Type { get; set; }
    public DateTime Date { get; set; }
    [Required]
    public string DestUrl { get; set; } = null!;
    public string? Token { get; set; }
    [Required]
    public string? UserId { get; set; }
}
