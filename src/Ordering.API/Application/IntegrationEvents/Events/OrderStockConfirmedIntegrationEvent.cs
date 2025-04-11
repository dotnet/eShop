namespace Inked.Ordering.API.Application.IntegrationEvents.Events;

public record OrderStockConfirmedIntegrationEvent : IntegrationEvent
{
    public OrderStockConfirmedIntegrationEvent(int orderId)
    {
        OrderId = orderId;
    }

    public int OrderId { get; }
}
