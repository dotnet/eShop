using System.Reflection;
using System.Threading;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShop.Ordering.FunctionalTests.Infrastructure;

internal sealed class OrderingAspireTestHost : IAsyncDisposable
{
    private readonly IHost _app;

    public OrderingAspireTestHost(Assembly testAssembly, bool includeRabbitMq = false)
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = testAssembly.FullName,
            DisableDashboard = true
        };

        var appBuilder = DistributedApplication.CreateBuilder(options);
        Postgres = appBuilder.AddPostgres("OrderingDB");
        IdentityDB = appBuilder.AddPostgres("IdentityDB");
        IdentityApi = appBuilder.AddProject<Projects.Identity_API>("identity-api").WithReference(IdentityDB);

        if (includeRabbitMq)
        {
            EventBus = appBuilder.AddRabbitMQ("eventbus");
        }

        _app = appBuilder.Build();
    }

    public IResourceBuilder<PostgresServerResource> Postgres { get; }
    public IResourceBuilder<PostgresServerResource> IdentityDB { get; }
    public IResourceBuilder<ProjectResource> IdentityApi { get; }
    public IResourceBuilder<RabbitMQServerResource>? EventBus { get; }

    public string IdentityApiUrl => IdentityApi.GetEndpoint("http").Url;

    public async Task<OrderingAspireEndpoints> StartAsync(bool waitForEventBus, CancellationToken cancellationToken = default)
    {
        await _app.StartAsync(cancellationToken);

        var resourceNotifications = _app.Services.GetRequiredService<ResourceNotificationService>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));

        await resourceNotifications.WaitForResourceHealthyAsync(Postgres.Resource.Name, timeout.Token);

        string? eventBusConnectionString = null;
        if (waitForEventBus && EventBus is not null)
        {
            await resourceNotifications.WaitForResourceHealthyAsync(EventBus.Resource.Name, timeout.Token);
            eventBusConnectionString = await ((IResourceWithConnectionString)EventBus.Resource)
                .GetConnectionStringAsync(cancellationToken);
        }

        return new OrderingAspireEndpoints(
            await Postgres.Resource.GetConnectionStringAsync(cancellationToken),
            eventBusConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();

        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _app.Dispose();
        }
    }
}
