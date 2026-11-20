using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace eShop.Basket.FunctionalTests;

public sealed class RedisFixture : IAsyncLifetime
{
    private readonly IHost _app;
    private readonly IResourceBuilder<RedisResource> _redis;
    private IConnectionMultiplexer? _connection;

    public IConnectionMultiplexer Connection =>
        _connection ?? throw new InvalidOperationException("The Redis fixture has not been initialized.");

    public RedisFixture()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(RedisFixture).Assembly.FullName,
            DisableDashboard = true
        });
        _redis = builder.AddRedis("redis");
        _app = builder.Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _app.StartAsync();
        var notifications = _app.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync(_redis.Resource.Name);

        var connectionString = await _redis.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        _connection = await ConnectionMultiplexer.ConnectAsync(connectionString!);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _app.Dispose();
        }
    }
}
