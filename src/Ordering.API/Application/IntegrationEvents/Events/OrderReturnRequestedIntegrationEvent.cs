namespace Inked.WebApp.Services.OrderStatus.IntegrationEvents;

public record OrderReturnRequestedIntegrationEvent : IntegrationEvent
{
    public OrderReturnRequestedIntegrationEvent(
        int orderId, Ordering.Domain.AggregatesModel.OrderAggregate.OrderStatus orderStatus, string buyerName,
        string buyerIdentityGuid)
    {
        OrderId = orderId;
        OrderStatus = orderStatus;
        BuyerName = buyerName;
        BuyerIdentityGuid = buyerIdentityGuid;
    }

    public int OrderId { get; }
    public Ordering.Domain.AggregatesModel.OrderAggregate.OrderStatus OrderStatus { get; }
    public string BuyerName { get; }
    public string BuyerIdentityGuid { get; }
}
