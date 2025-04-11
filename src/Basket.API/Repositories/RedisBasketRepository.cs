using System.Text.Json.Serialization;
using Inked.Basket.API.Model;

namespace Inked.Basket.API.Repositories;

public class RedisBasketRepository(ILogger<RedisBasketRepository> logger, IConnectionMultiplexer redis)
    : IBasketRepository
{
    // implementation:

    // - /basket/{id} "string" per unique basket
    private static readonly RedisKey BasketKeyPrefix = "/basket/"u8.ToArray();
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<bool> DeleteBasketAsync(string id)
    {
        return await _database.KeyDeleteAsync(GetBasketKey(id));
    }

    public async Task<CustomerBasket> GetBasketAsync(string customerId)
    {
        using var data = await _database.StringGetLeaseAsync(GetBasketKey(customerId));

        if (data is null || data.Length == 0)
        {
            return null;
        }

        return JsonSerializer.Deserialize(data.Span, BasketSerializationContext.Default.CustomerBasket);
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(basket, BasketSerializationContext.Default.CustomerBasket);
        var created = await _database.StringSetAsync(GetBasketKey(basket.BuyerId), json);

        if (!created)
        {
            logger.LogInformation("Problem occurred persisting the item.");
            return null;
        }


        logger.LogInformation("Basket item persisted successfully.");
        return await GetBasketAsync(basket.BuyerId);
    }
    // note on UTF8 here: library limitation (to be fixed) - prefixes are more efficient as blobs

    private static RedisKey GetBasketKey(string userId)
    {
        return BasketKeyPrefix.Append(userId);
    }
}

[JsonSerializable(typeof(CustomerBasket))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class BasketSerializationContext : JsonSerializerContext
{
}
