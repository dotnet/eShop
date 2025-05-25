using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Pgvector;

namespace Inked.Submission.API.Model;

public class Submission
{
    [Required] public Guid Id { get; set; }

    [Required] [StringLength(40)] public string Title { get; set; }

    [Required] [StringLength(100)] public string Description { get; set; }

    [Required] public string Author { get; set; }

    [Required] public string Artitst { get; set; }

    public bool IsApproved { get; set; } = false;
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? MintedAt { get; set; } = null;

    public int MintingTypeId { get; set; } = 0;
    [ForeignKey("MintingTypeId")] public MintingType MintingType { get; set; }
    public int CardTypeId { get; set; }
    [ForeignKey("CardTypeId")] public CardType CardType { get; set; }

    public ICollection<CardTheme> CardThemes { get; set; } = new List<CardTheme>();

    [JsonIgnore] public Vector Embedding { get; set; }
}
