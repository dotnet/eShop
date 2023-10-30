using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using eShop.Catalog.API.Model;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogApiTests : IClassFixture<CatalogApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public CatalogApiTests(CatalogApiFixture fixture)
    {
        _webApplicationFactory = fixture;
        _httpClient = _webApplicationFactory.CreateClient();
    }

    [Fact]
    public async Task GetCatalogItemsRespectsPageSize()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/v1/catalog/items?pageIndex=0&pageSize=5");

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        // Assert 12 total items with 5 retrieved from index 0
        Assert.Equal(101, result.Count);
        Assert.Equal(0, result.PageIndex);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task UpdateCatalogItemWorksWithoutPriceUpdate()
    {
        // Act - 1
        var response = await _httpClient.GetAsync("/api/v1/catalog/items/1");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var itemToUpdate = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Act - 2
        var priorAvailableStock = itemToUpdate.AvailableStock;
        itemToUpdate.AvailableStock -= 1;
        response = await _httpClient.PutAsJsonAsync("/api/v1/catalog/items", itemToUpdate);
        response.EnsureSuccessStatusCode();

        // Act - 3
        response = await _httpClient.GetAsync("/api/v1/catalog/items/1");
        response.EnsureSuccessStatusCode();
        body = await response.Content.ReadAsStringAsync();
        var updatedItem = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Assert - 1
        Assert.Equal(itemToUpdate.Id, updatedItem.Id);
        Assert.NotEqual(priorAvailableStock, updatedItem.AvailableStock);
    }

    [Fact]
    public async Task UpdateCatalogItemWorksWithPriceUpdate()
    {
        // Act - 1
        var response = await _httpClient.GetAsync("/api/v1/catalog/items/1");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var itemToUpdate = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Act - 2
        var priorAvailableStock = itemToUpdate.AvailableStock;
        itemToUpdate.AvailableStock -= 1;
        itemToUpdate.Price = 1.99m;
        response = await _httpClient.PutAsJsonAsync("/api/v1/catalog/items", itemToUpdate);
        response.EnsureSuccessStatusCode();

        // Act - 3
        response = await _httpClient.GetAsync("/api/v1/catalog/items/1");
        response.EnsureSuccessStatusCode();
        body = await response.Content.ReadAsStringAsync();
        var updatedItem = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Assert - 1
        Assert.Equal(itemToUpdate.Id, updatedItem.Id);
        Assert.Equal(1.99m, updatedItem.Price);
        Assert.NotEqual(priorAvailableStock, updatedItem.AvailableStock);
    }
}
