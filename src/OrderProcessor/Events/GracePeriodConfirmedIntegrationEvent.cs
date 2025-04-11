using Inked.EventBus.Events;

namespace Inked.OrderProcessor.Events;

public record GracePeriodConfirmedIntegrationEvent : IntegrationEvent
{
    public GracePeriodConfirmedIntegrationEvent(int orderId)
    {
        OrderId = orderId;
    }

    public int OrderId { get; }
}
