namespace Inked.Catalog.API.IntegrationEvents.Events;

public record OrderStatusChangedToPaidIntegrationEvent(int OrderId, IEnumerable<OrderStockItem> OrderStockItems)
    : IntegrationEvent;
