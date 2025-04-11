namespace Inked.PaymentProcessor.IntegrationEvents.Events;

public record OrderStatusChangedToStockConfirmedIntegrationEvent(int OrderId) : IntegrationEvent;
