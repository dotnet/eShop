namespace eShop.Shipping.API.Application.IntegrationEvents.Events;

public record OrderStockItem
{
    public int ProductId { get; init; }
    public int Units { get; init; }
}

public record ShipmentCancelledIntegrationEvent : IntegrationEvent
{
    public int ShipmentId { get; init; }
    public int OrderId { get; init; }
    public int ReturnWarehouseId { get; init; }
    public IEnumerable<OrderStockItem> OrderItems { get; init; } = [];
}
