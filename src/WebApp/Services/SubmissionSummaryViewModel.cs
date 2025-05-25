namespace Inked.WebApp.Services;

public class SubmissionSummaryViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string CardTypeName { get; set; } = string.Empty;
    public List<string> ThemeNames { get; set; } = new();
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
