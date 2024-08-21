using System.Net.Http.Json;
using System.Text.Json;
using Asp.Versioning;
using Asp.Versioning.Http;
using eShop.Catalog.API.Model;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogApiTests : IClassFixture<CatalogApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public CatalogApiTests(CatalogApiFixture fixture)
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(1.0));

        _webApplicationFactory = fixture;
        _httpClient = _webApplicationFactory.CreateDefaultClient(handler);
    }

    [Fact]
    public async Task GetCatalogItemsRespectsPageSize()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/catalog/items?pageIndex=0&pageSize=5");

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
        var response = await _httpClient.GetAsync("/api/catalog/items/1");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var itemToUpdate = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Act - 2
        var priorAvailableStock = itemToUpdate.AvailableStock;
        itemToUpdate.AvailableStock -= 1;
        response = await _httpClient.PutAsJsonAsync("/api/catalog/items", itemToUpdate);
        response.EnsureSuccessStatusCode();

        // Act - 3
        response = await _httpClient.GetAsync("/api/catalog/items/1");
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
        var response = await _httpClient.GetAsync("/api/catalog/items/1");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var itemToUpdate = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Act - 2
        var priorAvailableStock = itemToUpdate.AvailableStock;
        itemToUpdate.AvailableStock -= 1;
        itemToUpdate.Price = 1.99m;
        response = await _httpClient.PutAsJsonAsync("/api/catalog/items", itemToUpdate);
        response.EnsureSuccessStatusCode();

        // Act - 3
        response = await _httpClient.GetAsync("/api/catalog/items/1");
        response.EnsureSuccessStatusCode();
        body = await response.Content.ReadAsStringAsync();
        var updatedItem = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Assert - 1
        Assert.Equal(itemToUpdate.Id, updatedItem.Id);
        Assert.Equal(1.99m, updatedItem.Price);
        Assert.NotEqual(priorAvailableStock, updatedItem.AvailableStock);
    }

    [Fact]
    public async Task GetCatalogItemsbyIds()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/catalog/items/by?ids=1&ids=2&ids=3");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<CatalogItem>>(body, _jsonSerializerOptions);

        // Assert 3 items      
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetCatalogItemWithId()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/catalog/items/2");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Assert       
        Assert.Equal(2, result.Id);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetCatalogItemWithExactName()
    {
        // Act
        var response = await _httpClient.GetAsync("api/catalog/items/by/Wanderer%20Black%20Hiking%20Boots?PageSize=5&PageIndex=0");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Count);
        Assert.Equal(0, result.PageIndex);
        Assert.Equal(5, result.PageSize);
        Assert.Equal("Wanderer Black Hiking Boots", result.Data.ToList().FirstOrDefault().Name);
    }

    // searching partial name Alpine
    [Fact]
    public async Task GetCatalogItemWithPartialName()
    {
       // Act
       var response = await _httpClient.GetAsync("api/catalog/items/by/Alpine?PageSize=5&PageIndex=0");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal(4, result.Count);
        Assert.Equal(0, result.PageIndex);
        Assert.Equal(5, result.PageSize);
        Assert.Contains("Alpine", result.Data.ToList().FirstOrDefault().Name);
    }


    [Fact]
    public async Task GetCatalogItemPicWithId()
    {
        // Act
        var response = await _httpClient.GetAsync("api/catalog/items/1/pic");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var result = response.Content.Headers.ContentType.MediaType;

        // Assert       
        Assert.Equal("image/webp", result);
    }


    [Fact]
    public async Task GetCatalogItemWithsemanticrelevance()
    {
        // Act
        var response = await _httpClient.GetAsync("api/catalog/items/withsemanticrelevance/Wanderer?PageSize=5&PageIndex=0");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        // Assert       
        Assert.Equal(1, result.Count);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.PageIndex);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetCatalogItemWithTypeIdBrandId()
    {
        // Act
        var response = await _httpClient.GetAsync("api/catalog/items/type/3/brand/3?PageSize=5&PageIndex=0");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        // Assert    
        Assert.NotNull(result.Data);
        Assert.Equal(4, result.Count);
        Assert.Equal(0, result.PageIndex);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(3, result.Data.ToList().FirstOrDefault().CatalogTypeId);
        Assert.Equal(3, result.Data.ToList().FirstOrDefault().CatalogBrandId);
    }

    [Fact]
    public async Task GetAllCatalogTypeItemWithBrandId()
    {
        // Act
        var response = await _httpClient.GetAsync("api/catalog/items/type/all/brand/3?PageSize=5&PageIndex=0");

        // Arrange
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedItems<CatalogItem>>(body, _jsonSerializerOptions);

        // Assert              
        Assert.NotNull(result.Data);
        Assert.Equal(11, result.Count);
        Assert.Equal(0, result.PageIndex);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(3, result.Data.ToList().FirstOrDefault().CatalogBrandId);
    }

    [Fact]
    public async Task GetAllCatalogTypes()
    {
        // Act
        var response = await _httpClient.GetAsync("api/catalog/catalogtypes");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<CatalogType>>(body, _jsonSerializerOptions);

        // Assert       
        Assert.Equal(8, result.Count);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllCatalogBrands()
    {
        // Act
        var response = await _httpClient.GetAsync("api/catalog/catalogbrands");

        // Arrange   
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<CatalogBrand>>(body, _jsonSerializerOptions);

        // Assert       
        Assert.Equal(13, result.Count);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddCatalogItem()
    {
        // Act - 1
        var bodyContent = new CatalogItem {
            Id = 10015,
            Name = "TestCatalog1",
            Description = "Test catalog description 1",
            Price = 11000.08m,
            PictureFileName = null,
            CatalogTypeId = 8,
            CatalogType = null,
            CatalogBrandId = 13,
            CatalogBrand = null,
            AvailableStock = 100,
            RestockThreshold = 10,
            MaxStockThreshold = 200,
            OnReorder = false
        };
        var response = await _httpClient.PostAsJsonAsync("/api/catalog/items", bodyContent);
        response.EnsureSuccessStatusCode();

        // Act - 2
        response = await _httpClient.GetAsync("/api/catalog/items/10015");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var addedItem = JsonSerializer.Deserialize<CatalogItem>(body, _jsonSerializerOptions);

        // Assert - 1
        Assert.Equal(bodyContent.Id, addedItem.Id);

    }

    [Fact]
    public async Task DeleteCatalogItem()
    {
        //Act - 1
        var response = await _httpClient.DeleteAsync("/api/catalog/items/5");
        response.EnsureSuccessStatusCode();

        // Act - 2
        var response1 = await _httpClient.GetAsync("/api/catalog/items/5");
        var responseStatus = response1.StatusCode;

        // Assert - 1
        Assert.Equal("NoContent", response.StatusCode.ToString());
        Assert.Equal("NotFound", responseStatus.ToString());
    }
}
