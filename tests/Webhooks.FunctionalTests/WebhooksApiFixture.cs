using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eShop.Webhooks.FunctionalTests;

public sealed class WebhooksApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IHost _app;
    private readonly IResourceBuilder<PostgresServerResource> _database;
    private readonly IResourceBuilder<RabbitMQServerResource> _rabbitMq;
    private string _databaseConnectionString = null!;
    private string _rabbitMqConnectionString = null!;

    public WebhooksApiFixture()
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = typeof(WebhooksApiFixture).Assembly.FullName,
            DisableDashboard = true
        };
        var appBuilder = DistributedApplication.CreateBuilder(options);
        _database = appBuilder.AddPostgres("webhooksdb");
        _rabbitMq = appBuilder.AddRabbitMQ("eventbus");
        _app = appBuilder.Build();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{_database.Resource.Name}"] = _databaseConnectionString,
                [$"ConnectionStrings:{_rabbitMq.Resource.Name}"] = _rabbitMqConnectionString,
                ["Identity:Url"] = "http://identity.test"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGrantUrlTesterService>();
            services.AddSingleton<IGrantUrlTesterService, ConfigurableGrantUrlTester>();
            services.AddSingleton<IStartupFilter>(new TestAuthenticationStartupFilter());
        });
        return base.CreateHost(builder);
    }

    public async ValueTask InitializeAsync()
    {
        await _app.StartAsync();
        _databaseConnectionString = (await _database.Resource.GetConnectionStringAsync())!;
        _rabbitMqConnectionString = (await _rabbitMq.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None))!;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
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

    private sealed class TestAuthenticationStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.UseMiddleware<TestAuthenticationMiddleware>();
                next(app);
            };
    }
}

public sealed class ConfigurableGrantUrlTester : IGrantUrlTesterService
{
    public Task<bool> TestGrantUrl(string urlHook, string url, string token) =>
        Task.FromResult(!url.Contains("reject", StringComparison.OrdinalIgnoreCase));
}
