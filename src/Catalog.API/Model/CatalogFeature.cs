namespace Catalog.API.Model
{
    public class CatalogFeature
    {
        public int Id { get; set; }
        public string Icon { get; set; }

        public string TitleEN { get; set; }

        public string TitleDE { get; set; }

        public string ValueEN { get; set; }

        public string ValueDE { get; set; }

        public List<CatalogItem> CatalogItems { get; set; }
    }
}
