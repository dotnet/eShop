using eShop.Ordering.API.Application.Commands;
using eShop.Ordering.API.Application.Queries;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace eShop.IntegrationTests.Api;

[TestClass]
public class OrderingApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Configure test-specific services
                    services.AddSingleton<TestDataSeeder>();
                });
            });

        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task CreateOrder_ValidOrderData_ReturnsCreatedOrder()
    {
        // Arrange
        var command = new CreateOrderCommand(
            basketItems: new List<BasketItem>
            {
                new("item1", 1, "Test Product", 99.99m, 0, 2, "test.jpg")
            },
            userId: "test-user-123",
            userName: "testuser@example.com",
            city: "Seattle",
            street: "123 Test St",
            state: "WA",
            country: "USA",
            zipCode: "98101",
            cardNumber: "4111111111111111",
            cardHolderName: "Test User",
            cardExpiration: DateTime.Now.AddYears(2),
            cardSecurityNumber: "123",
            cardTypeId: 1
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", command);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var orderId = JsonSerializer.Deserialize<int>(content);
        orderId.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public async Task GetOrder_ExistingOrderId_ReturnsOrderDetails()
    {
        // Arrange - First create an order
        var createCommand = new CreateOrderCommand(
            basketItems: new List<BasketItem>
            {
                new("item1", 1, "Test Product", 99.99m, 0, 1, "test.jpg")
            },
            userId: "test-user-456",
            userName: "testuser2@example.com",
            city: "Portland",
            street: "456 Test Ave",
            state: "OR",
            country: "USA",
            zipCode: "97201",
            cardNumber: "4000000000000002",
            cardHolderName: "Test User 2",
            cardExpiration: DateTime.Now.AddYears(3),
            cardSecurityNumber: "456",
            cardTypeId: 1
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<int>();

        // Act
        var response = await _client.GetAsync($"/api/v1/orders/{orderId}");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var orderDetails = await response.Content.ReadFromJsonAsync<OrderDetailsDto>();
        orderDetails.ShouldNotBeNull();
        orderDetails.OrderId.ShouldBe(orderId);
        orderDetails.BuyerId.ShouldBe("test-user-456");
        orderDetails.OrderItems.ShouldNotBeEmpty();
    }

    [TestMethod]
    public async Task GetOrder_NonExistentOrderId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/orders/99999");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task CancelOrder_ExistingOrder_CancelsSuccessfully()
    {
        // Arrange - Create an order first
        var createCommand = new CreateOrderCommand(
            basketItems: new List<BasketItem>
            {
                new("item1", 1, "Test Product", 99.99m, 0, 1, "test.jpg")
            },
            userId: "test-user-789",
            userName: "testuser3@example.com",
            city: "Denver",
            street: "789 Test Blvd",
            state: "CO",
            country: "USA",
            zipCode: "80201",
            cardNumber: "5555555555554444",
            cardHolderName: "Test User 3",
            cardExpiration: DateTime.Now.AddYears(1),
            cardSecurityNumber: "789",
            cardTypeId: 2
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<int>();

        // Act
        var cancelCommand = new CancelOrderCommand(orderId);
        var response = await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}/cancel", cancelCommand);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();

        // Verify order is cancelled
        var getResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        var orderDetails = await getResponse.Content.ReadFromJsonAsync<OrderDetailsDto>();
        orderDetails.Status.ShouldBe("Cancelled");
    }

    [TestMethod]
    public async Task GetOrdersByUser_ExistingUser_ReturnsUserOrders()
    {
        // Arrange - Create multiple orders for the same user
        var userId = "test-user-multi";
        var orders = new[]
        {
            new CreateOrderCommand(
                basketItems: new List<BasketItem>
                {
                    new("item1", 1, "Product 1", 50.00m, 0, 1, "test1.jpg")
                },
                userId: userId,
                userName: "multiuser@example.com",
                city: "Austin",
                street: "100 Multi St",
                state: "TX",
                country: "USA",
                zipCode: "73301",
                cardNumber: "4111111111111111",
                cardHolderName: "Multi User",
                cardExpiration: DateTime.Now.AddYears(2),
                cardSecurityNumber: "100",
                cardTypeId: 1
            ),
            new CreateOrderCommand(
                basketItems: new List<BasketItem>
                {
                    new("item2", 2, "Product 2", 75.00m, 0, 2, "test2.jpg")
                },
                userId: userId,
                userName: "multiuser@example.com",
                city: "Austin",
                street: "100 Multi St",
                state: "TX",
                country: "USA",
                zipCode: "73301",
                cardNumber: "4111111111111111",
                cardHolderName: "Multi User",
                cardExpiration: DateTime.Now.AddYears(2),
                cardSecurityNumber: "100",
                cardTypeId: 1
            )
        };

        foreach (var order in orders)
        {
            await _client.PostAsJsonAsync("/api/v1/orders", order);
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/orders/user/{userId}");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var userOrders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
        userOrders.ShouldNotBeNull();
        userOrders.Count.ShouldBeGreaterThanOrEqualTo(2);
        userOrders.All(o => o.BuyerId == userId).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CreateOrder_InvalidBasketItems_ReturnsBadRequest()
    {
        // Arrange - Create order with invalid basket items (empty list)
        var command = new CreateOrderCommand(
            basketItems: new List<BasketItem>(), // Empty basket
            userId: "test-user-invalid",
            userName: "invalid@example.com",
            city: "Seattle",
            street: "123 Invalid St",
            state: "WA",
            country: "USA",
            zipCode: "98101",
            cardNumber: "4111111111111111",
            cardHolderName: "Invalid User",
            cardExpiration: DateTime.Now.AddYears(2),
            cardSecurityNumber: "123",
            cardTypeId: 1
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", command);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateOrder_InvalidPaymentInfo_ReturnsBadRequest()
    {
        // Arrange - Create order with invalid payment information
        var command = new CreateOrderCommand(
            basketItems: new List<BasketItem>
            {
                new("item1", 1, "Test Product", 99.99m, 0, 1, "test.jpg")
            },
            userId: "test-user-payment",
            userName: "payment@example.com",
            city: "Seattle",
            street: "123 Payment St",
            state: "WA",
            country: "USA",
            zipCode: "98101",
            cardNumber: "1234", // Invalid card number
            cardHolderName: "Payment User",
            cardExpiration: DateTime.Now.AddYears(2),
            cardSecurityNumber: "123",
            cardTypeId: 1
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", command);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task GetOrderHealth_Always_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        var healthStatus = await response.Content.ReadAsStringAsync();
        healthStatus.ShouldContain("Healthy");
    }

    [TestMethod]
    public async Task OrderWorkflow_CreateToCancellation_CompletesSuccessfully()
    {
        // Arrange
        var userId = "workflow-user";
        var createCommand = new CreateOrderCommand(
            basketItems: new List<BasketItem>
            {
                new("workflow1", 1, "Workflow Product", 199.99m, 0, 1, "workflow.jpg")
            },
            userId: userId,
            userName: "workflow@example.com",
            city: "Phoenix",
            street: "123 Workflow Ave",
            state: "AZ",
            country: "USA",
            zipCode: "85001",
            cardNumber: "5200000000000007",
            cardHolderName: "Workflow User",
            cardExpiration: DateTime.Now.AddYears(2),
            cardSecurityNumber: "321",
            cardTypeId: 2
        );

        // Act & Assert - Create Order
        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createCommand);
        createResponse.IsSuccessStatusCode.ShouldBeTrue();
        var orderId = await createResponse.Content.ReadFromJsonAsync<int>();

        // Act & Assert - Get Order
        var getResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.IsSuccessStatusCode.ShouldBeTrue();
        var orderDetails = await getResponse.Content.ReadFromJsonAsync<OrderDetailsDto>();
        orderDetails.Status.ShouldBe("Submitted");

        // Act & Assert - Cancel Order
        var cancelCommand = new CancelOrderCommand(orderId);
        var cancelResponse = await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}/cancel", cancelCommand);
        cancelResponse.IsSuccessStatusCode.ShouldBeTrue();

        // Act & Assert - Verify Cancellation
        var finalGetResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        var finalOrderDetails = await finalGetResponse.Content.ReadFromJsonAsync<OrderDetailsDto>();
        finalOrderDetails.Status.ShouldBe("Cancelled");
    }
}

// DTOs for integration tests
public class OrderDetailsDto
{
    public int OrderId { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
    public AddressDto Address { get; set; } = new();
}

public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
    public string PictureUrl { get; set; } = string.Empty;
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class TestDataSeeder
{
    // Test data seeding logic for integration tests
    public async Task SeedTestDataAsync()
    {
        // Implement test data seeding if needed
        await Task.CompletedTask;
    }
}