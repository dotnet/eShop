using eShop.Basket.API.Grpc;
using GrpcBasketItem = eShop.Basket.API.Grpc.BasketItem;
using GrpcBasketClient = eShop.Basket.API.Grpc.Basket.BasketClient;

namespace eShop.WebApp.Services;

public class BasketService(GrpcBasketClient basketClient)
{
    public async Task<IReadOnlyCollection<BasketQuantity>> GetBasketAsync()
    {
        var result = await basketClient.GetBasketAsync(new ());
        return MapToBasket(result);
    }

    public async Task DeleteBasketAsync()
    {
        await basketClient.DeleteBasketAsync(new DeleteBasketRequest());
    }

    public async Task UpdateBasketAsync(IReadOnlyCollection<BasketQuantity> basket)
    {
        var updatePayload = new UpdateBasketRequest();

        foreach (var item in basket)
        {
            var updateItem = new GrpcBasketItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            };
            updatePayload.Items.Add(updateItem);
        }

        await basketClient.UpdateBasketAsync(updatePayload);
    }

    private static List<BasketQuantity> MapToBasket(CustomerBasketResponse response)
    {
        var result = new List<BasketQuantity>();
        foreach (var item in response.Items)
        {
            result.Add(new BasketQuantity(item.ProductId, item.Quantity));
        }

        return result;
    }

    public List<BasketQuantity> MapToBasket(Dictionary<int,int> productIdToQuantity)
    {
        var result = new List<BasketQuantity>();
        foreach (var item in productIdToQuantity)
        {
            result.Add(new BasketQuantity(item.Key, item.Value));
        }

        return result;
    }
}

public record BasketQuantity(int ProductId, int Quantity);
