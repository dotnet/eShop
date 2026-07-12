using Testcontainers.PostgreSql;

namespace eShop.Ordering.FunctionalTests.Infrastructure;

internal sealed class OrderingTestcontainersHost : IAsyncDisposable
{
    private PostgreSqlContainer? _postgresContainer;

    public async Task<string> StartAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("OrderingDB")
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
