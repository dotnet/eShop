namespace eShop.ClientApp.Models.Catalog;

public class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string PictureUri { get; set; }
    public int CatalogBrandId { get; set; }
    public CatalogBrand CatalogBrand { get; set; }
    public int CatalogTypeId { get; set; }
    public CatalogType CatalogType { get; set; }
    public decimal? SalePrice { get; set; }
    public bool IsOnSale { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string Geography { get; set; }
}
