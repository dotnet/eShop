namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderPaymentFailedIntegrationEvent : IntegrationEvent
{
    public OrderPaymentFailedIntegrationEvent(int orderId)
    {
        OrderId = orderId;
    }

    public int OrderId { get; }
}
