using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using eShop.WebAppComponents.Catalog;
using eShop.WebAppComponents.Services;
using System.Text;
using System.Text.Json;
namespace eShop.WebApp.Services;

public class BasketState(
    BasketService basketService,
    CatalogService catalogService,
    OrderingService orderingService,
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor httpContextAccessor)
{
    private Task<IReadOnlyCollection<BasketItem>>? _cachedBasket;
    private HashSet<BasketStateChangedSubscription> _changeSubscriptions = new();
    Dictionary<int,int>? ProductIdToQuantity = new();
    private readonly ISession session = httpContextAccessor.HttpContext!.Session;
    public Task DeleteBasketAsync()
        => basketService.DeleteBasketAsync();

    public async Task<IReadOnlyCollection<BasketItem>> GetBasketItemsAsync()
    {
        if ((await GetUserAsync()).Identity?.IsAuthenticated == true)
        {
            await MoveItemFromSessionToRedis();

            return await FetchBasketItemsAsync();
        }
        return [];
    }
    public async Task<IReadOnlyCollection<BasketItem>> GetBasketItemsAsAnonymous()
        => (await GetUserAsync()).Identity?.IsAuthenticated == false
        ? await GetBasketItemsAsAnonymousAsync()
        : [];
    public IDisposable NotifyOnChange(EventCallback callback)
    {
        var subscription = new BasketStateChangedSubscription(this, callback);
        _changeSubscriptions.Add(subscription);
        return subscription;
    }

    public async Task AddAsAnonymousUser(CatalogItem item)
    {
        // Retrieve existing cart items from session
        if (session.TryGetValue("ShoppingCart", out var cartData))
        {
            ProductIdToQuantity = JsonSerializer.Deserialize<Dictionary<int, int>>(Encoding.UTF8.GetString(cartData));
        }
        if (!ProductIdToQuantity!.ContainsKey(item.Id))
        {
            ProductIdToQuantity[item.Id] = 1;
        }
        else
        {
            ProductIdToQuantity[item.Id]++;
        }
        session.SetString("ShoppingCart", JsonSerializer.Serialize(ProductIdToQuantity));
        await NotifyChangeSubscribersAsync();
    }

    public async Task AddAsync(CatalogItem item)
    {
        var items = (await FetchBasketItemsAsync()).Select(i => new BasketQuantity(i.ProductId, i.Quantity)).ToList();
        bool found = false;
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
            await basketService.UpdateBasketAsync(existingItems.Select(i => new BasketQuantity(i.ProductId, i.Quantity)).ToList());
            await NotifyChangeSubscribersAsync();
        }
    }

    public async Task CheckoutAsync(BasketCheckoutInfo checkoutInfo)
    {
        if (checkoutInfo.RequestId == default)
        {
            checkoutInfo.RequestId = Guid.NewGuid();
        }

        var buyerId = await authenticationStateProvider.GetBuyerIdAsync() ?? throw new InvalidOperationException("User does not have a buyer ID");
        var userName = await authenticationStateProvider.GetUserNameAsync() ?? throw new InvalidOperationException("User does not have a user name");

        // Get details for the items in the basket
        var orderItems = await FetchBasketItemsAsync();

        // Call into Ordering.API to create the order using those details
        var request = new CreateOrderRequest(
            UserId: buyerId,
            UserName: userName,
            City: checkoutInfo.City!,
            Street: checkoutInfo.Street!,
            State: checkoutInfo.State!,
            Country: checkoutInfo.Country!,
            ZipCode: checkoutInfo.ZipCode!,
            CardNumber: checkoutInfo.CardNumber!,
            CardHolderName: checkoutInfo.CardHolderName!,
            CardExpiration: checkoutInfo.CardExpiration!.Value,
            CardSecurityNumber: checkoutInfo.CardSecurityNumber!,
            CardTypeId: checkoutInfo.CardTypeId,
            Buyer: buyerId,
            Items: [.. orderItems]);
        await orderingService.CreateOrder(request, checkoutInfo.RequestId);
        await DeleteBasketAsync();
    }

    private Task NotifyChangeSubscribersAsync()
        => Task.WhenAll(_changeSubscriptions.Select(s => s.NotifyAsync()));

    private async Task<ClaimsPrincipal> GetUserAsync()
        => (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
    private Task<IReadOnlyCollection<BasketItem>> GetBasketItemsAsAnonymousAsync()
    {
        return _cachedBasket = FetchCoreAsync();
        async Task<IReadOnlyCollection<BasketItem>> FetchCoreAsync()
        {
            if (session.TryGetValue("ShoppingCart", out var cartData))
            {
                ProductIdToQuantity = JsonSerializer.Deserialize<Dictionary<int, int>>(Encoding.UTF8.GetString(cartData));
            }
            if (ProductIdToQuantity?.Count == 0)
            {
                return [];
            }

            // Get details for the items in the basket
            var basketItems = new List<BasketItem>();
            var productIds = ProductIdToQuantity != null ? ProductIdToQuantity.Select(row => row.Key) : new List<int>();
            var catalogItems = (await catalogService.GetCatalogItems(productIds)).ToDictionary(k => k.Id, v => v);
            foreach (var item in ProductIdToQuantity!)
            {
                var catalogItem = catalogItems[item.Key];
                var orderItem = new BasketItem
                {
                    Id = Guid.NewGuid().ToString(), // TODO: this value is meaningless, use ProductId instead.
                    ProductId = catalogItem.Id,
                    ProductName = catalogItem.Name,
                    UnitPrice = catalogItem.Price,
                    Quantity = item.Value,
                };
                basketItems.Add(orderItem);
            }
            return basketItems;
        }
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
                    Quantity = item.Quantity,
                };
                basketItems.Add(orderItem);
            }

            return basketItems;
        }
    }

    private async Task MoveItemFromSessionToRedis()
    {
        //User just logged in, let's empty session cart and put in Redis
        if (session.TryGetValue("ShoppingCart", out var cartData))
        {
            ProductIdToQuantity = JsonSerializer.Deserialize<Dictionary<int, int>>(Encoding.UTF8.GetString(cartData));

            if (ProductIdToQuantity?.Count != 0)
            {
                var basketQuantity = basketService.MapToBasket(ProductIdToQuantity!);
                await basketService.UpdateBasketAsync(basketQuantity);
            }
            session.Remove("ShoppingCart");
        }
    }

    private class BasketStateChangedSubscription(BasketState Owner, EventCallback Callback) : IDisposable
    {
        public Task NotifyAsync() => Callback.InvokeAsync();
        public void Dispose() => Owner._changeSubscriptions.Remove(this);
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
