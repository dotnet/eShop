using System.Text.Json.Serialization;

namespace eShop.Catalog.API.Model;

public class TradeGeckoProduct
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("retail_price")]
    public decimal RetailPrice { get; set; }

    [JsonPropertyName("wholesale_price")]
    public decimal WholesalePrice { get; set; }

    [JsonPropertyName("variant_ids")]
    public List<int> VariantIds { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class TradeGeckoProductResponse
{
    [JsonPropertyName("products")]
    public List<TradeGeckoProduct> Products { get; set; } = new();
}

public class TradeGeckoProductVariant
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("retail_price")]
    public decimal RetailPrice { get; set; }

    [JsonPropertyName("wholesale_price")]
    public decimal WholesalePrice { get; set; }

    [JsonPropertyName("stock_on_hand")]
    public int StockOnHand { get; set; }

    [JsonPropertyName("stock_level")]
    public int StockLevel { get; set; }
} 