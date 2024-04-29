using System.Text.Json.Serialization;

namespace eShop.ClientApp.Models.Orders;

public class OrderItem
{
    public string ProductId { get; set; }
    public Guid? OrderId { get; set; }

    [JsonPropertyName("UnitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("ProductName")]
    public string ProductName { get; set; }

    [JsonPropertyName("PictureUrl")]
    public string PictureUrl { get; set; }

    [JsonPropertyName("Units")]
    public int Quantity { get; set; }

    public decimal Discount { get; set; }
    public decimal Total => Quantity * UnitPrice;

    public override string ToString()
    {
        return String.Format("Product Id: {0}, Quantity: {1}", ProductId, Quantity);
    }
}
