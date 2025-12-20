using System.Text.Json.Serialization;
using eShop.Basket.API.Model;

namespace eShop.Basket.API.Repositories;

public class RedisBasketRepository(ILogger<RedisBasketRepository> logger, IConnectionMultiplexer redis) : IBasketRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    // implementation:

    // - /basket/{id} "string" per unique basket
    private static RedisKey BasketKeyPrefix = "/basket/"u8.ToArray();
    // note on UTF8 here: library limitation (to be fixed) - prefixes are more efficient as blobs

    private static RedisKey GetBasketKey(string userId, string basketId) => BasketKeyPrefix.Append(userId).Append(basketId);
    public async Task<bool> DeleteBasketAsync(string userId, string basketId)
    {
        return await _database.KeyDeleteAsync(GetBasketKey(userId, basketId));
    }

    public async Task<CustomerBasket> GetBasketAsync(string userId, string basketId)
    {
        using var data = await _database.StringGetLeaseAsync(GetBasketKey(userId, basketId));

        if (data is null || data.Length == 0)
        {
            return null;
        }
        return JsonSerializer.Deserialize(data.Span, BasketSerializationContext.Default.CustomerBasket);
    }

    public async Task<CustomerBasket> UpdateBasketAsync(string userId, CustomerBasket basket)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(basket, BasketSerializationContext.Default.CustomerBasket);
        var created = await _database.StringSetAsync(GetBasketKey(userId, basket.Id), json);

        if (!created)
        {
            logger.LogInformation("Problem occurred persisting the item.");
            return null;
        }

        logger.LogInformation("Basket item persisted successfully.");
        return await GetBasketAsync(userId, basket.Id);
    }
}

[JsonSerializable(typeof(CustomerBasket))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class BasketSerializationContext : JsonSerializerContext
{

}
