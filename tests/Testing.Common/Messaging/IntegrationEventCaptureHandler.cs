using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;

namespace eShop.Testing.Common.Messaging;

public sealed class IntegrationEventCaptureHandler<TIntegrationEvent> : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    public Task Handle(TIntegrationEvent @event)
    {
        IntegrationEventCapture.Add(@event);
        return Task.CompletedTask;
    }
}
