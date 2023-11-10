using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IHost _app;

    public IResourceBuilder<PostgresContainerResource> Postgres { get; private set; }

    public CatalogApiFixture()
    {
        var options = new DistributedApplicationOptions { AssemblyName = typeof(CatalogApiFixture).Assembly.FullName, DisableDashboard = true };
        var appBuilder = DistributedApplication.CreateBuilder(options);
        Postgres = appBuilder.AddPostgresContainer("CatalogDB")
            .WithAnnotation(new ContainerImageAnnotation
            {
                Image = "ankane/pgvector",
                Tag = "latest"
            });
        _app = appBuilder.Build();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"ConnectionStrings:{Postgres.Resource.Name}", Postgres.Resource.GetConnectionString() },
                });
        });
        return base.CreateHost(builder);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
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

    public async Task InitializeAsync()
    {
        await _app.StartAsync();
    }
}
