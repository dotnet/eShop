using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using eShop.Catalog.API.Model;
using Microsoft.Extensions.Options;

namespace eShop.Catalog.API.Services;

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

    public async Task<List<TradeGeckoProduct>> GetProductsAsync(int? limit = null, int? page = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (limit.HasValue)
                queryParams.Add($"limit={limit.Value}");
            if (page.HasValue)
                queryParams.Add($"page={page.Value}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"/products{queryString}");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var productResponse = JsonSerializer.Deserialize<TradeGeckoProductResponse>(content, _jsonOptions);

            return productResponse?.Products ?? new List<TradeGeckoProduct>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from TradeGecko");
            return new List<TradeGeckoProduct>();
        }
    }

    public async Task<TradeGeckoProduct?> GetProductAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/products/{productId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<TradeGeckoProduct>(content, _jsonOptions);

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} from TradeGecko", productId);
            return null;
        }
    }

    public async Task<List<TradeGeckoProductVariant>> GetProductVariantsAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/products/{productId}/variants");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var variants = JsonSerializer.Deserialize<List<TradeGeckoProductVariant>>(content, _jsonOptions);

            return variants ?? new List<TradeGeckoProductVariant>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching variants for product {ProductId} from TradeGecko", productId);
            return new List<TradeGeckoProductVariant>();
        }
    }

    public async Task<TradeGeckoProductVariant?> GetVariantAsync(int variantId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/variants/{variantId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var variant = JsonSerializer.Deserialize<TradeGeckoProductVariant>(content, _jsonOptions);

            return variant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching variant {VariantId} from TradeGecko", variantId);
            return null;
        }
    }

    public async Task<bool> UpdateVariantStockAsync(int variantId, int stockLevel)
    {
        try
        {
            var updateData = new { variant = new { stock_level = stockLevel } };
            var json = JsonSerializer.Serialize(updateData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/variants/{variantId}", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock level for variant {VariantId} in TradeGecko", variantId);
            return false;
        }
    }
} 