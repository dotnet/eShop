using Microsoft.Extensions.DependencyInjection;

namespace eShop.Ordering.Infrastructure;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dispatches domain events to the <see cref="IDomainEventHandler"/> services registered
/// as keyed services, keyed by the domain event type.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        foreach (var handler in serviceProvider.GetKeyedServices<IDomainEventHandler>(domainEvent.GetType()))
        {
            await handler.Handle(domainEvent, cancellationToken);
        }
    }
}

public static class DomainEventServiceCollectionExtensions
{
    public static IServiceCollection AddDomainEventHandler<TDomainEvent, THandler>(this IServiceCollection services)
        where TDomainEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TDomainEvent>
    {
        services.AddKeyedTransient<IDomainEventHandler, THandler>(typeof(TDomainEvent));
        return services;
    }
}
