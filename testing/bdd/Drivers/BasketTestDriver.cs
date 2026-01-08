using eShop.Basket.API.Model;
using eShop.BddTests.Support;
using System.Text.Json;

namespace eShop.BddTests.Drivers;

public class BasketTestDriver
{
    private readonly HttpClient _httpClient;
    private readonly IInMemoryBasketRepository _basketRepository;
    private readonly TestConfiguration _config;

    public BasketTestDriver(
        HttpClient httpClient, 
        IInMemoryBasketRepository basketRepository, 
        TestConfiguration config)
    {
        _httpClient = httpClient;
        _basketRepository = basketRepository;
        _config = config;
    }

    public async Task<bool> CheckServiceHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task AuthenticateCustomerAsync(string customerId)
    {
        // In a real implementation, this would handle authentication
        // For testing, we just store the customer ID
        await Task.CompletedTask;
    }

    public async Task<CustomerBasket> GetBasketAsync(string customerId)
    {
        return await _basketRepository.GetBasketAsync(customerId) ?? new CustomerBasket(customerId);
    }

    public async Task ClearBasketAsync(string customerId)
    {
        await _basketRepository.DeleteBasketAsync(customerId);
    }

    public async Task<BasketItem> AddItemToBasketAsync(string customerId, int productId, string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        if (productId == 99999) // Simulate non-existent product
        {
            throw new InvalidOperationException("Product not found");
        }

        var basket = await GetBasketAsync(customerId);
        
        var existingItem = basket.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            var newItem = new BasketItem
            {
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                PictureUrl = $"https://example.com/products/{productId}.jpg"
            };
            basket.Items.Add(newItem);
        }

        await _basketRepository.UpdateBasketAsync(basket);
        
        return basket.Items.First(i => i.ProductId == productId);
    }

    public async Task UpdateItemQuantityAsync(string customerId, int productId, int newQuantity)
    {
        if (newQuantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));
        }

        var basket = await GetBasketAsync(customerId);
        var item = basket.Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (item == null)
        {
            throw new InvalidOperationException($"Product {productId} not found in basket");
        }

        item.Quantity = newQuantity;
        await _basketRepository.UpdateBasketAsync(basket);
    }

    public async Task RemoveItemFromBasketAsync(string customerId, int productId)
    {
        var basket = await GetBasketAsync(customerId);
        var item = basket.Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (item != null)
        {
            basket.Items.Remove(item);
            await _basketRepository.UpdateBasketAsync(basket);
        }
    }

    public async Task<object> PrepareCheckoutAsync(string customerId)
    {
        var basket = await GetBasketAsync(customerId);
        
        if (!basket.Items.Any())
        {
            throw new InvalidOperationException("Cannot checkout with empty basket");
        }

        // Validate all items
        foreach (var item in basket.Items)
        {
            if (item.Quantity <= 0 || item.UnitPrice <= 0)
            {
                throw new InvalidOperationException($"Invalid item: {item.ProductName}");
            }
        }

        // Simulate checkout preparation
        var checkoutData = new
        {
            CustomerId = customerId,
            Items = basket.Items.Select(i => new
            {
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                Total = i.Quantity * i.UnitPrice
            }).ToList(),
            TotalAmount = basket.Items.Sum(i => i.Quantity * i.UnitPrice),
            ItemCount = basket.Items.Sum(i => i.Quantity),
            Timestamp = DateTime.UtcNow
        };

        return checkoutData;
    }

    public async Task SeedTestDataAsync()
    {
        // Seed some test baskets if needed
        await Task.CompletedTask;
    }

    public async Task CleanupScenarioDataAsync()
    {
        // Clean up scenario-specific data
        await _basketRepository.ClearScenarioDataAsync();
    }
}