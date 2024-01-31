namespace eShop.PaymentProcessor.IntegrationEvents.Events;

public record OrderPaymentFailedIntegrationEvent(int OrderId) : IntegrationEvent;
