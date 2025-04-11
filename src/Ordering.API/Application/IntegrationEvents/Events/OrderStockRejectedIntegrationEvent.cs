namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderStockRejectedIntegrationEvent : IntegrationEvent
{
    public OrderStockRejectedIntegrationEvent(int orderId,
        List<ConfirmedOrderStockItem> orderStockItems)
    {
        OrderId = orderId;
        OrderStockItems = orderStockItems;
    }

    public int OrderId { get; }

    public List<ConfirmedOrderStockItem> OrderStockItems { get; }
}

public record ConfirmedOrderStockItem
{
    public ConfirmedOrderStockItem(int productId, bool hasStock)
    {
        ProductId = productId;
        HasStock = hasStock;
    }

    public int ProductId { get; }
    public bool HasStock { get; }
}
