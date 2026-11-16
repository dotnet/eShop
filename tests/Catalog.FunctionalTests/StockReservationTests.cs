using System.Collections.Concurrent;
using System.Threading;
using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.IntegrationEvents;
using eShop.Catalog.API.IntegrationEvents.EventHandling;
using eShop.Catalog.API.IntegrationEvents.Events;
using eShop.Catalog.API.Model;
using eShop.EventBus.Events;
using eShop.IntegrationEventLogEF.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace eShop.Catalog.FunctionalTests;

[Collection(CatalogApiFixture.CollectionName)]
public sealed class StockReservationTests : IClassFixture<CatalogApiFixture>
{
    private static int _lastOrderId;

    private readonly CatalogApiFixture _fixture;
    private readonly RecordingIntegrationEventService _integrationEvents = new();

    public StockReservationTests(CatalogApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentOrdersNeverGetMoreThanTheAvailableStock()
    {
        var productId = await CreateCatalogItemAsync(availableStock: 5);
        var orderIds = Enumerable.Range(0, 20).Select(_ => NextOrderId()).ToList();

        // Act
        await Task.WhenAll(orderIds.Select(orderId => Task.Run(() => ValidateAsync(orderId, new OrderStockItem(productId, 1)))));

        // Assert
        Assert.Equal(5, _integrationEvents.Published.OfType<OrderStockConfirmedIntegrationEvent>().Count());
        Assert.Equal(15, _integrationEvents.Published.OfType<OrderStockRejectedIntegrationEvent>().Count());
        Assert.Equal(0, await GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task RejectedOrderDoesNotKeepStockOfItsOtherItems()
    {
        var availableProductId = await CreateCatalogItemAsync(availableStock: 10);
        var soldOutProductId = await CreateCatalogItemAsync(availableStock: 1);
        var orderId = NextOrderId();

        // Act
        await ValidateAsync(orderId, new OrderStockItem(availableProductId, 2), new OrderStockItem(soldOutProductId, 2));

        // Assert
        var rejected = Assert.IsType<OrderStockRejectedIntegrationEvent>(Assert.Single(_integrationEvents.Published));
        Assert.Equal(orderId, rejected.OrderId);
        Assert.True(rejected.OrderStockItems.Single(i => i.ProductId == availableProductId).HasStock);
        Assert.False(rejected.OrderStockItems.Single(i => i.ProductId == soldOutProductId).HasStock);
        Assert.Equal(10, await GetAvailableStockAsync(availableProductId));
        Assert.Equal(1, await GetAvailableStockAsync(soldOutProductId));
    }

    [Fact]
    public async Task OrderForUnknownProductIsRejected()
    {
        var orderId = NextOrderId();

        // Act
        await ValidateAsync(orderId, new OrderStockItem(int.MaxValue, 1));

        // Assert
        Assert.IsType<OrderStockRejectedIntegrationEvent>(Assert.Single(_integrationEvents.Published));
    }

    [Fact]
    public async Task ValidatingTheSameOrderTwiceReservesStockOnce()
    {
        var productId = await CreateCatalogItemAsync(availableStock: 10);
        var orderId = NextOrderId();

        // Act
        await ValidateAsync(orderId, new OrderStockItem(productId, 3));
        await ValidateAsync(orderId, new OrderStockItem(productId, 3));

        // Assert
        Assert.Equal(2, _integrationEvents.Published.OfType<OrderStockConfirmedIntegrationEvent>().Count());
        Assert.Equal(7, await GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task CancelledOrderGivesItsStockBackOnce()
    {
        var productId = await CreateCatalogItemAsync(availableStock: 10);
        var orderId = NextOrderId();
        await ValidateAsync(orderId, new OrderStockItem(productId, 3));
        Assert.Equal(7, await GetAvailableStockAsync(productId));

        // Act
        await CancelAsync(orderId);
        await CancelAsync(orderId);

        // Assert
        Assert.Equal(10, await GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task PaidOrderKeepsItsStock()
    {
        var productId = await CreateCatalogItemAsync(availableStock: 10);
        var orderId = NextOrderId();
        await ValidateAsync(orderId, new OrderStockItem(productId, 3));

        // Act
        await PayAsync(orderId, new OrderStockItem(productId, 3));
        await PayAsync(orderId, new OrderStockItem(productId, 3));
        await CancelAsync(orderId);

        // Assert
        Assert.Equal(7, await GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task OrderCancelledBeforeValidationDoesNotReserveStock()
    {
        var productId = await CreateCatalogItemAsync(availableStock: 10);
        var orderId = NextOrderId();
        await CancelAsync(orderId);

        // Act
        await ValidateAsync(orderId, new OrderStockItem(productId, 3));

        // Assert
        Assert.Empty(_integrationEvents.Published);
        Assert.Equal(10, await GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task PaidOrderWithoutReservationRemovesStockOnce()
    {
        var productId = await CreateCatalogItemAsync(availableStock: 10);
        var orderId = NextOrderId();

        // Act
        await PayAsync(orderId, new OrderStockItem(productId, 3));
        await PayAsync(orderId, new OrderStockItem(productId, 3));

        // Assert
        Assert.Equal(7, await GetAvailableStockAsync(productId));
    }

    private static int NextOrderId() => Interlocked.Increment(ref _lastOrderId);

    private async Task ValidateAsync(int orderId, params OrderStockItem[] orderStockItems)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var handler = new OrderStatusChangedToAwaitingValidationIntegrationEventHandler(
            scope.ServiceProvider.GetRequiredService<CatalogContext>(),
            scope.ServiceProvider.GetRequiredService<IIntegrationEventLogService>(),
            _integrationEvents,
            NullLogger<OrderStatusChangedToAwaitingValidationIntegrationEventHandler>.Instance);

        await handler.Handle(new OrderStatusChangedToAwaitingValidationIntegrationEvent(orderId, orderStockItems));
    }

    private async Task PayAsync(int orderId, params OrderStockItem[] orderStockItems)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var handler = new OrderStatusChangedToPaidIntegrationEventHandler(
            scope.ServiceProvider.GetRequiredService<CatalogContext>(),
            NullLogger<OrderStatusChangedToPaidIntegrationEventHandler>.Instance);

        await handler.Handle(new OrderStatusChangedToPaidIntegrationEvent(orderId, orderStockItems));
    }

    private async Task CancelAsync(int orderId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var handler = new OrderStatusChangedToCancelledIntegrationEventHandler(
            scope.ServiceProvider.GetRequiredService<CatalogContext>(),
            NullLogger<OrderStatusChangedToCancelledIntegrationEventHandler>.Instance);

        await handler.Handle(new OrderStatusChangedToCancelledIntegrationEvent(orderId));
    }

    private async Task<int> CreateCatalogItemAsync(int availableStock)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        var catalogItem = new CatalogItem("Stock reservation test item")
        {
            // The seeded items come with their ids, the database doesn't generate ones that are free
            Id = await catalogContext.CatalogItems.MaxAsync(ci => ci.Id) + 1,
            CatalogBrandId = await catalogContext.CatalogBrands.Select(b => b.Id).FirstAsync(),
            CatalogTypeId = await catalogContext.CatalogTypes.Select(t => t.Id).FirstAsync(),
            Price = 10,
            AvailableStock = availableStock,
            MaxStockThreshold = 100
        };

        catalogContext.CatalogItems.Add(catalogItem);
        await catalogContext.SaveChangesAsync();

        return catalogItem.Id;
    }

    private async Task<int> GetAvailableStockAsync(int productId)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        return await catalogContext.CatalogItems
            .Where(ci => ci.Id == productId)
            .Select(ci => ci.AvailableStock)
            .SingleAsync();
    }

    private sealed class RecordingIntegrationEventService : ICatalogIntegrationEventService
    {
        public ConcurrentQueue<IntegrationEvent> Published { get; } = new();

        public Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt) => throw new NotSupportedException();

        public Task PublishThroughEventBusAsync(IntegrationEvent evt)
        {
            Published.Enqueue(evt);
            return Task.CompletedTask;
        }
    }
}
