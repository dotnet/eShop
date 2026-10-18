namespace eShop.Catalog.API.IntegrationEvents.EventHandling;

public class OrderStatusChangedToAwaitingValidationIntegrationEventHandler(
    CatalogContext catalogContext,
    IIntegrationEventLogService integrationEventLogService,
    ICatalogIntegrationEventService catalogIntegrationEventService,
    ILogger<OrderStatusChangedToAwaitingValidationIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderStatusChangedToAwaitingValidationIntegrationEvent>
{
    private const string BeforeReservationSavepoint = "BeforeStockReservation";

    public async Task Handle(OrderStatusChangedToAwaitingValidationIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        // Always touch the products in the same order, so that two orders for the same products can't deadlock each other
        var orderStockItems = @event.OrderStockItems
            .GroupBy(orderStockItem => orderStockItem.ProductId)
            .Select(group => new OrderStockItem(group.Key, group.Sum(orderStockItem => orderStockItem.Units)))
            .OrderBy(orderStockItem => orderStockItem.ProductId)
            .ToList();

        IntegrationEvent? confirmedIntegrationEvent = null;

        // The stock, the reservation and the outgoing event are saved in a single local transaction
        await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
        {
            // The execution strategy may run this more than once
            catalogContext.ChangeTracker.Clear();

            confirmedIntegrationEvent = await ReserveStockAsync(@event.OrderId, orderStockItems);

            if (confirmedIntegrationEvent is not null)
            {
                await integrationEventLogService.SaveEventAsync(confirmedIntegrationEvent, catalogContext.Database.CurrentTransaction!);
            }
        });

        if (confirmedIntegrationEvent is not null)
        {
            await catalogIntegrationEventService.PublishThroughEventBusAsync(confirmedIntegrationEvent);
        }
    }

    private async Task<IntegrationEvent?> ReserveStockAsync(int orderId, List<OrderStockItem> orderStockItems)
    {
        var existingReservation = await catalogContext.StockReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(sr => sr.OrderId == orderId);

        if (existingReservation is not null)
        {
            // The event was already handled, the stock must not be taken twice.
            // A released reservation means the order was cancelled in the meantime, so there is nobody left to answer to.
            return existingReservation.Status == StockReservationStatus.Released
                ? null
                : new OrderStockConfirmedIntegrationEvent(orderId);
        }

        var transaction = catalogContext.Database.CurrentTransaction!;
        await transaction.CreateSavepointAsync(BeforeReservationSavepoint);

        var confirmedOrderStockItems = new List<ConfirmedOrderStockItem>();

        foreach (var orderStockItem in orderStockItems)
        {
            // Check and decrement in one statement, the database decides who gets the last units.
            // The row stays locked until the transaction ends, a product that doesn't exist has no stock.
            var updated = await catalogContext.CatalogItems
                .Where(ci => ci.Id == orderStockItem.ProductId && ci.AvailableStock >= orderStockItem.Units)
                .ExecuteUpdateAsync(setters => setters.SetProperty(ci => ci.AvailableStock, ci => ci.AvailableStock - orderStockItem.Units));

            confirmedOrderStockItems.Add(new ConfirmedOrderStockItem(orderStockItem.ProductId, updated == 1));
        }

        if (confirmedOrderStockItems.Any(c => !c.HasStock))
        {
            // All or nothing, give back what was taken for the other items of the order
            await transaction.RollbackToSavepointAsync(BeforeReservationSavepoint);

            return new OrderStockRejectedIntegrationEvent(orderId, confirmedOrderStockItems);
        }

        catalogContext.StockReservations.Add(new StockReservation
        {
            OrderId = orderId,
            Status = StockReservationStatus.Reserved,
            Items = orderStockItems.ConvertAll(orderStockItem => new StockReservationItem { ProductId = orderStockItem.ProductId, Units = orderStockItem.Units })
        });

        await catalogContext.SaveChangesAsync();

        return new OrderStockConfirmedIntegrationEvent(orderId);
    }
}
