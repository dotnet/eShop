using System.ComponentModel.DataAnnotations;

namespace Inked.Submission.API.Model;

public class CardType
{
    public int Id { get; set; }

    [Required] public string Type { get; set; }

    public string Kanji { get; set; } = string.Empty;
}
