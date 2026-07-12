using System.Reflection;

using eShop.Ordering.API;
using eShop.Ordering.FunctionalTests.Configuration;
using eShop.Ordering.FunctionalTests.Infrastructure;
using eShop.Ordering.FunctionalTests.Mocks;
using eShop.Ordering.FunctionalTests.Mocks.RepositoryMock;

using eShop.Testing.Common;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShop.Ordering.FunctionalTests;

public sealed class OrderingApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly OrderingFunctionalTestMode _mode;
    private readonly OrderingAspireTestHost? _aspireHost;
    private readonly OrderingTestcontainersHost? _testcontainersHost;
    private readonly OrderingRepositoryMockStore _repositoryMockStore = new();
    private string _postgresConnectionString = string.Empty;
    private string? _eventBusConnectionString;
    private readonly string _eventBusSubscriptionClientName = $"Ordering.MessagingTest.{Guid.NewGuid():N}";

    public OrderingApiFixture()
        : this(FunctionalTestModeReader.ReadFromEnvironment())
    {
    }

    public OrderingApiFixture(OrderingFunctionalTestMode mode)
    {
        _mode = mode;

        if (_mode is OrderingFunctionalTestMode.Aspire
            or OrderingFunctionalTestMode.AspireMessagingOutbox
            or OrderingFunctionalTestMode.AspireMessagingRabbitMq)
        {
            _aspireHost = new OrderingAspireTestHost(
                typeof(OrderingApiFixture).Assembly,
                includeRabbitMq: _mode == OrderingFunctionalTestMode.AspireMessagingRabbitMq);
        }
        else if (_mode == OrderingFunctionalTestMode.Testcontainers)
        {
            _testcontainersHost = new OrderingTestcontainersHost();
        }
    }

    public OrderingFunctionalTestMode Mode => _mode;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestLogging();

        switch (_mode)
        {
            case OrderingFunctionalTestMode.RepositoryMock:
                builder.UseEnvironment("Build");
                builder.ConfigureTestServices(services => OrderingTestServiceConfiguration.ConfigureRepositoryMock(services, _repositoryMockStore));
                break;
            case OrderingFunctionalTestMode.EfCoreInMemory:
                builder.UseEnvironment("Build");
                builder.ConfigureTestServices(OrderingTestServiceConfiguration.ConfigureEfCoreInMemory);
                break;
            case OrderingFunctionalTestMode.Aspire:
            case OrderingFunctionalTestMode.Testcontainers:
                builder.ConfigureTestServices(OrderingTestServiceConfiguration.ConfigureSharedExternalDependencies);
                break;
            case OrderingFunctionalTestMode.AspireMessagingOutbox:
                builder.ConfigureTestServices(OrderingMessagingTestServiceConfiguration.ConfigureOutboxWithSpyBus);
                break;
            case OrderingFunctionalTestMode.AspireMessagingRabbitMq:
                builder.ConfigureTestServices(OrderingMessagingTestServiceConfiguration.ConfigureRabbitMqWithCaptureHandlers);
                break;
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new AutoAuthorizeStartupFilter());
        });

        builder.ConfigureHostConfiguration(config =>
        {
            OrderingTestHostConfiguration.Apply(config, new OrderingTestHostConfigurationOptions
            {
                Mode = _mode,
                PostgresConnectionString = _postgresConnectionString,
                PostgresResourceName = _aspireHost?.Postgres.Resource.Name,
                IdentityApiUrl = _aspireHost?.IdentityApiUrl,
                EventBusConnectionString = _eventBusConnectionString,
                EventBusSubscriptionClientName = _mode == OrderingFunctionalTestMode.AspireMessagingRabbitMq
                    ? _eventBusSubscriptionClientName
                    : null
            });
        });

        return base.CreateHost(builder);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        if (_testcontainersHost is not null)
        {
            await _testcontainersHost.DisposeAsync();
        }

        if (_aspireHost is not null)
        {
            await _aspireHost.DisposeAsync();
        }
    }

    public async ValueTask InitializeAsync()
    {
        switch (_mode)
        {
            case OrderingFunctionalTestMode.RepositoryMock:
                await _repositoryMockStore.ResetAsync();
                _ = Services;
                break;
            case OrderingFunctionalTestMode.EfCoreInMemory:
                _ = Services;
                await OrderingDatabaseHelper.EnsureInMemorySeededAsync(Services);
                break;
            case OrderingFunctionalTestMode.Aspire:
            case OrderingFunctionalTestMode.AspireMessagingOutbox:
            case OrderingFunctionalTestMode.AspireMessagingRabbitMq:
                var endpoints = await _aspireHost!.StartAsync(
                    waitForEventBus: _mode == OrderingFunctionalTestMode.AspireMessagingRabbitMq);
                _postgresConnectionString = endpoints.PostgresConnectionString;
                _eventBusConnectionString = endpoints.EventBusConnectionString;
                _ = Services;
                await OrderingDatabaseHelper.EnsurePostgresSeededAsync(Services);
                break;
            case OrderingFunctionalTestMode.Testcontainers:
                _postgresConnectionString = await _testcontainersHost!.StartAsync();
                _ = Services;
                await OrderingDatabaseHelper.EnsurePostgresSeededAsync(Services);
                break;
        }
    }

    public async Task<int> GetPersistedOrderCountAsync()
    {
        return _mode switch
        {
            OrderingFunctionalTestMode.RepositoryMock => _repositoryMockStore.Orders.Count,
            OrderingFunctionalTestMode.EfCoreInMemory => await OrderingDatabaseHelper.GetOrderCountFromScopedContextAsync(Services),
            OrderingFunctionalTestMode.Aspire or OrderingFunctionalTestMode.AspireMessagingOutbox or OrderingFunctionalTestMode.AspireMessagingRabbitMq =>
                await OrderingDatabaseHelper.GetOrderCountFromScopedContextAsync(Services),
            OrderingFunctionalTestMode.Testcontainers =>
                await OrderingDatabaseHelper.GetOrderCountFromPostgresAsync(_postgresConnectionString),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
