using eShop.Basket.API.Model;

namespace eShop.BddTests.Support;

public interface IInMemoryBasketRepository
{
    Task<CustomerBasket?> GetBasketAsync(string customerId);
    Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string customerId);
    Task ClearAllAsync();
    Task ClearScenarioDataAsync();
}

public class InMemoryBasketRepository : IInMemoryBasketRepository
{
    private readonly Dictionary<string, CustomerBasket> _baskets = new();
    private readonly List<string> _seededBasketIds = new();
    private readonly List<string> _scenarioBasketIds = new();

    public async Task<CustomerBasket?> GetBasketAsync(string customerId)
    {
        _baskets.TryGetValue(customerId, out var basket);
        return await Task.FromResult(basket);
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        _baskets[basket.BuyerId] = basket;
        
        // Track scenario-created baskets
        if (!_seededBasketIds.Contains(basket.BuyerId) && !_scenarioBasketIds.Contains(basket.BuyerId))
        {
            _scenarioBasketIds.Add(basket.BuyerId);
        }
        
        return await Task.FromResult(basket);
    }

    public async Task<bool> DeleteBasketAsync(string customerId)
    {
        var removed = _baskets.Remove(customerId);
        _scenarioBasketIds.Remove(customerId);
        return await Task.FromResult(removed);
    }

    public async Task ClearAllAsync()
    {
        _baskets.Clear();
        _seededBasketIds.Clear();
        _scenarioBasketIds.Clear();
        await Task.CompletedTask;
    }

    public async Task ClearScenarioDataAsync()
    {
        // Only remove baskets created during scenarios, not seeded data
        foreach (var basketId in _scenarioBasketIds.ToList())
        {
            _baskets.Remove(basketId);
            _scenarioBasketIds.Remove(basketId);
        }
        await Task.CompletedTask;
    }

    public void TrackSeededBasket(string customerId)
    {
        if (!_seededBasketIds.Contains(customerId))
        {
            _seededBasketIds.Add(customerId);
        }
    }
}