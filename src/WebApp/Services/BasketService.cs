using Inked.Basket.API.Grpc;

namespace Inked.WebApp.Services;

public class BasketService(Basket.API.Grpc.Basket.BasketClient basketClient)
{
    public async Task<IReadOnlyCollection<BasketQuantity>> GetBasketAsync()
    {
        var result = await basketClient.GetBasketAsync(new GetBasketRequest());
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
            var updateItem = new Basket.API.Grpc.BasketItem { ProductId = item.ProductId, Quantity = item.Quantity };
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
}

public record BasketQuantity(int ProductId, int Quantity);
