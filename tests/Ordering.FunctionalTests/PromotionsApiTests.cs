using System.Net;
using System.Net.Http.Json;
using Asp.Versioning;
using Asp.Versioning.Http;
using eShop.Ordering.API.Apis;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Ordering.FunctionalTests;

public sealed class PromotionsApiTests : IClassFixture<OrderingApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly HttpClient _httpClient;

    public PromotionsApiTests(OrderingApiFixture fixture)
    {
        var handler = new ApiVersionHandler(new QueryStringApiVersionWriter(), new ApiVersion(1.0));

        _webApplicationFactory = fixture;
        _httpClient = _webApplicationFactory.CreateDefaultClient(handler);
    }

    [Fact]
    public async Task GetPromotionsAsync_ReturnsOk()
    {
        // Act
        var response = await _httpClient.GetAsync("api/promotions", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreatePromotionAsync_ReturnsCreatedAt()
    {
        // Arrange
        var promotion = new PromotionDTO(
            0,
            "Test Promotion",
            "PercentageDiscount",
            10,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            null,
            null,
            null,
            true,
            1);

        // Act
        var response = await _httpClient.PostAsJsonAsync("api/promotions", promotion, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldReturnNoContent_WhenPromotionExists()
    {
        // Arrange
        var createResponse = await _httpClient.PostAsJsonAsync("api/promotions", new PromotionDTO(
            0, "Original Name", "PercentageDiscount", 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, null, null, true, 1), TestContext.Current.CancellationToken);
        
        var location = createResponse.Headers.Location.ToString();
        var createdPromotion = await _httpClient.GetFromJsonAsync<PromotionDTO>(location, TestContext.Current.CancellationToken);
        var updatedPromotion = createdPromotion with { Name = "Updated Name", DiscountValue = 20 };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"api/promotions/{createdPromotion.Id}", updatedPromotion, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldReturnNotFound_WhenPromotionDoesNotExist()
    {
        // Arrange
        var updatedPromotion = new PromotionDTO(9999, "Non-existent", "PercentageDiscount", 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, null, null, true, 1);

        // Act
        var response = await _httpClient.PutAsJsonAsync("api/promotions/9999", updatedPromotion, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldReturnBadRequest_WhenDataIsInvalid()
    {
        // Arrange
        var createResponse = await _httpClient.PostAsJsonAsync("api/promotions", new PromotionDTO(
            0, "Valid Promo", "PercentageDiscount", 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, null, null, true, 1), TestContext.Current.CancellationToken);
        
        var location = createResponse.Headers.Location.ToString();
        var createdPromotion = await _httpClient.GetFromJsonAsync<PromotionDTO>(location, TestContext.Current.CancellationToken);
        var updatedPromotion = createdPromotion with { DiscountValue = -10 }; // Invalid negative discount

        // Act
        var response = await _httpClient.PutAsJsonAsync($"api/promotions/{createdPromotion.Id}", updatedPromotion, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldUpdateCategories()
    {
        // Arrange
        var createResponse = await _httpClient.PostAsJsonAsync("api/promotions", new PromotionDTO(
            0, "Category Test", "CategoryDiscount", 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, null, null, true, 1), TestContext.Current.CancellationToken);
        
        var location = createResponse.Headers.Location.ToString();
        var createdPromotion = await _httpClient.GetFromJsonAsync<PromotionDTO>(location, TestContext.Current.CancellationToken);
        var updatedPromotion = createdPromotion with 
        { 
            ApplicableCategories = new List<string> { "Electronics", "Books" },
            ExcludedCategories = new List<string> { "Refurbished" }
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"api/promotions/{createdPromotion.Id}", updatedPromotion, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldDeactivatePromotion_WhenIsActiveSetToFalse()
    {
        // Arrange: Create an active promotion
        var createResponse = await _httpClient.PostAsJsonAsync("api/promotions", new PromotionDTO(
            0, "Deactivate Test", "PercentageDiscount", 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, null, null, true, 1), TestContext.Current.CancellationToken);
        
        var location = createResponse.Headers.Location.ToString();
        var createdPromotion = await _httpClient.GetFromJsonAsync<PromotionDTO>(location, TestContext.Current.CancellationToken);
        Assert.True(createdPromotion.IsActive);
        
        var updatedPromotion = createdPromotion with { IsActive = false };

        // Act: Update the promotion with IsActive = false
        var response = await _httpClient.PutAsJsonAsync($"api/promotions/{createdPromotion.Id}", updatedPromotion, TestContext.Current.CancellationToken);

        // Assert: Promotion is now inactive (retrieve and verify)
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        var finalPromotion = await _httpClient.GetFromJsonAsync<PromotionDTO>(location, TestContext.Current.CancellationToken);
        Assert.False(finalPromotion.IsActive);
    }

    [Fact]
    public async Task UpdatePromotionAsync_ShouldActivatePromotion_WhenIsActiveSetToTrue()
    {
        // Arrange: Create a promotion and deactivate it
        var createResponse = await _httpClient.PostAsJsonAsync("api/promotions", new PromotionDTO(
            0, "Activate Test", "PercentageDiscount", 10, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, null, null, false, 1), TestContext.Current.CancellationToken);
        
        var location = createResponse.Headers.Location.ToString();
        var createdPromotion = await _httpClient.GetFromJsonAsync<PromotionDTO>(location, TestContext.Current.CancellationToken);
        Assert.False(createdPromotion.IsActive);
        
        var updatedPromotion = createdPromotion with { IsActive = true };

        // Act: Update the promotion with IsActive = true
        var response = await _httpClient.PutAsJsonAsync($"api/promotions/{createdPromotion.Id}", updatedPromotion, TestContext.Current.CancellationToken);

        // Assert: Promotion is now active (retrieve and verify)
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        var finalPromotion = await _httpClient.GetFromJsonAsync<PromotionDTO>(location, TestContext.Current.CancellationToken);
        Assert.True(finalPromotion.IsActive);
    }
}
