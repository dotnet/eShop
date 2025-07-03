using eShop.Ordering.API.Models;

namespace eShop.Ordering.API.Services;

public interface ITradeGeckoService
{
    Task<List<TradeGeckoOrder>> GetOrdersAsync(int? limit = null, int? page = null);
    Task<TradeGeckoOrder?> GetOrderAsync(int orderId);
    Task<TradeGeckoOrder?> CreateOrderAsync(TradeGeckoOrder order);
    Task<bool> UpdateOrderStatusAsync(int orderId, string financialStatus, string fulfillmentStatus);
} 