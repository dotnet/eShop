namespace eShop.Shipping.API.Application.IntegrationEvents.EventHandling;

public class OrderStatusChangedToPaidIntegrationEventHandler
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShipperRepository _shipperRepository;
    private readonly ILogger<OrderStatusChangedToPaidIntegrationEventHandler> _logger;

    public OrderStatusChangedToPaidIntegrationEventHandler(
        IShipmentRepository shipmentRepository,
        IShipperRepository shipperRepository,
        ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger)
    {
        _shipmentRepository = shipmentRepository;
        _shipperRepository = shipperRepository;
        _logger = logger;
    }

    public async Task Handle(OrderStatusChangedToPaidIntegrationEvent @event)
    {
        _logger.LogInformation("Handling OrderStatusChangedToPaidIntegrationEvent for order {OrderId}", @event.OrderId);

        // Check if shipment already exists for this order
        var existingShipment = await _shipmentRepository.GetByOrderIdAsync(@event.OrderId);
        if (existingShipment != null)
        {
            _logger.LogWarning("Shipment already exists for order {OrderId}", @event.OrderId);
            return;
        }

        // Create shipment with customer address
        var customerAddress = $"{@event.Street}, {@event.ZipCode}";
        var shipment = new Shipment(
            @event.OrderId,
            customerAddress,
            @event.City,
            @event.Country
        );

        // Generate random route (1-3 warehouses for demo)
        var random = new Random();
        var warehouseCount = random.Next(1, 4); // 1 to 3 warehouses
        var warehouseIds = new List<int>();

        for (int i = 0; i < warehouseCount; i++)
        {
            var warehouseId = random.Next(1, 4); // Assuming warehouses 1-3 exist
            if (!warehouseIds.Contains(warehouseId))
            {
                warehouseIds.Add(warehouseId);
                shipment.AddWaypoint(warehouseId, $"Warehouse {warehouseId}", i);
            }
        }

        // Ensure at least one waypoint
        if (shipment.Waypoints.Count == 0)
        {
            shipment.AddWaypoint(1, "Warehouse 1", 0);
            warehouseIds.Add(1);
        }

        _shipmentRepository.Add(shipment);
        await _shipmentRepository.UnitOfWork.SaveEntitiesAsync();

        // Try to auto-assign nearest available shipper
        var firstWarehouseId = shipment.GetFirstWarehouseId();
        var availableShipper = await _shipperRepository.GetNearestAvailableAsync(firstWarehouseId);

        if (availableShipper != null)
        {
            shipment.AssignShipper(availableShipper.Id);
            availableShipper.SetBusy();
            _shipperRepository.Update(availableShipper);
            _shipmentRepository.Update(shipment);
            await _shipmentRepository.UnitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("Auto-assigned shipper {ShipperId} to shipment {ShipmentId}",
                availableShipper.Id, shipment.Id);
        }
        else
        {
            _logger.LogInformation("No available shipper for shipment {ShipmentId}. Awaiting manual assignment.",
                shipment.Id);
        }

        _logger.LogInformation("Created shipment {ShipmentId} for order {OrderId} with {WaypointCount} waypoints",
            shipment.Id, @event.OrderId, shipment.Waypoints.Count);
    }
}
