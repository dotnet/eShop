namespace Catalog.API.Model
{
    public class CatalogKit
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string NameDE { get; set; }

        public List<CatalogItem> CatalogItems { get; set; }
    }
}
