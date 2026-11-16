namespace eShop.Ordering.Domain.Seedwork;

public interface IDomainEventHandler<in TDomainEvent> : IDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);

    Task IDomainEventHandler.Handle(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        Handle((TDomainEvent)domainEvent, cancellationToken);
}

public interface IDomainEventHandler
{
    Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
