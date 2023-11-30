using System.Net;
using System.Text;
using System.Text.Json;
using AutoFixture;
using eShop.Ordering.API.Application.Commands;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Ordering.FunctionalTests;

public sealed class OrderingApiTests : IClassFixture<OrderingApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;
    private readonly HttpClient _httpClient;

    public OrderingApiTests(OrderingApiFixture fixture)
    {
        _webApplicationFactory = fixture;
        _httpClient = _webApplicationFactory.CreateClient();
    }


    [Fact]
    public async Task GetAllStoredOrdersWorks()
    {
        var response = await _httpClient.GetAsync("api/v1/orders");

        var s = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CancelWithEmptyGuidFails()
    {
        var content = new StringContent(BuildOrder(), UTF8Encoding.UTF8, "application/json")
        {
            Headers = { { "x-requestid", Guid.Empty.ToString() } }
        };
        var response = await _httpClient.PutAsync("/api/v1/orders/cancel", content);

        var s = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelNonExistentOrderFails()
    {
        var content = new StringContent(BuildOrder(), UTF8Encoding.UTF8, "application/json")
        {
            Headers = { { "x-requestid", Guid.NewGuid().ToString() } }
        };
        var response = await _httpClient.PutAsync("api/v1/orders/cancel", content);

        var s = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task ShipWithEmptyGuidFails()
    {
        var content = new StringContent(BuildOrder(), UTF8Encoding.UTF8, "application/json")
        {
            Headers = { { "x-requestid", Guid.Empty.ToString() } }
        };
        var response = await _httpClient.PutAsync("api/v1/orders/ship", content);

        var s = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShipNonExistentOrderFails()
    {
        var content = new StringContent(BuildOrder(), UTF8Encoding.UTF8, "application/json")
        {
            Headers = { { "x-requestid", Guid.NewGuid().ToString() } }
        };
        var response = await _httpClient.PutAsync("api/v1/orders/ship", content);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrderDraftSucceeds()
    {
        Fixture fixture = new Fixture();
        var payload = fixture.Create<CreateOrderDraftCommand>();
        var content = new StringContent(JsonSerializer.Serialize(payload), UTF8Encoding.UTF8, "application/json")
        {
            Headers = { { "x-requestid", Guid.NewGuid().ToString() } }
        };
        var response = await _httpClient.PostAsync("api/v1/orders/draft", content);

        var s = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<OrderDraftDTO>(s, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(payload.Items.Count(), responseData.OrderItems.Count());
        Assert.Contains(payload.Items.First().ProductId, responseData.OrderItems.Select(i => i.ProductId));
        Assert.Contains(payload.Items.Skip(1).First().ProductId, responseData.OrderItems.Select(i => i.ProductId));
        Assert.Contains(payload.Items.Skip(2).First().ProductId, responseData.OrderItems.Select(i => i.ProductId));
    }

    string BuildOrder()
    {
        var order = new
        {
            OrderNumber = "-1"
        };
        return JsonSerializer.Serialize(order);
    }
}
