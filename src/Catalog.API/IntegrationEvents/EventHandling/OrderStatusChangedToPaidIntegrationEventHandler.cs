namespace eShop.Catalog.API.IntegrationEvents.EventHandling;

public class OrderStatusChangedToPaidIntegrationEventHandler(
    CatalogContext catalogContext,
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
        {
            // The execution strategy may run this more than once
            catalogContext.ChangeTracker.Clear();

            // The stock was already taken when the order was validated, the payment only makes it definitive
            var committed = await catalogContext.StockReservations
                .Where(sr => sr.OrderId == @event.OrderId && sr.Status == StockReservationStatus.Reserved)
                .ExecuteUpdateAsync(setters => setters.SetProperty(sr => sr.Status, StockReservationStatus.Committed));

            if (committed == 1 || await catalogContext.StockReservations.AnyAsync(sr => sr.OrderId == @event.OrderId))
            {
                return;
            }

            // No reservation at all: the order was validated before stock got reserved at validation time,
            // so its stock still has to be removed here
            var orderStockItems = @event.OrderStockItems
                .GroupBy(orderStockItem => orderStockItem.ProductId)
                .Select(group => new StockReservationItem { ProductId = group.Key, Units = group.Sum(orderStockItem => orderStockItem.Units) })
                .OrderBy(orderStockItem => orderStockItem.ProductId)
                .ToList();

            foreach (var orderStockItem in orderStockItems)
            {
                await catalogContext.CatalogItems
                    .Where(ci => ci.Id == orderStockItem.ProductId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(ci => ci.AvailableStock, ci => Math.Max(ci.AvailableStock - orderStockItem.Units, 0)));
            }

            catalogContext.StockReservations.Add(new StockReservation
            {
                OrderId = @event.OrderId,
                Status = StockReservationStatus.Committed,
                Items = orderStockItems
            });

            await catalogContext.SaveChangesAsync();
        });
    }
}
