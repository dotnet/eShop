namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderStatusChangedToReceivedReturnIntegrationEvent : IntegrationEvent
{
    public OrderStatusChangedToReceivedReturnIntegrationEvent(int orderId, string buyerId)
    {
        OrderId = orderId;
        BuyerId = buyerId;
    }

    public int OrderId { get; }
    public string BuyerId { get; }
}
