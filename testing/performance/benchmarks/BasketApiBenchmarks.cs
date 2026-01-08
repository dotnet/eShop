using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using eShop.Basket.API.Model;
using eShop.Basket.API.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace eShop.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BasketApiBenchmarks
{
    private IBasketRepository _basketRepository = null!;
    private CustomerBasket _testBasket = null!;
    private List<CustomerBasket> _testBaskets = null!;
    private ILogger<BasketApiBenchmarks> _logger = null!;

    [GlobalSetup]
    public void Setup()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BasketApiBenchmarks>();
        _basketRepository = new InMemoryBasketRepository();
        
        // Create test data
        _testBasket = CreateTestBasket("benchmark-user");
        _testBaskets = CreateTestBaskets(100);
        
        // Pre-populate repository
        foreach (var basket in _testBaskets)
        {
            _basketRepository.UpdateBasketAsync(basket).Wait();
        }
    }

    [BenchmarkCategory("BasketOperations")]
    [Benchmark(Baseline = true)]
    public async Task<CustomerBasket> GetBasket_SingleUser()
    {
        return await _basketRepository.GetBasketAsync("benchmark-user");
    }

    [BenchmarkCategory("BasketOperations")]
    [Benchmark]
    public async Task<CustomerBasket> UpdateBasket_SingleItem()
    {
        var basket = CreateTestBasket($"update-user-{Guid.NewGuid()}");
        return await _basketRepository.UpdateBasketAsync(basket);
    }

    [BenchmarkCategory("BasketOperations")]
    [Benchmark]
    public async Task<CustomerBasket> UpdateBasket_MultipleItems()
    {
        var basket = CreateTestBasketWithMultipleItems($"multi-user-{Guid.NewGuid()}", 10);
        return await _basketRepository.UpdateBasketAsync(basket);
    }

    [BenchmarkCategory("BasketOperations")]
    [Benchmark]
    public async Task<bool> DeleteBasket_ExistingUser()
    {
        var userId = $"delete-user-{Guid.NewGuid()}";
        var basket = CreateTestBasket(userId);
        await _basketRepository.UpdateBasketAsync(basket);
        
        return await _basketRepository.DeleteBasketAsync(userId);
    }

    [BenchmarkCategory("BasketSerialization")]
    [Benchmark]
    public string SerializeBasket_SmallBasket()
    {
        var basket = CreateTestBasket("serialize-user");
        return JsonSerializer.Serialize(basket);
    }

    [BenchmarkCategory("BasketSerialization")]
    [Benchmark]
    public string SerializeBasket_LargeBasket()
    {
        var basket = CreateTestBasketWithMultipleItems("large-user", 50);
        return JsonSerializer.Serialize(basket);
    }

    [BenchmarkCategory("BasketSerialization")]
    [Benchmark]
    public CustomerBasket DeserializeBasket_SmallBasket()
    {
        var json = JsonSerializer.Serialize(CreateTestBasket("deserialize-user"));
        return JsonSerializer.Deserialize<CustomerBasket>(json)!;
    }

    [BenchmarkCategory("BasketCalculations")]
    [Benchmark]
    public decimal CalculateBasketTotal_SmallBasket()
    {
        var basket = CreateTestBasket("calc-user");
        return basket.Items.Sum(item => item.UnitPrice * item.Quantity);
    }

    [BenchmarkCategory("BasketCalculations")]
    [Benchmark]
    public decimal CalculateBasketTotal_LargeBasket()
    {
        var basket = CreateTestBasketWithMultipleItems("calc-large-user", 100);
        return basket.Items.Sum(item => item.UnitPrice * item.Quantity);
    }

    [BenchmarkCategory("BasketValidation")]
    [Benchmark]
    public bool ValidateBasket_SmallBasket()
    {
        var basket = CreateTestBasket("validate-user");
        return ValidateBasket(basket);
    }

    [BenchmarkCategory("BasketValidation")]
    [Benchmark]
    public bool ValidateBasket_LargeBasket()
    {
        var basket = CreateTestBasketWithMultipleItems("validate-large-user", 50);
        return ValidateBasket(basket);
    }

    [BenchmarkCategory("ConcurrentOperations")]
    [Benchmark]
    public async Task ConcurrentBasketUpdates_10Users()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var userId = $"concurrent-user-{i}";
            var basket = CreateTestBasket(userId);
            tasks.Add(_basketRepository.UpdateBasketAsync(basket));
        }
        await Task.WhenAll(tasks);
    }

    [BenchmarkCategory("ConcurrentOperations")]
    [Benchmark]
    public async Task ConcurrentBasketReads_10Users()
    {
        var tasks = new List<Task<CustomerBasket>>();
        for (int i = 0; i < 10; i++)
        {
            var userId = $"user-{i % _testBaskets.Count}";
            tasks.Add(_basketRepository.GetBasketAsync(userId));
        }
        await Task.WhenAll(tasks);
    }

    [BenchmarkCategory("BasketSearch")]
    [Benchmark]
    public List<BasketItem> SearchBasketItems_ByProductName()
    {
        var basket = CreateTestBasketWithMultipleItems("search-user", 20);
        var searchTerm = "Product 5";
        return basket.Items.Where(item => item.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    [BenchmarkCategory("BasketSearch")]
    [Benchmark]
    public List<BasketItem> SearchBasketItems_ByPriceRange()
    {
        var basket = CreateTestBasketWithMultipleItems("price-search-user", 30);
        var minPrice = 50m;
        var maxPrice = 150m;
        return basket.Items.Where(item => item.UnitPrice >= minPrice && item.UnitPrice <= maxPrice).ToList();
    }

    // Helper methods
    private CustomerBasket CreateTestBasket(string buyerId)
    {
        return new CustomerBasket
        {
            BuyerId = buyerId,
            Items = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductId = 1,
                    ProductName = "Test Product 1",
                    UnitPrice = 99.99m,
                    Quantity = 2,
                    PictureUrl = "test1.jpg"
                },
                new BasketItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductId = 2,
                    ProductName = "Test Product 2",
                    UnitPrice = 149.99m,
                    Quantity = 1,
                    PictureUrl = "test2.jpg"
                }
            }
        };
    }

    private CustomerBasket CreateTestBasketWithMultipleItems(string buyerId, int itemCount)
    {
        var items = new List<BasketItem>();
        for (int i = 1; i <= itemCount; i++)
        {
            items.Add(new BasketItem
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = i,
                ProductName = $"Product {i}",
                UnitPrice = 10m + (i * 5m),
                Quantity = (i % 5) + 1,
                PictureUrl = $"product{i}.jpg"
            });
        }

        return new CustomerBasket
        {
            BuyerId = buyerId,
            Items = items
        };
    }

    private List<CustomerBasket> CreateTestBaskets(int count)
    {
        var baskets = new List<CustomerBasket>();
        for (int i = 0; i < count; i++)
        {
            baskets.Add(CreateTestBasket($"user-{i}"));
        }
        return baskets;
    }

    private bool ValidateBasket(CustomerBasket basket)
    {
        if (string.IsNullOrEmpty(basket.BuyerId))
            return false;

        if (basket.Items == null || !basket.Items.Any())
            return false;

        foreach (var item in basket.Items)
        {
            if (item.ProductId <= 0 || 
                string.IsNullOrEmpty(item.ProductName) || 
                item.UnitPrice <= 0 || 
                item.Quantity <= 0)
                return false;
        }

        return true;
    }
}

// In-memory repository for benchmarking
public class InMemoryBasketRepository : IBasketRepository
{
    private readonly Dictionary<string, CustomerBasket> _baskets = new();

    public async Task<CustomerBasket> GetBasketAsync(string customerId)
    {
        _baskets.TryGetValue(customerId, out var basket);
        return await Task.FromResult(basket ?? new CustomerBasket { BuyerId = customerId });
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        _baskets[basket.BuyerId] = basket;
        return await Task.FromResult(basket);
    }

    public async Task<bool> DeleteBasketAsync(string id)
    {
        var removed = _baskets.Remove(id);
        return await Task.FromResult(removed);
    }
}