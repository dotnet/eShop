namespace eShop.Shipping.API.Application.IntegrationEvents.Events;

public record ShipmentCreatedIntegrationEvent : IntegrationEvent
{
    public int ShipmentId { get; init; }
    public int OrderId { get; init; }
    public List<int> RouteWarehouseIds { get; init; } = [];
    public string CustomerAddress { get; init; } = string.Empty;
}
