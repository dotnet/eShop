using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using StackExchange.Redis;

namespace eShop.TestStack;

internal sealed class RedisStore(StackContext context)
{
    private async Task<ConnectionMultiplexer> ConnectAsync()
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { { context.Config.Redis.Host, context.Config.Redis.Port } },
            Password = context.Variable(context.Config.Redis.PasswordVariable),
            AllowAdmin = true,
            AbortOnConnectFail = true,
            ConnectTimeout = 3000,
            ConnectRetry = 1,
        };
        return await ConnectionMultiplexer.ConnectAsync(options);
    }

    public async Task PingAsync()
    {
        await using var redis = await ConnectAsync();
        await redis.GetDatabase().PingAsync();
    }

    public async Task FlushAsync()
    {
        await using var redis = await ConnectAsync();
        foreach (var server in redis.GetServers())
        {
            await server.FlushAllDatabasesAsync();
        }
    }

    public async Task<int> LoadAsync(IReadOnlyCollection<RedisEntry> entries)
    {
        if (entries.Count == 0)
        {
            return 0;
        }

        await using var redis = await ConnectAsync();
        var database = redis.GetDatabase();
        foreach (var entry in entries)
        {
            await database.StringSetAsync(entry.Key, entry.Value, entry.Ttl is { } ttl ? new Expiration(ttl) : default(Expiration));
        }

        return entries.Count;
    }
}

internal sealed record QueueState(string Queue, bool Exists, uint Messages, uint Consumers);

internal sealed class RabbitMqStore(StackContext context)
{
    private Task<IConnection> ConnectAsync(CancellationToken ct) => new ConnectionFactory
    {
        HostName = context.Config.RabbitMq.Host,
        Port = context.Config.RabbitMq.Port,
        UserName = context.Config.RabbitMq.Username,
        Password = context.Variable(context.Config.RabbitMq.PasswordVariable),
        RequestedConnectionTimeout = TimeSpan.FromSeconds(3),
        AutomaticRecoveryEnabled = false,
    }.CreateConnectionAsync("eshop-teststack", ct);

    /// <summary>Passively inspects each configured queue (never creates queues the services own).</summary>
    public async Task<IReadOnlyList<QueueState>> InspectAsync(CancellationToken ct)
    {
        await using var connection = await ConnectAsync(ct);
        var states = new List<QueueState>();
        foreach (var queue in context.Config.RabbitMq.Queues)
        {
            // A failed passive declare closes the channel, so use one channel per queue.
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            try
            {
                var ok = await channel.QueueDeclarePassiveAsync(queue, ct);
                states.Add(new QueueState(queue, true, ok.MessageCount, ok.ConsumerCount));
            }
            catch (OperationInterruptedException)
            {
                states.Add(new QueueState(queue, false, 0, 0));
            }
        }

        return states;
    }

    /// <summary>Drops every ready message from the configured queues. Returns the number of purged messages.</summary>
    public async Task<uint> PurgeAsync(CancellationToken ct)
    {
        await using var connection = await ConnectAsync(ct);
        uint purged = 0;
        foreach (var queue in context.Config.RabbitMq.Queues)
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            try
            {
                await channel.QueueDeclarePassiveAsync(queue, ct);
                purged += await channel.QueuePurgeAsync(queue, ct);
            }
            catch (OperationInterruptedException)
            {
                // Queue not declared yet (its service hasn't started); nothing to purge.
            }
        }

        return purged;
    }
}
