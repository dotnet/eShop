using System.Text.Json.Serialization;

namespace eShop.ClientApp.Models.Orders;

public class Order
{
    public Order()
    {
        SequenceNumber = 1;
        OrderItems = new List<OrderItem>();
    }

    public string BuyerId { get; set; }

    public int SequenceNumber { get; set; }

    [JsonPropertyName("Date")]
    public DateTime OrderDate { get; set; }

    [JsonPropertyName("Status")]
    public OrderStatus OrderStatus { get; set; }

    [JsonPropertyName("City")]
    public string ShippingCity { get; set; }

    [JsonPropertyName("Street")]
    public string ShippingStreet { get; set; }

    [JsonPropertyName("State")]
    public string ShippingState { get; set; }

    [JsonPropertyName("Country")]
    public string ShippingCountry { get; set; }

    [JsonPropertyName("ZipCode")]
    public string ShippingZipCode { get; set; }

    public int CardTypeId { get; set; }

    public string CardNumber { get; set; }

    public string CardHolderName { get; set; }

    public DateTime CardExpiration { get; set; }

    public string CardSecurityNumber { get; set; }

    [JsonPropertyName("Orderitems")]
    public List<OrderItem> OrderItems { get; set; }

    [JsonPropertyName("Total")]
    public decimal Total { get; set; }

    [JsonPropertyName("Ordernumber")]
    public int OrderNumber { get; set; }
}
