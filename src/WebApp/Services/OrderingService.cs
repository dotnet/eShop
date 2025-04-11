namespace Inked.WebApp.Services;

public class OrderingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderingService> _logger;
    private readonly string remoteServiceBaseUrl = "/api/Orders/";

    public OrderingService(HttpClient httpClient, ILogger<OrderingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<OrderRecord[]> GetOrders()
    {
        return _httpClient.GetFromJsonAsync<OrderRecord[]>(remoteServiceBaseUrl)!;
    }

    public Task CreateOrder(CreateOrderRequest request, Guid requestId)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, remoteServiceBaseUrl);
        requestMessage.Headers.Add("x-requestid", requestId.ToString());
        requestMessage.Content = JsonContent.Create(request);
        return _httpClient.SendAsync(requestMessage);
    }

    public async Task CancelOrder(int orderNumber)
    {
        _logger.LogInformation("Starting cancellation of order {OrderNumber}", orderNumber);
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{remoteServiceBaseUrl}cancel?api-version=1.0");
        requestMessage.Headers.Add("x-requestid", Guid.NewGuid().ToString());
        requestMessage.Content = JsonContent.Create(new { orderNumber });
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
    }

    public async Task RequestReturn(int orderNumber)
    {
        _logger.LogInformation("Starting return request for order {OrderNumber}", orderNumber);
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{remoteServiceBaseUrl}return?api-version=1.0");
        requestMessage.Headers.Add("x-requestid", Guid.NewGuid().ToString());
        requestMessage.Content = JsonContent.Create(new { orderNumber });
        var response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode();
    }
}

public record OrderRecord(
    int OrderNumber,
    DateTime Date,
    Ordering.Domain.AggregatesModel.OrderAggregate.OrderStatus Status,
    decimal Total);
