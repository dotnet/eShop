namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderPaymentSucceededIntegrationEvent : IntegrationEvent
{
    public OrderPaymentSucceededIntegrationEvent(int orderId)
    {
        OrderId = orderId;
    }

    public int OrderId { get; }
}
