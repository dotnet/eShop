using System.IO;
using System.Threading;

using eShop.Ordering.API.Infrastructure;
using eShop.Ordering.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

namespace eShop.Ordering.FunctionalTests.Infrastructure;

internal static class OrderingDatabaseHelper
{
    private const int MaxPostgresAttempts = 30;

    public static async Task EnsureInMemorySeededAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await new OrderingContextSeed().SeedAsync(context);
    }

    public static async Task EnsurePostgresSeededAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await ExecuteWithPostgresRetryAsync(async () =>
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderingContext>();
            await context.Database.MigrateAsync(cancellationToken);
            await new OrderingContextSeed().SeedAsync(context);
        }, cancellationToken);
    }

    public static async Task<int> GetOrderCountFromScopedContextAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPostgresRetryAsync(async () =>
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderingContext>();
            return await context.Orders.CountAsync(cancellationToken);
        }, cancellationToken);
    }

    public static async Task<int> GetOrderCountFromPostgresAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPostgresRetryAsync(async () =>
        {
            var options = new DbContextOptionsBuilder<OrderingContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using var context = new OrderingContext(options);
            return await context.Orders.CountAsync(cancellationToken);
        }, cancellationToken);
    }

    private static async Task ExecuteWithPostgresRetryAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await ExecuteWithPostgresRetryAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);
    }

    private static async Task<T> ExecuteWithPostgresRetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxPostgresAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await action();
            }
            catch (Exception ex) when (IsTransientPostgresError(ex) && attempt < MaxPostgresAttempts)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Failed to connect to PostgreSQL.");
    }

    private static bool IsTransientPostgresError(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException or EndOfStreamException or TimeoutException)
            {
                return true;
            }
        }

        return ex is InvalidOperationException { Message: var message }
            && message.Contains("transient failure", StringComparison.OrdinalIgnoreCase);
    }
}
