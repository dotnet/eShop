namespace eShop.Ordering.API.Application.IntegrationEvents.Events;

public record ShipmentCompletedIntegrationEvent : IntegrationEvent
{
    public int ShipmentId { get; init; }
    public int OrderId { get; init; }
    public DateTime CompletedAt { get; init; }
}
