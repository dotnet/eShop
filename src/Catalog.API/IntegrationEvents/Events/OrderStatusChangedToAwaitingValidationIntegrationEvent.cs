namespace Inked.Catalog.API.IntegrationEvents.Events;

public record OrderStatusChangedToAwaitingValidationIntegrationEvent(
    int OrderId,
    IEnumerable<OrderStockItem> OrderStockItems) : IntegrationEvent;
