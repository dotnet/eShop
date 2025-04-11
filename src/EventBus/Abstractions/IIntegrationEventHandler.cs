namespace Inked.EventBus.Abstractions;

public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task IIntegrationEventHandler.Handle(IntegrationEvent @event)
    {
        return Handle((TIntegrationEvent)@event);
    }

    Task Handle(TIntegrationEvent @event);
}

public interface IIntegrationEventHandler
{
    Task Handle(IntegrationEvent @event);
}
