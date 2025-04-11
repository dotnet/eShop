using Inked.EventBus.Events;

namespace Inked.WebApp.Services.OrderStatus.IntegrationEvents;

public record OrderStatusChangedToCancelledIntegrationEvent : IntegrationEvent
{
    public OrderStatusChangedToCancelledIntegrationEvent(
        int orderId, string orderStatus, string buyerName, string buyerIdentityGuid)
    {
        OrderId = orderId;
        OrderStatus = orderStatus;
        BuyerName = buyerName;
        BuyerIdentityGuid = buyerIdentityGuid;
    }

    public int OrderId { get; }
    public string OrderStatus { get; }
    public string BuyerName { get; }
    public string BuyerIdentityGuid { get; }
}
