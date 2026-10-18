namespace eShop.Catalog.API.IntegrationEvents.Events;

public record OrderStatusChangedToCancelledIntegrationEvent(int OrderId) : IntegrationEvent;
