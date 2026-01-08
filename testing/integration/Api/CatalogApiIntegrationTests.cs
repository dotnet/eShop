using Aspire.Hosting.Testing;
using eShop.Catalog.API.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace eShop.IntegrationTests.Api;

[TestClass]
public class CatalogApiIntegrationTests
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;

    [TestInitialize]
    public async Task Setup()
    {
        // Create the distributed application
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.eShop_AppHost>()
            .ConfigureAwait(false);

        _app = await appHost.BuildAsync().ConfigureAwait(false);
        await _app.StartAsync().ConfigureAwait(false);

        // Get the HTTP client for the Catalog API
        _httpClient = _app.CreateHttpClient("catalog-api");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_app != null)
        {
            await _app.DisposeAsync().ConfigureAwait(false);
        }
        _httpClient?.Dispose();
    }

    [TestMethod]
    public async Task GetCatalogItems_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/items");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [TestMethod]
    public async Task GetCatalogItems_WithPagination_ReturnsCorrectFormat()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/items?pageSize=5&pageIndex=0");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotBeNullOrEmpty();

        // Verify JSON structure
        var jsonDocument = JsonDocument.Parse(content);
        jsonDocument.RootElement.TryGetProperty("data", out var dataProperty).ShouldBeTrue();
        jsonDocument.RootElement.TryGetProperty("count", out var countProperty).ShouldBeTrue();
        jsonDocument.RootElement.TryGetProperty("pageIndex", out var pageIndexProperty).ShouldBeTrue();
        jsonDocument.RootElement.TryGetProperty("pageSize", out var pageSizeProperty).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetCatalogItemById_ValidId_ReturnsItem()
    {
        // Arrange - First get available items to find a valid ID
        var itemsResponse = await _httpClient!.GetAsync("/api/catalog/items?pageSize=1");
        itemsResponse.IsSuccessStatusCode.ShouldBeTrue();

        var itemsContent = await itemsResponse.Content.ReadAsStringAsync();
        var itemsJson = JsonDocument.Parse(itemsContent);
        
        if (itemsJson.RootElement.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
        {
            var firstItem = dataArray[0];
            var itemId = firstItem.GetProperty("id").GetInt32();

            // Act
            var response = await _httpClient.GetAsync($"/api/catalog/items/{itemId}");

            // Assert
            response.IsSuccessStatusCode.ShouldBeTrue();
            
            var content = await response.Content.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<CatalogItem>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            item.ShouldNotBeNull();
            item.Id.ShouldBe(itemId);
        }
    }

    [TestMethod]
    public async Task GetCatalogItemById_InvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/items/99999");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetCatalogItemsByBrand_ValidBrandId_ReturnsFilteredItems()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/items/type/1/brand/1");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        
        if (jsonDocument.RootElement.TryGetProperty("data", out var dataArray))
        {
            foreach (var item in dataArray.EnumerateArray())
            {
                item.GetProperty("catalogBrandId").GetInt32().ShouldBe(1);
            }
        }
    }

    [TestMethod]
    public async Task GetCatalogItemsByType_ValidTypeId_ReturnsFilteredItems()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/items/type/1");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        
        if (jsonDocument.RootElement.TryGetProperty("data", out var dataArray))
        {
            foreach (var item in dataArray.EnumerateArray())
            {
                item.GetProperty("catalogTypeId").GetInt32().ShouldBe(1);
            }
        }
    }

    [TestMethod]
    public async Task GetCatalogBrands_ReturnsAllBrands()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/catalogbrands");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var brands = await response.Content.ReadFromJsonAsync<CatalogBrand[]>();
        brands.ShouldNotBeNull();
        brands.Length.ShouldBeGreaterThan(0);
        
        foreach (var brand in brands)
        {
            brand.Id.ShouldBeGreaterThan(0);
            brand.Brand.ShouldNotBeNullOrEmpty();
        }
    }

    [TestMethod]
    public async Task GetCatalogTypes_ReturnsAllTypes()
    {
        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/catalogtypes");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var types = await response.Content.ReadFromJsonAsync<CatalogType[]>();
        types.ShouldNotBeNull();
        types.Length.ShouldBeGreaterThan(0);
        
        foreach (var type in types)
        {
            type.Id.ShouldBeGreaterThan(0);
            type.Type.ShouldNotBeNullOrEmpty();
        }
    }

    [TestMethod]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _httpClient!.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Healthy");
    }

    [TestMethod]
    public async Task ConcurrentRequests_SameEndpoint_HandledCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        const int concurrentRequests = 10;

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_httpClient!.GetAsync("/api/catalog/items?pageSize=5"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.IsSuccessStatusCode.ShouldBeTrue();
        }

        // Verify all responses have consistent structure
        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
        foreach (var content in contents)
        {
            var jsonDocument = JsonDocument.Parse(content);
            jsonDocument.RootElement.TryGetProperty("data", out _).ShouldBeTrue();
        }
    }

    [TestMethod]
    public async Task ApiResponseTime_WithinAcceptableLimits()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _httpClient!.GetAsync("/api/catalog/items");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000, "API response should be under 5 seconds");
    }
}