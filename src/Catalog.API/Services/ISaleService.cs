namespace eShop.Catalog.API.Services;

public interface ISaleService
{
    Task<List<CatalogItem>> GetSaleItems();
    Task UpdateSalePrice(int id, decimal salePrice);
    Task<List<CatalogItem>> GetItemsByGeography(string geography);
}
