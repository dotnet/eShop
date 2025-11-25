using System.ComponentModel.DataAnnotations;

namespace EShop.Persistence.Models.Catalog;

public class CatalogType
{
    public int Id { get; set; }

    [Required]
    public string Type { get; set; }
}