namespace eShop.Shipping.API.Application.IntegrationEvents.Events;

public record ShipmentStatusChangedIntegrationEvent : IntegrationEvent
{
    public int ShipmentId { get; init; }
    public int OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ShipperName { get; init; }
    public DateTime Timestamp { get; init; }
}
