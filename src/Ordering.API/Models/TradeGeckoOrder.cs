using System.Text.Json.Serialization;

namespace eShop.Ordering.API.Models;

public class TradeGeckoOrder
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("order_number")]
    public string OrderNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
    
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }
    
    [JsonPropertyName("total")]
    public decimal Total { get; set; }
    
    [JsonPropertyName("total_tax")]
    public decimal TotalTax { get; set; }
    
    [JsonPropertyName("financial_status")]
    public string FinancialStatus { get; set; } = string.Empty;
    
    [JsonPropertyName("fulfillment_status")]
    public string FulfillmentStatus { get; set; } = string.Empty;
    
    [JsonPropertyName("line_items")]
    public List<TradeGeckoOrderLineItem> LineItems { get; set; } = new();
    
    [JsonPropertyName("billing_address")]
    public TradeGeckoAddress? BillingAddress { get; set; }
    
    [JsonPropertyName("shipping_address")]
    public TradeGeckoAddress? ShippingAddress { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class TradeGeckoOrderLineItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("variant_id")]
    public int VariantId { get; set; }
    
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;
}

public class TradeGeckoAddress
{
    [JsonPropertyName("firstname")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("lastname")]
    public string LastName { get; set; } = string.Empty;
    
    [JsonPropertyName("company")]
    public string Company { get; set; } = string.Empty;
    
    [JsonPropertyName("address1")]
    public string Address1 { get; set; } = string.Empty;
    
    [JsonPropertyName("address2")]
    public string Address2 { get; set; } = string.Empty;
    
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;
    
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    
    [JsonPropertyName("zipcode")]
    public string ZipCode { get; set; } = string.Empty;
    
    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
    
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
}

public class TradeGeckoOrderResponse
{
    [JsonPropertyName("orders")]
    public List<TradeGeckoOrder> Orders { get; set; } = new();
} 