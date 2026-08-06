using Npgsql;

namespace eShop.OrderProcessor.Services;

public interface IGracePeriodOrdersRepository
{
    ValueTask<List<int>> GetConfirmedGracePeriodOrdersAsync(
        TimeSpan gracePeriod,
        CancellationToken cancellationToken);
}

internal sealed class GracePeriodOrdersRepository(
    NpgsqlDataSource dataSource,
    ILogger<GracePeriodOrdersRepository> logger) : IGracePeriodOrdersRepository
{
    public async ValueTask<List<int>> GetConfirmedGracePeriodOrdersAsync(
        TimeSpan gracePeriod,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = dataSource.CreateConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id"
                FROM ordering.orders
                WHERE CURRENT_TIMESTAMP - "OrderDate" >= @GracePeriodTime AND "OrderStatus" = 'Submitted'
                """;
            command.Parameters.AddWithValue("GracePeriodTime", gracePeriod);

            List<int> ids = [];
            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }
        catch (NpgsqlException exception)
        {
            logger.LogError(exception, "Fatal error establishing database connection");
            return [];
        }
    }
}
