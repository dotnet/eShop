using eShop.Catalog.API.Model;

namespace eShop.Catalog.API.Services;

public interface ITradeGeckoService
{
    Task<List<TradeGeckoProduct>> GetProductsAsync(int? limit = null, int? page = null);
    Task<TradeGeckoProduct?> GetProductAsync(int productId);
    Task<List<TradeGeckoProductVariant>> GetProductVariantsAsync(int productId);
    Task<TradeGeckoProductVariant?> GetVariantAsync(int variantId);
    Task<bool> UpdateVariantStockAsync(int variantId, int stockLevel);
} 