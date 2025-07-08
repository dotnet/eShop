using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using eShop.Ordering.API.Models;
using Microsoft.Extensions.Options;

namespace eShop.Ordering.API.Services;

public class TradeGeckoService : ITradeGeckoService
{
    private readonly HttpClient _httpClient;
    private readonly TradeGeckoOptions _options;
    private readonly ILogger<TradeGeckoService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TradeGeckoService(
        HttpClient httpClient,
        IOptions<TradeGeckoOptions> options,
        ILogger<TradeGeckoService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<TradeGeckoOrder>> GetOrdersAsync(int? limit = null, int? page = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (limit.HasValue)
                queryParams.Add($"limit={limit.Value}");
            if (page.HasValue)
                queryParams.Add($"page={page.Value}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"/orders{queryString}");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var orderResponse = JsonSerializer.Deserialize<TradeGeckoOrderResponse>(content, _jsonOptions);

            return orderResponse?.Orders ?? new List<TradeGeckoOrder>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders from TradeGecko");
            return new List<TradeGeckoOrder>();
        }
    }

    public async Task<TradeGeckoOrder?> GetOrderAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/orders/{orderId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var order = JsonSerializer.Deserialize<TradeGeckoOrder>(content, _jsonOptions);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {OrderId} from TradeGecko", orderId);
            return null;
        }
    }

    public async Task<TradeGeckoOrder?> CreateOrderAsync(TradeGeckoOrder order)
    {
        try
        {
            var orderData = new { order = order };
            var json = JsonSerializer.Serialize(orderData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/orders", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdOrder = JsonSerializer.Deserialize<TradeGeckoOrder>(responseContent, _jsonOptions);

            return createdOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order in TradeGecko");
            return null;
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string financialStatus, string fulfillmentStatus)
    {
        try
        {
            var updateData = new 
            { 
                order = new 
                { 
                    financial_status = financialStatus,
                    fulfillment_status = fulfillmentStatus
                } 
            };
            var json = JsonSerializer.Serialize(updateData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/orders/{orderId}", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {OrderId} in TradeGecko", orderId);
            return false;
        }
    }
} 