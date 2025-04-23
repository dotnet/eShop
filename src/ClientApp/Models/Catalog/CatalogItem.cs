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
    
    public int? PromotionalDiscountPercentage { get; set; }
    public string PromotionalDiscountDescription { get; set; }
    public DateTime? PromotionStartDate { get; set; }
    public DateTime? PromotionEndDate { get; set; }
    public string BadgeDesign { get; set; }
}