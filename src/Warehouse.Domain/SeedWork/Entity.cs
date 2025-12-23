namespace eShop.Warehouse.Domain.SeedWork;

public abstract class Entity
{
    int? _requestedHashCode;
    int _Id;

    public virtual int Id
    {
        get => _Id;
        protected set => _Id = value;
    }

    private List<INotification>? _domainEvents;
    public IReadOnlyCollection<INotification>? DomainEvents => _domainEvents?.AsReadOnly();

    public void AddDomainEvent(INotification eventItem)
    {
        _domainEvents ??= new List<INotification>();
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem)
    {
        _domainEvents?.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    public bool IsTransient()
    {
        return Id == default;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (GetType() != obj.GetType())
            return false;

        if (entity.IsTransient() || IsTransient())
            return false;

        return entity.Id == Id;
    }

    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            _requestedHashCode ??= Id.GetHashCode() ^ 31;
            return _requestedHashCode.Value;
        }

        return base.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, null) ? Equals(right, null) : left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }
}
