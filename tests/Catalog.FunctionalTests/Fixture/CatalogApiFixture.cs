using eShop.Catalog.API;
using eShop.Catalog.API.Model;
using eShop.Catalog.FunctionalTests.Configuration;
using eShop.Catalog.FunctionalTests.Infrastructure;
using eShop.Catalog.FunctionalTests.Mocks.RepositoryMock;

using eShop.Testing.Common;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace eShop.Catalog.FunctionalTests;

public sealed class CatalogApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly CatalogFunctionalTestMode _mode;
    private readonly CatalogAspireTestHost? _aspireHost;
    private readonly CatalogTestcontainersHost? _testcontainersHost;
    private readonly CatalogRepositoryMockStore _repositoryMockStore = new();
    private string _postgresConnectionString = string.Empty;
    private string? _eventBusConnectionString;
    private readonly string _eventBusSubscriptionClientName = $"Catalog.MessagingTest.{Guid.NewGuid():N}";

    public CatalogApiFixture()
        : this(FunctionalTestModeReader.ReadFromEnvironment())
    {
    }

    public CatalogApiFixture(CatalogFunctionalTestMode mode)
    {
        _mode = mode;

        if (_mode is CatalogFunctionalTestMode.Aspire
            or CatalogFunctionalTestMode.AspireMessagingOutbox
            or CatalogFunctionalTestMode.AspireMessagingRabbitMq)
        {
            _aspireHost = new CatalogAspireTestHost(
                typeof(CatalogApiFixture).Assembly,
                includeRabbitMq: _mode == CatalogFunctionalTestMode.AspireMessagingRabbitMq);
        }
        else if (_mode == CatalogFunctionalTestMode.Testcontainers)
        {
            _testcontainersHost = new CatalogTestcontainersHost();
        }
    }

    public CatalogFunctionalTestMode Mode => _mode;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestLogging();

        switch (_mode)
        {
            case CatalogFunctionalTestMode.RepositoryMock:
                builder.UseEnvironment("Build");
                builder.ConfigureTestServices(services => CatalogTestServiceConfiguration.ConfigureRepositoryMock(services, _repositoryMockStore));
                break;
            case CatalogFunctionalTestMode.EfCoreInMemory:
                builder.UseEnvironment("Build");
                builder.ConfigureTestServices(CatalogTestServiceConfiguration.ConfigureEfCoreInMemory);
                break;
            case CatalogFunctionalTestMode.Testcontainers:
                builder.ConfigureTestServices(CatalogTestServiceConfiguration.ConfigureSharedExternalDependencies);
                break;
            case CatalogFunctionalTestMode.Aspire:
                builder.ConfigureTestServices(CatalogTestServiceConfiguration.ConfigureSharedExternalDependencies);
                break;
            case CatalogFunctionalTestMode.AspireMessagingOutbox:
                builder.ConfigureTestServices(CatalogMessagingTestServiceConfiguration.ConfigureOutboxWithSpyBus);
                break;
            case CatalogFunctionalTestMode.AspireMessagingRabbitMq:
                builder.ConfigureTestServices(CatalogMessagingTestServiceConfiguration.ConfigureRabbitMqWithCaptureHandlers);
                break;
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            CatalogTestHostConfiguration.Apply(config, new CatalogTestHostConfigurationOptions
            {
                Mode = _mode,
                PostgresConnectionString = _postgresConnectionString,
                PostgresResourceName = _aspireHost?.Postgres.Resource.Name,
                EventBusConnectionString = _eventBusConnectionString,
                EventBusSubscriptionClientName = _mode == CatalogFunctionalTestMode.AspireMessagingRabbitMq
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
            case CatalogFunctionalTestMode.RepositoryMock:
                await _repositoryMockStore.ResetAsync();
                _ = Services;
                break;
            case CatalogFunctionalTestMode.EfCoreInMemory:
                _ = Services;
                await CatalogDatabaseHelper.EnsureInMemorySeededAsync(Services);
                break;
            case CatalogFunctionalTestMode.Aspire:
            case CatalogFunctionalTestMode.AspireMessagingOutbox:
            case CatalogFunctionalTestMode.AspireMessagingRabbitMq:
                var endpoints = await _aspireHost!.StartAsync(
                    waitForEventBus: _mode == CatalogFunctionalTestMode.AspireMessagingRabbitMq);
                _postgresConnectionString = endpoints.PostgresConnectionString;
                _eventBusConnectionString = endpoints.EventBusConnectionString;
                _ = Services;
                await CatalogDatabaseHelper.EnsurePostgresSeededAsync(Services);
                break;
            case CatalogFunctionalTestMode.Testcontainers:
                _postgresConnectionString = await _testcontainersHost!.StartAsync();
                _ = Services;
                await CatalogDatabaseHelper.EnsurePostgresSeededAsync(Services);
                break;
        }
    }

    public async Task<CatalogItem?> LoadPersistedCatalogItemAsync(int id)
    {
        return _mode switch
        {
            CatalogFunctionalTestMode.RepositoryMock => _repositoryMockStore.GetItemById(id),
            CatalogFunctionalTestMode.EfCoreInMemory => await CatalogDatabaseHelper.LoadItemFromScopedContextAsync(Services, id),
            CatalogFunctionalTestMode.Aspire or CatalogFunctionalTestMode.Testcontainers =>
                await CatalogDatabaseHelper.LoadItemFromPostgresAsync(_postgresConnectionString, id),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public async Task<long> GetPersistedCatalogItemCountAsync()
    {
        return _mode switch
        {
            CatalogFunctionalTestMode.RepositoryMock => _repositoryMockStore.Items.Count,
            CatalogFunctionalTestMode.EfCoreInMemory => await CatalogDatabaseHelper.GetCatalogItemCountFromScopedContextAsync(Services),
            CatalogFunctionalTestMode.Aspire or CatalogFunctionalTestMode.Testcontainers =>
                await CatalogDatabaseHelper.GetCatalogItemCountFromPostgresAsync(_postgresConnectionString),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
