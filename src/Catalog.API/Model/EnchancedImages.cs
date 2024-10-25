namespace Catalog.API.Model
{
    public class EnchancedImages
    {

        public int Id { get; set; }
        public string Src { get; set; }

        public int CatalogItemId { get; set; } // Required foreign key property
        public CatalogItem CatalogItem { get; set; } = null!;
    }
}
