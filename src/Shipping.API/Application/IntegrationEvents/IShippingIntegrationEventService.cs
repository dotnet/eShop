namespace eShop.Shipping.API.Application.IntegrationEvents;

public interface IShippingIntegrationEventService
{
    Task PublishEventsThroughEventBusAsync(Guid transactionId);
    Task AddAndSaveEventAsync(IntegrationEvent evt);
}
