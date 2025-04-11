namespace Inked.Catalog.API.IntegrationEvents.Events;

public record ConfirmedOrderStockItem(int ProductId, bool HasStock);
