using System.ComponentModel.DataAnnotations;

namespace Inked.Submission.API.Model;

public class MintingType
{
    public int Id { get; set; }

    [Required] public string Type { get; set; }
}
