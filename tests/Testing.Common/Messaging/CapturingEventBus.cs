using System.Collections.Concurrent;

using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;

namespace eShop.Testing.Common.Messaging;

public sealed class CapturingEventBus : IEventBus
{
    private readonly ConcurrentQueue<IntegrationEvent> _published = new();

    public IReadOnlyCollection<IntegrationEvent> Published => _published.ToArray();

    public void Reset()
    {
        while (_published.TryDequeue(out _))
        {
        }
    }

    public Task PublishAsync(IntegrationEvent @event)
    {
        _published.Enqueue(@event);
        return Task.CompletedTask;
    }
}
