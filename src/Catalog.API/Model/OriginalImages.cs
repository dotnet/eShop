

using System.Reflection.Metadata;

namespace Catalog.API.Model
{
    public class OriginalImages
    {
        public int Id { get; set; }
        public string Src { get; set; }

        public int CatalogItemId { get; set; } // Required foreign key property
        public CatalogItem CatalogItem { get; set; } = null!;
    }
}
