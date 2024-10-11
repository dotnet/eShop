using eShop.Catalog.API.Model;
using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.API.Services;

public class SaleService : ISaleService
{
    private readonly CatalogContext _context;

    public SaleService(CatalogContext context)
    {
        _context = context;
    }

    public async Task<List<CatalogItem>> GetSaleItems()
    {
        return await _context.CatalogItems.Where(item => item.IsOnSale).ToListAsync();
    }

    public async Task UpdateSalePrice(int id, decimal salePrice)
    {
        var item = await _context.CatalogItems.SingleOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            throw new ArgumentException($"Item with id {id} not found.");
        }

        item.SalePrice = salePrice;
        item.IsOnSale = true;
        item.DiscountPercentage = ((item.Price - salePrice) / item.Price) * 100;

        await _context.SaveChangesAsync();
    }

    public async Task<List<CatalogItem>> GetItemsByGeography(string geography)
    {
        return await _context.CatalogItems.Where(item => item.Geography == geography).ToListAsync();
    }
}
