using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EShop.Persistence.Models.Catalog
{
    public class CatalogItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? PictureFileName { get; set; }

        public int AvailableStock { get; set; }
        public int RestockThreshold { get; set; }
        public int MaxStockThreshold { get; set; }
        public bool OnReorder { get; set; }

        [ForeignKey(nameof(CatalogBrandId))]
        public int CatalogBrandId { get; set; }
        public CatalogBrand? CatalogBrand { get; set; }

        [Required]
        [ForeignKey(nameof(CatalogId))]
        public int CatalogId { get; set; }
        public Catalog Catalog { get; set; }
    }
}