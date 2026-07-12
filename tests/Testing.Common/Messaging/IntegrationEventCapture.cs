using System.Collections.Concurrent;

using eShop.EventBus.Events;

namespace eShop.Testing.Common.Messaging;

public static class IntegrationEventCapture
{
    private static readonly ConcurrentQueue<IntegrationEvent> Events = new();

    public static IReadOnlyCollection<IntegrationEvent> All => Events.ToArray();

    public static void Reset()
    {
        while (Events.TryDequeue(out _))
        {
        }
    }

    public static void Add(IntegrationEvent integrationEvent) => Events.Enqueue(integrationEvent);

    public static async Task WaitForCountAsync(int expectedCount, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        while (!timeoutSource.IsCancellationRequested)
        {
            if (Events.Count >= expectedCount)
            {
                return;
            }

            await Task.Delay(100, timeoutSource.Token);
        }

        throw new TimeoutException(
            $"Timed out waiting for {expectedCount} captured integration events. Captured {Events.Count}.");
    }
}
