namespace eShop.Catalog.API.IntegrationEvents.EventHandling;

public class OrderStatusChangedToCancelledIntegrationEventHandler(
    CatalogContext catalogContext,
    ILogger<OrderStatusChangedToCancelledIntegrationEventHandler> logger) :
    IIntegrationEventHandler<OrderStatusChangedToCancelledIntegrationEvent>
{
    public async Task Handle(OrderStatusChangedToCancelledIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
        {
            // The execution strategy may run this more than once
            catalogContext.ChangeTracker.Clear();

            // Only one caller can move the reservation out of Reserved, so the stock can't be given back twice
            var released = await catalogContext.StockReservations
                .Where(sr => sr.OrderId == @event.OrderId && sr.Status == StockReservationStatus.Reserved)
                .ExecuteUpdateAsync(setters => setters.SetProperty(sr => sr.Status, StockReservationStatus.Released));

            if (released == 1)
            {
                var reservation = await catalogContext.StockReservations
                    .AsNoTracking()
                    .SingleAsync(sr => sr.OrderId == @event.OrderId);

                foreach (var reservedItem in reservation.Items.OrderBy(item => item.ProductId))
                {
                    await catalogContext.CatalogItems
                        .Where(ci => ci.Id == reservedItem.ProductId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(ci => ci.AvailableStock, ci => ci.AvailableStock + reservedItem.Units));
                }
            }
            else if (!await catalogContext.StockReservations.AnyAsync(sr => sr.OrderId == @event.OrderId))
            {
                // The order was cancelled before its stock was validated.
                // Remember it, so that a validation request arriving late doesn't hold stock for a dead order.
                catalogContext.StockReservations.Add(new StockReservation
                {
                    OrderId = @event.OrderId,
                    Status = StockReservationStatus.Released
                });

                await catalogContext.SaveChangesAsync();
            }
        });
    }
}
