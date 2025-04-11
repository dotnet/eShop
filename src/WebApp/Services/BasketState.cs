using System.Security.Claims;
using Inked.WebAppComponents.Catalog;
using Inked.WebAppComponents.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Inked.WebApp.Services;

public class BasketState(
    BasketService basketService,
    CatalogService catalogService,
    OrderingService orderingService,
    AuthenticationStateProvider authenticationStateProvider) : IBasketState
{
    private readonly HashSet<BasketStateChangedSubscription> _changeSubscriptions = new();
    private Task<IReadOnlyCollection<BasketItem>>? _cachedBasket;

    public async Task<IReadOnlyCollection<BasketItem>> GetBasketItemsAsync()
    {
        return (await GetUserAsync()).Identity?.IsAuthenticated == true
            ? await FetchBasketItemsAsync()
            : [];
    }

    public async Task AddAsync(CatalogItem item)
    {
        var items = (await FetchBasketItemsAsync()).Select(i => new BasketQuantity(i.ProductId, i.Quantity)).ToList();
        var found = false;
        for (var i = 0; i < items.Count; i++)
        {
            var existing = items[i];
            if (existing.ProductId == item.Id)
            {
                items[i] = existing with { Quantity = existing.Quantity + 1 };
                found = true;
                break;
            }
        }

        if (!found)
        {
            items.Add(new BasketQuantity(item.Id, 1));
        }

        _cachedBasket = null;
        await basketService.UpdateBasketAsync(items);
        await NotifyChangeSubscribersAsync();
    }

    public Task DeleteBasketAsync()
    {
        return basketService.DeleteBasketAsync();
    }

    public IDisposable NotifyOnChange(EventCallback callback)
    {
        var subscription = new BasketStateChangedSubscription(this, callback);
        _changeSubscriptions.Add(subscription);
        return subscription;
    }

    public async Task SetQuantityAsync(int productId, int quantity)
    {
        var existingItems = (await FetchBasketItemsAsync()).ToList();
        if (existingItems.FirstOrDefault(row => row.ProductId == productId) is { } row)
        {
            if (quantity > 0)
            {
                row.Quantity = quantity;
            }
            else
            {
                existingItems.Remove(row);
            }

            _cachedBasket = null;
            await basketService.UpdateBasketAsync(existingItems.Select(i => new BasketQuantity(i.ProductId, i.Quantity))
                .ToList());
            await NotifyChangeSubscribersAsync();
        }
    }

    public async Task CheckoutAsync(BasketCheckoutInfo checkoutInfo)
    {
        if (checkoutInfo.RequestId == default)
        {
            checkoutInfo.RequestId = Guid.NewGuid();
        }

        var buyerId = await authenticationStateProvider.GetBuyerIdAsync() ??
                      throw new InvalidOperationException("User does not have a buyer ID");
        var userName = await authenticationStateProvider.GetUserNameAsync() ??
                       throw new InvalidOperationException("User does not have a user name");

        // Get details for the items in the basket
        var orderItems = await FetchBasketItemsAsync();

        // Call into Ordering.API to create the order using those details
        var request = new CreateOrderRequest(
            buyerId,
            userName,
            checkoutInfo.City!,
            checkoutInfo.Street!,
            checkoutInfo.State!,
            checkoutInfo.Country!,
            checkoutInfo.ZipCode!,
            "1111222233334444",
            "TESTUSER",
            DateTime.UtcNow.AddYears(1),
            "111",
            checkoutInfo.CardTypeId,
            buyerId,
            [.. orderItems]);
        await orderingService.CreateOrder(request, checkoutInfo.RequestId);
        await DeleteBasketAsync();
    }

    private Task NotifyChangeSubscribersAsync()
    {
        return Task.WhenAll(_changeSubscriptions.Select(s => s.NotifyAsync()));
    }

    private async Task<ClaimsPrincipal> GetUserAsync()
    {
        return (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
    }

    private Task<IReadOnlyCollection<BasketItem>> FetchBasketItemsAsync()
    {
        return _cachedBasket ??= FetchCoreAsync();

        async Task<IReadOnlyCollection<BasketItem>> FetchCoreAsync()
        {
            var quantities = await basketService.GetBasketAsync();
            if (quantities.Count == 0)
            {
                return [];
            }

            // Get details for the items in the basket
            var basketItems = new List<BasketItem>();
            var productIds = quantities.Select(row => row.ProductId);
            var catalogItems = (await catalogService.GetCatalogItems(productIds)).ToDictionary(k => k.Id, v => v);
            foreach (var item in quantities)
            {
                var catalogItem = catalogItems[item.ProductId];
                var orderItem = new BasketItem
                {
                    Id = Guid.NewGuid().ToString(), // TODO: this value is meaningless, use ProductId instead.
                    ProductId = catalogItem.Id,
                    ProductName = catalogItem.Name,
                    UnitPrice = catalogItem.Price,
                    Quantity = item.Quantity
                };
                basketItems.Add(orderItem);
            }

            return basketItems;
        }
    }

    private class BasketStateChangedSubscription(BasketState Owner, EventCallback Callback) : IDisposable
    {
        public void Dispose()
        {
            Owner._changeSubscriptions.Remove(this);
        }

        public Task NotifyAsync()
        {
            return Callback.InvokeAsync();
        }
    }
}

public record CreateOrderRequest(
    string UserId,
    string UserName,
    string City,
    string Street,
    string State,
    string Country,
    string ZipCode,
    string CardNumber,
    string CardHolderName,
    DateTime CardExpiration,
    string CardSecurityNumber,
    int CardTypeId,
    string Buyer,
    List<BasketItem> Items);
