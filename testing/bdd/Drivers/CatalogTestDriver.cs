using eShop.Catalog.API.Model;
using eShop.BddTests.Support;
using System.Text.Json;

namespace eShop.BddTests.Drivers;

public class CatalogTestDriver
{
    private readonly HttpClient _httpClient;
    private readonly InMemoryCatalogRepository _repository;
    private readonly TestConfiguration _config;

    public CatalogTestDriver(HttpClient httpClient, InMemoryCatalogRepository repository, TestConfiguration config)
    {
        _httpClient = httpClient;
        _repository = repository;
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

    public async Task SeedTestDataAsync()
    {
        await _repository.SeedTestDataAsync();
    }

    public async Task<IEnumerable<CatalogItem>> GetSeededProductsAsync()
    {
        return await _repository.GetAllProductsAsync();
    }

    public async Task<CatalogItem?> GetProductByIdAsync(int productId)
    {
        return await _repository.GetProductByIdAsync(productId);
    }

    public async Task<IEnumerable<CatalogItem>> GetCatalogItemsAsync(int pageIndex, int pageSize)
    {
        var allProducts = await _repository.GetAllProductsAsync();
        return allProducts.Skip(pageIndex * pageSize).Take(pageSize);
    }

    public async Task<IEnumerable<CatalogItem>> SearchProductsByNameAsync(string searchTerm)
    {
        var allProducts = await _repository.GetAllProductsAsync();
        return allProducts.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<CatalogItem>> GetProductsByBrandAsync(string brandName)
    {
        // In a real implementation, this would filter by brand
        var allProducts = await _repository.GetAllProductsAsync();
        return allProducts.Where(p => p.CatalogBrandId == 1); // Simulate brand filtering
    }

    public async Task<IEnumerable<CatalogItem>> GetProductsByCategoryAsync(string categoryName)
    {
        // In a real implementation, this would filter by category
        var allProducts = await _repository.GetAllProductsAsync();
        return allProducts.Where(p => p.CatalogTypeId == 1); // Simulate category filtering
    }

    public async Task CleanupScenarioDataAsync()
    {
        await _repository.ClearScenarioDataAsync();
    }
}