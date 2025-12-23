namespace eShop.Shipping.API.Application.IntegrationEvents.Events;

public record OrderCancelledIntegrationEvent : IntegrationEvent
{
    public int OrderId { get; init; }
    public string BuyerIdentityGuid { get; init; } = string.Empty;
    public IEnumerable<OrderStockItem> OrderStockItems { get; init; } = [];
}
