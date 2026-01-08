using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Net.Http.Json;
using System.Text.Json;

namespace eShop.PerformanceTests.Benchmarks;

[Config(typeof(Config))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class CatalogApiBenchmarks
{
    private HttpClient _httpClient = null!;
    private const string BaseUrl = "https://localhost:5001";

    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(BaseUrl);
        
        // Warm up the API
        try
        {
            var warmupResponse = _httpClient.GetAsync("/health").Result;
            if (!warmupResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("API is not available for benchmarking");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to API: {ex.Message}");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    [Benchmark]
    public async Task<string> GetCatalogItems_DefaultPaging()
    {
        var response = await _httpClient.GetAsync("/api/catalog/items?pageSize=12&pageIndex=0");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetCatalogItems_LargePage()
    {
        var response = await _httpClient.GetAsync("/api/catalog/items?pageSize=50&pageIndex=0");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task<string> GetCatalogItem_ById(int productId)
    {
        var response = await _httpClient.GetAsync($"/api/catalog/items/{productId}");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetCatalogBrands()
    {
        var response = await _httpClient.GetAsync("/api/catalog/catalogbrands");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetCatalogTypes()
    {
        var response = await _httpClient.GetAsync("/api/catalog/catalogtypes");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetCatalogItems_ByBrand()
    {
        var response = await _httpClient.GetAsync("/api/catalog/items/type/1/brand/1?pageSize=12&pageIndex=0");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<string> GetCatalogItems_ByType()
    {
        var response = await _httpClient.GetAsync("/api/catalog/items/type/1?pageSize=12&pageIndex=0");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<T?> DeserializeCatalogResponse<T>()
    {
        var response = await _httpClient.GetAsync("/api/catalog/items?pageSize=12&pageIndex=0");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    [Benchmark]
    public async Task<bool> HealthCheck()
    {
        var response = await _httpClient.GetAsync("/health");
        return response.IsSuccessStatusCode;
    }

    [Benchmark]
    public async Task<int> ConcurrentCatalogRequests()
    {
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_httpClient.GetAsync("/api/catalog/items?pageSize=12&pageIndex=0"));
        }
        
        var responses = await Task.WhenAll(tasks);
        return responses.Count(r => r.IsSuccessStatusCode);
    }

    [Benchmark]
    [Arguments("jacket")]
    [Arguments("boot")]
    [Arguments("backpack")]
    public async Task<string> SearchCatalogItems(string searchTerm)
    {
        var response = await _httpClient.GetAsync($"/api/catalog/items?search={searchTerm}&pageSize=12&pageIndex=0");
        return await response.Content.ReadAsStringAsync();
    }

    [Benchmark]
    public async Task<TimeSpan> MeasureResponseTime()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _httpClient.GetAsync("/api/catalog/items?pageSize=12&pageIndex=0");
        stopwatch.Stop();
        
        // Ensure we read the response
        await response.Content.ReadAsStringAsync();
        
        return stopwatch.Elapsed;
    }
}