using Testcontainers.PostgreSql;

namespace eShop.Catalog.FunctionalTests.Infrastructure;

internal sealed class CatalogTestcontainersHost : IAsyncDisposable
{
    private PostgreSqlContainer? _postgresContainer;

    public async Task<string> StartAsync()
    {
        _postgresContainer = new PostgreSqlBuilder("ankane/pgvector:latest")
            .WithDatabase("CatalogDB")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();
        return _postgresContainer.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }
}
