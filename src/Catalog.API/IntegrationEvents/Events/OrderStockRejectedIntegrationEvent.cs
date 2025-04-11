namespace Inked.Catalog.API.IntegrationEvents.Events;

public record OrderStockRejectedIntegrationEvent(int OrderId, List<ConfirmedOrderStockItem> OrderStockItems)
    : IntegrationEvent;
