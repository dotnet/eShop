using System.ComponentModel.DataAnnotations;

namespace Inked.Submission.API.Model;

public class CardTheme
{
    public int Id { get; set; }

    [Required] public string Theme { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public string Accent { get; set; } = string.Empty;
}
