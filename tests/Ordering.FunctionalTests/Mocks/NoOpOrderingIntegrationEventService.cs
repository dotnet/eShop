using eShop.EventBus.Events;
using eShop.Ordering.API.Application.IntegrationEvents;

namespace eShop.Ordering.FunctionalTests.Mocks;

internal sealed class NoOpOrderingIntegrationEventService : IOrderingIntegrationEventService
{
    public Task AddAndSaveEventAsync(IntegrationEvent evt) => Task.CompletedTask;

    public Task PublishEventsThroughEventBusAsync(Guid transactionId) => Task.CompletedTask;
}
