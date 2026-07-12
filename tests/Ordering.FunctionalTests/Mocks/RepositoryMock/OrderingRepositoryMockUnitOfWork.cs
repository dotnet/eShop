using System.Threading;

using eShop.Ordering.Domain.Seedwork;

using MediatR;

namespace eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;

internal sealed class OrderingRepositoryMockUnitOfWork(IMediator mediator, OrderingRepositoryMockStore store) : IUnitOfWork
{
    private readonly List<Entity> _tracked = [];

    public void Track(Entity entity) => _tracked.Add(entity);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var count = _tracked.Count;
        await SaveEntitiesAsync(cancellationToken);
        return count;
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        store.Commit(_tracked);

        var domainEvents = _tracked
            .Where(entity => entity.DomainEvents is not null && entity.DomainEvents.Any())
            .SelectMany(entity => entity.DomainEvents!)
            .ToList();

        foreach (var entity in _tracked)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }

        _tracked.Clear();
        return true;
    }

    public void Dispose()
    {
    }
}
