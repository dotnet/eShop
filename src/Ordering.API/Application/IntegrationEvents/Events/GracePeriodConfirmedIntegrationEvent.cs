namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record GracePeriodConfirmedIntegrationEvent : IntegrationEvent
{
    public GracePeriodConfirmedIntegrationEvent(int orderId)
    {
        OrderId = orderId;
    }

    public int OrderId { get; }
}
