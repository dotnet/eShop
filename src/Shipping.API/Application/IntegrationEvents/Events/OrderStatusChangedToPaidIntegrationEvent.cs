namespace eShop.Shipping.API.Application.IntegrationEvents.Events;

public record OrderStatusChangedToPaidIntegrationEvent : IntegrationEvent
{
    public int OrderId { get; init; }
    public string OrderStatus { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerIdentityGuid { get; init; } = string.Empty;
    public IEnumerable<OrderStockItem> OrderStockItems { get; init; } = [];
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}
