namespace eShop.Ordering.Domain.Events;

/// <summary>
/// Base class for entities that raise domain events. Events are dispatched by
/// <c>OrderingContext.SaveEntitiesAsync</c> before changes are saved.
/// </summary>
public abstract class EntityWithDomainEvents
{
    private readonly List<INotification> _domainEvents = [];

    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(INotification eventItem) => _domainEvents.Add(eventItem);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
