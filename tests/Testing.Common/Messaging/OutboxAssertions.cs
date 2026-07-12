using eShop.IntegrationEventLogEF;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Testing.Common.Messaging;

public static class OutboxAssertions
{
    public static async Task<IReadOnlyList<IntegrationEventLogEntry>> GetPublishedEventsAsync<TContext>(
        IServiceProvider services,
        CancellationToken cancellationToken = default,
        params string[] eventTypeShortNames)
        where TContext : DbContext
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        var query = context.Set<IntegrationEventLogEntry>()
            .AsNoTracking()
            .Where(entry => entry.State == EventStateEnum.Published);

        if (eventTypeShortNames.Length > 0)
        {
            query = query.Where(entry => eventTypeShortNames.Any(shortName => entry.EventTypeName.EndsWith(shortName)));
        }

        return await query
            .OrderBy(entry => entry.CreationTime)
            .ToListAsync(cancellationToken);
    }

    public static async Task<int> CountPublishedEventsAsync<TContext>(
        IServiceProvider services,
        CancellationToken cancellationToken = default,
        params string[] eventTypeShortNames)
        where TContext : DbContext
    {
        var events = await GetPublishedEventsAsync<TContext>(services, cancellationToken, eventTypeShortNames);
        return events.Count;
    }
}
