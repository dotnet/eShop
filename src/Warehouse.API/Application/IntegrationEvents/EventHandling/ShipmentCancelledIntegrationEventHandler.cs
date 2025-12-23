namespace eShop.Warehouse.API.Application.IntegrationEvents.EventHandling;

public class ShipmentCancelledIntegrationEventHandler(
    IWarehouseRepository warehouseRepository,
    ILogger<ShipmentCancelledIntegrationEventHandler> logger) :
    IIntegrationEventHandler<ShipmentCancelledIntegrationEvent>
{
    public async Task Handle(ShipmentCancelledIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var warehouse = await warehouseRepository.GetAsync(@event.ReturnWarehouseId);

        if (warehouse == null)
        {
            logger.LogWarning("Warehouse {WarehouseId} not found, cannot restore inventory for cancelled shipment {ShipmentId}",
                @event.ReturnWarehouseId, @event.ShipmentId);
            return;
        }

        foreach (var item in @event.OrderItems)
        {
            logger.LogInformation("Restoring {Units} units of product {ProductId} to warehouse {WarehouseId}",
                item.Units, item.ProductId, @event.ReturnWarehouseId);

            warehouse.AddInventory(item.ProductId, item.Units);
        }

        warehouseRepository.Update(warehouse);
        await warehouseRepository.UnitOfWork.SaveEntitiesAsync();

        logger.LogInformation("Inventory restored for cancelled shipment {ShipmentId} to warehouse {WarehouseId}",
            @event.ShipmentId, @event.ReturnWarehouseId);
    }
}
