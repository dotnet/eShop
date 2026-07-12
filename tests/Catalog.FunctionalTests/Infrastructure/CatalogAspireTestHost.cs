using System.Reflection;
using System.Threading;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShop.Catalog.FunctionalTests.Infrastructure;

internal sealed class CatalogAspireTestHost : IAsyncDisposable
{
    private readonly IHost _app;

    public CatalogAspireTestHost(Assembly testAssembly, bool includeRabbitMq = false)
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = testAssembly.FullName,
            DisableDashboard = true
        };

        var appBuilder = DistributedApplication.CreateBuilder(options);
        Postgres = appBuilder.AddPostgres("CatalogDB")
            .WithImage("ankane/pgvector")
            .WithImageTag("latest");

        if (includeRabbitMq)
        {
            EventBus = appBuilder.AddRabbitMQ("eventbus");
        }

        _app = appBuilder.Build();
    }

    public IResourceBuilder<PostgresServerResource> Postgres { get; }
    public IResourceBuilder<RabbitMQServerResource>? EventBus { get; }

    public async Task<CatalogAspireEndpoints> StartAsync(bool waitForEventBus, CancellationToken cancellationToken = default)
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

        return new CatalogAspireEndpoints(
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
