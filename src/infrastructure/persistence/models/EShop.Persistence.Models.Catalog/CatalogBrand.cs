using System.ComponentModel.DataAnnotations;

namespace EShop.Persistence.Models.Catalog
{
    public class CatalogBrand
    {
        public int Id { get; set; }

        [Required]
        public string Brand { get; set; }
    }
}
