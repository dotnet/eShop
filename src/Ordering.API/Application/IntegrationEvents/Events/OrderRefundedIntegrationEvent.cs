namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderRefundedIntegrationEvent : IntegrationEvent
{
    public OrderRefundedIntegrationEvent(int orderId, string buyerId)
    {
        OrderId = orderId;
        BuyerId = buyerId;
    }

    public int OrderId { get; }
    public string BuyerId { get; }
}
