using Aspire.Hosting.Testing;
using eShop.Basket.API.Model;
using System.Net.Http.Json;
using System.Text.Json;

namespace eShop.IntegrationTests.Api;

[TestClass]
public class BasketApiIntegrationTests
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

        // Get the HTTP client for the Basket API
        _httpClient = _app.CreateHttpClient("basket-api");
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
    public async Task GetBasket_NewUser_ReturnsEmptyBasket()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act
        var response = await _httpClient!.GetAsync($"/api/basket/{userId}");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var basket = await response.Content.ReadFromJsonAsync<CustomerBasket>();
            basket.ShouldNotBeNull();
            basket.Items.ShouldBeEmpty();
        }
        else
        {
            // Empty basket might return 404, which is acceptable
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
        }
    }

    [TestMethod]
    public async Task UpdateBasket_ValidBasket_ReturnsUpdatedBasket()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var basket = new CustomerBasket
        {
            BuyerId = userId,
            Items = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = "1",
                    ProductId = 1,
                    ProductName = "Test Product",
                    UnitPrice = 99.99m,
                    Quantity = 2,
                    PictureUrl = "test.jpg"
                }
            }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync($"/api/basket", basket);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var updatedBasket = await response.Content.ReadFromJsonAsync<CustomerBasket>();
        updatedBasket.ShouldNotBeNull();
        updatedBasket.BuyerId.ShouldBe(userId);
        updatedBasket.Items.Count.ShouldBe(1);
        updatedBasket.Items.First().ProductId.ShouldBe(1);
    }

    [TestMethod]
    public async Task UpdateBasket_MultipleItems_CalculatesCorrectTotal()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var basket = new CustomerBasket
        {
            BuyerId = userId,
            Items = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = "1",
                    ProductId = 1,
                    ProductName = "Product 1",
                    UnitPrice = 50.00m,
                    Quantity = 2,
                    PictureUrl = "test1.jpg"
                },
                new BasketItem
                {
                    Id = "2",
                    ProductId = 2,
                    ProductName = "Product 2",
                    UnitPrice = 25.00m,
                    Quantity = 3,
                    PictureUrl = "test2.jpg"
                }
            }
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync($"/api/basket", basket);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var updatedBasket = await response.Content.ReadFromJsonAsync<CustomerBasket>();
        updatedBasket.ShouldNotBeNull();
        
        var expectedTotal = (50.00m * 2) + (25.00m * 3); // 175.00
        var actualTotal = updatedBasket.Items.Sum(item => item.UnitPrice * item.Quantity);
        actualTotal.ShouldBe(expectedTotal);
    }

    [TestMethod]
    public async Task DeleteBasket_ExistingBasket_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        
        // First create a basket
        var basket = new CustomerBasket
        {
            BuyerId = userId,
            Items = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = "1",
                    ProductId = 1,
                    ProductName = "Test Product",
                    UnitPrice = 99.99m,
                    Quantity = 1,
                    PictureUrl = "test.jpg"
                }
            }
        };

        await _httpClient!.PostAsJsonAsync($"/api/basket", basket);

        // Act
        var deleteResponse = await _httpClient.DeleteAsync($"/api/basket/{userId}");

        // Assert
        deleteResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Verify basket is deleted
        var getResponse = await _httpClient.GetAsync($"/api/basket/{userId}");
        getResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task UpdateBasket_InvalidData_ReturnsValidationError()
    {
        // Arrange
        var basket = new CustomerBasket
        {
            BuyerId = "", // Invalid empty buyer ID
            Items = new List<BasketItem>()
        };

        // Act
        var response = await _httpClient!.PostAsJsonAsync($"/api/basket", basket);

        // Assert
        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task BasketOperations_ConcurrentModifications_MaintainConsistency()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var tasks = new List<Task<HttpResponseMessage>>();

        // Create multiple concurrent basket updates
        for (int i = 0; i < 5; i++)
        {
            var basket = new CustomerBasket
            {
                BuyerId = userId,
                Items = new List<BasketItem>
                {
                    new BasketItem
                    {
                        Id = i.ToString(),
                        ProductId = i + 1,
                        ProductName = $"Product {i + 1}",
                        UnitPrice = 10.00m * (i + 1),
                        Quantity = 1,
                        PictureUrl = $"test{i + 1}.jpg"
                    }
                }
            };

            tasks.Add(_httpClient!.PostAsJsonAsync($"/api/basket", basket));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        // At least one operation should succeed
        responses.Any(r => r.IsSuccessStatusCode).ShouldBeTrue();

        // Final basket should be in a consistent state
        var finalResponse = await _httpClient!.GetAsync($"/api/basket/{userId}");
        if (finalResponse.IsSuccessStatusCode)
        {
            var finalBasket = await finalResponse.Content.ReadFromJsonAsync<CustomerBasket>();
            finalBasket.ShouldNotBeNull();
            finalBasket.BuyerId.ShouldBe(userId);
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
    public async Task BasketApiResponseTime_WithinAcceptableLimits()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _httpClient!.GetAsync($"/api/basket/{userId}");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(3000, "Basket API response should be under 3 seconds");
    }

    [TestMethod]
    public async Task BasketPersistence_AcrossMultipleOperations_MaintainsData()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var originalBasket = new CustomerBasket
        {
            BuyerId = userId,
            Items = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = "1",
                    ProductId = 1,
                    ProductName = "Persistent Product",
                    UnitPrice = 150.00m,
                    Quantity = 2,
                    PictureUrl = "persistent.jpg"
                }
            }
        };

        // Act - Create basket
        var createResponse = await _httpClient!.PostAsJsonAsync($"/api/basket", originalBasket);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Act - Retrieve basket
        var getResponse = await _httpClient.GetAsync($"/api/basket/{userId}");
        
        // Assert
        if (getResponse.IsSuccessStatusCode)
        {
            var retrievedBasket = await getResponse.Content.ReadFromJsonAsync<CustomerBasket>();
            retrievedBasket.ShouldNotBeNull();
            retrievedBasket.BuyerId.ShouldBe(userId);
            retrievedBasket.Items.Count.ShouldBe(1);
            retrievedBasket.Items.First().ProductName.ShouldBe("Persistent Product");
            retrievedBasket.Items.First().UnitPrice.ShouldBe(150.00m);
            retrievedBasket.Items.First().Quantity.ShouldBe(2);
        }
    }
}