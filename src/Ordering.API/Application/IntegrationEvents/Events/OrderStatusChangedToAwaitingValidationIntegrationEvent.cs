namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderStatusChangedToAwaitingValidationIntegrationEvent : IntegrationEvent
{
    public OrderStatusChangedToAwaitingValidationIntegrationEvent(
        int orderId, OrderStatus orderStatus, string buyerName, string buyerIdentityGuid,
        IEnumerable<OrderStockItem> orderStockItems)
    {
        OrderId = orderId;
        OrderStockItems = orderStockItems;
        OrderStatus = orderStatus;
        BuyerName = buyerName;
        BuyerIdentityGuid = buyerIdentityGuid;
    }

    public int OrderId { get; }
    public OrderStatus OrderStatus { get; }
    public string BuyerName { get; }
    public string BuyerIdentityGuid { get; }
    public IEnumerable<OrderStockItem> OrderStockItems { get; }
}

public record OrderStockItem
{
    public OrderStockItem(int productId, int units)
    {
        ProductId = productId;
        Units = units;
    }

    public int ProductId { get; }
    public int Units { get; }
}
