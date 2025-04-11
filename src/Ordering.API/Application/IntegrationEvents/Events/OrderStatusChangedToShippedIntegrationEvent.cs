namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderStatusChangedToShippedIntegrationEvent : IntegrationEvent
{
    public OrderStatusChangedToShippedIntegrationEvent(
        int orderId, OrderStatus orderStatus, string buyerName, string buyerIdentityGuid)
    {
        OrderId = orderId;
        OrderStatus = orderStatus;
        BuyerName = buyerName;
        BuyerIdentityGuid = buyerIdentityGuid;
    }

    public int OrderId { get; }
    public OrderStatus OrderStatus { get; }
    public string BuyerName { get; }
    public string BuyerIdentityGuid { get; }
}
