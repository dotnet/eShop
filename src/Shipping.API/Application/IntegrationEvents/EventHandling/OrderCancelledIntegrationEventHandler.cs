namespace eShop.Shipping.API.Application.IntegrationEvents.EventHandling;

public class OrderCancelledIntegrationEventHandler
    : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipperRepository _shipperRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderCancelledIntegrationEventHandler> _logger;

    public OrderCancelledIntegrationEventHandler(
        IShipmentRepository shipmentRepository,
        IShipperRepository shipperRepository,
        IEventBus eventBus,
        ILogger<OrderCancelledIntegrationEventHandler> logger)
    {
        _shipmentRepository = shipmentRepository;
        _shipperRepository = shipperRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledIntegrationEvent @event)
    {
        _logger.LogInformation("Handling OrderCancelledIntegrationEvent for order {OrderId}", @event.OrderId);

        var shipment = await _shipmentRepository.GetByOrderIdAsync(@event.OrderId);
        if (shipment == null)
        {
            _logger.LogWarning("No shipment found for order {OrderId}", @event.OrderId);
            return;
        }

        if (shipment.Status == ShipmentStatus.Delivered)
        {
            _logger.LogWarning("Cannot cancel delivered shipment for order {OrderId}", @event.OrderId);
            return;
        }

        // Get the return warehouse before cancelling
        var returnWarehouseId = shipment.GetLastWarehouseId();

        // Cancel the shipment
        shipment.Cancel(returnWarehouseId);

        // Free up the shipper if assigned
        if (shipment.ShipperId.HasValue)
        {
            var shipper = await _shipperRepository.GetAsync(shipment.ShipperId.Value);
            if (shipper != null)
            {
                shipper.SetAvailable();
                shipper.AssignToWarehouse(returnWarehouseId);
                _shipperRepository.Update(shipper);
            }
        }

        _shipmentRepository.Update(shipment);
        await _shipmentRepository.UnitOfWork.SaveEntitiesAsync();

        // Publish event to notify Warehouse to restore inventory
        var shipmentCancelledEvent = new ShipmentCancelledIntegrationEvent
        {
            ShipmentId = shipment.Id,
            OrderId = shipment.OrderId,
            ReturnWarehouseId = returnWarehouseId,
            OrderItems = @event.OrderStockItems
        };

        await _eventBus.PublishAsync(shipmentCancelledEvent);

        _logger.LogInformation("Cancelled shipment {ShipmentId} for order {OrderId}. Items to be returned to warehouse {WarehouseId}",
            shipment.Id, @event.OrderId, returnWarehouseId);
    }
}
