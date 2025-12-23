namespace eShop.Shipping.Domain.AggregatesModel.ShipmentAggregate;

public class Shipment : Entity, IAggregateRoot
{
    public int OrderId { get; private set; }
    public int? ShipperId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string CustomerAddress { get; private set; } = string.Empty;
    public string CustomerCity { get; private set; } = string.Empty;
    public string CustomerCountry { get; private set; } = string.Empty;

    private readonly List<ShipmentWaypoint> _waypoints = [];
    public IReadOnlyCollection<ShipmentWaypoint> Waypoints => _waypoints.AsReadOnly();

    private readonly List<ShipmentStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<ShipmentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    protected Shipment() { }

    public Shipment(int orderId, string customerAddress, string customerCity, string customerCountry)
    {
        OrderId = orderId;
        CustomerAddress = customerAddress;
        CustomerCity = customerCity;
        CustomerCountry = customerCountry;
        Status = ShipmentStatus.Created;
        CreatedAt = DateTime.UtcNow;

        AddStatusHistory(ShipmentStatus.Created, notes: "Shipment created");
        AddDomainEvent(new ShipmentCreatedDomainEvent(this));
    }

    public void AddWaypoint(int warehouseId, string warehouseName, int sequence)
    {
        if (Status != ShipmentStatus.Created)
            throw new ShippingDomainException("Cannot add waypoints after shipment has started");

        var waypoint = new ShipmentWaypoint(warehouseId, warehouseName, sequence);
        _waypoints.Add(waypoint);
    }

    public void AssignShipper(int shipperId)
    {
        if (Status != ShipmentStatus.Created && Status != ShipmentStatus.ShipperAssigned)
            throw new ShippingDomainException($"Cannot assign shipper when status is {Status}");

        ShipperId = shipperId;
        Status = ShipmentStatus.ShipperAssigned;
        AddStatusHistory(ShipmentStatus.ShipperAssigned, notes: $"Shipper {shipperId} assigned");
        AddDomainEvent(new ShipperAssignedDomainEvent(this, shipperId));
    }

    public void PickupFromWarehouse(int waypointId)
    {
        if (Status != ShipmentStatus.ShipperAssigned && Status != ShipmentStatus.ArrivedAtWarehouse)
            throw new ShippingDomainException($"Cannot pickup when status is {Status}");

        var waypoint = _waypoints.FirstOrDefault(w => w.Id == waypointId)
            ?? throw new ShippingDomainException($"Waypoint {waypointId} not found");

        if (waypoint.Sequence == 0)
        {
            waypoint.MarkArrived();
        }
        waypoint.MarkDeparted();

        Status = ShipmentStatus.PickedUpFromWarehouse;
        AddStatusHistory(ShipmentStatus.PickedUpFromWarehouse, waypointId, $"Picked up from {waypoint.WarehouseName}");
        AddDomainEvent(new ShipmentStatusChangedDomainEvent(this));
    }

    public void StartTransitToWarehouse()
    {
        if (Status != ShipmentStatus.PickedUpFromWarehouse)
            throw new ShippingDomainException($"Cannot start transit when status is {Status}");

        Status = ShipmentStatus.InTransitToWarehouse;
        AddStatusHistory(ShipmentStatus.InTransitToWarehouse, notes: "In transit to next warehouse");
        AddDomainEvent(new ShipmentStatusChangedDomainEvent(this));
    }

    public void ArriveAtWarehouse(int waypointId)
    {
        if (Status != ShipmentStatus.InTransitToWarehouse && Status != ShipmentStatus.PickedUpFromWarehouse)
            throw new ShippingDomainException($"Cannot arrive at warehouse when status is {Status}");

        var waypoint = _waypoints.FirstOrDefault(w => w.Id == waypointId)
            ?? throw new ShippingDomainException($"Waypoint {waypointId} not found");

        waypoint.MarkArrived();

        Status = ShipmentStatus.ArrivedAtWarehouse;
        AddStatusHistory(ShipmentStatus.ArrivedAtWarehouse, waypointId, $"Arrived at {waypoint.WarehouseName}");
        AddDomainEvent(new ShipmentStatusChangedDomainEvent(this));
    }

    public void DepartFromWarehouse(int waypointId)
    {
        if (Status != ShipmentStatus.ArrivedAtWarehouse)
            throw new ShippingDomainException($"Cannot depart when status is {Status}");

        var waypoint = _waypoints.FirstOrDefault(w => w.Id == waypointId)
            ?? throw new ShippingDomainException($"Waypoint {waypointId} not found");

        waypoint.MarkDeparted();

        // Check if this is the last waypoint
        var nextWaypoint = _waypoints.OrderBy(w => w.Sequence).FirstOrDefault(w => !w.IsCompleted);
        if (nextWaypoint == null)
        {
            Status = ShipmentStatus.DeliveringToCustomer;
            AddStatusHistory(ShipmentStatus.DeliveringToCustomer, notes: "Delivering to customer");
        }
        else
        {
            Status = ShipmentStatus.InTransitToWarehouse;
            AddStatusHistory(ShipmentStatus.InTransitToWarehouse, notes: $"In transit to {nextWaypoint.WarehouseName}");
        }
        AddDomainEvent(new ShipmentStatusChangedDomainEvent(this));
    }

    public void StartDeliveryToCustomer()
    {
        var allWaypointsCompleted = _waypoints.All(w => w.IsCompleted);
        if (!allWaypointsCompleted)
            throw new ShippingDomainException("All waypoints must be completed before delivering to customer");

        Status = ShipmentStatus.DeliveringToCustomer;
        AddStatusHistory(ShipmentStatus.DeliveringToCustomer, notes: "Delivering to customer");
        AddDomainEvent(new ShipmentStatusChangedDomainEvent(this));
    }

    public void MarkDelivered()
    {
        if (Status != ShipmentStatus.DeliveringToCustomer)
            throw new ShippingDomainException($"Cannot mark delivered when status is {Status}");

        Status = ShipmentStatus.Delivered;
        CompletedAt = DateTime.UtcNow;
        AddStatusHistory(ShipmentStatus.Delivered, notes: "Customer received the package");
        AddDomainEvent(new ShipmentCompletedDomainEvent(this));
    }

    public void Cancel(int? returnWarehouseId = null)
    {
        if (Status == ShipmentStatus.Delivered)
            throw new ShippingDomainException("Cannot cancel a delivered shipment");

        Status = ShipmentStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        AddStatusHistory(ShipmentStatus.Cancelled, notes: "Shipment cancelled");
        AddDomainEvent(new ShipmentCancelledDomainEvent(this, returnWarehouseId ?? GetLastWarehouseId()));
    }

    public void ReturnToWarehouse(int warehouseId)
    {
        if (Status != ShipmentStatus.Cancelled)
            throw new ShippingDomainException($"Cannot return to warehouse when status is {Status}");

        Status = ShipmentStatus.ReturnedToWarehouse;
        AddStatusHistory(ShipmentStatus.ReturnedToWarehouse, notes: $"Returned to warehouse {warehouseId}");
    }

    public int GetLastWarehouseId()
    {
        var lastCompletedWaypoint = _waypoints
            .Where(w => w.ArrivedAt.HasValue)
            .OrderByDescending(w => w.Sequence)
            .FirstOrDefault();

        return lastCompletedWaypoint?.WarehouseId ?? _waypoints.OrderBy(w => w.Sequence).First().WarehouseId;
    }

    public int GetFirstWarehouseId()
    {
        return _waypoints.OrderBy(w => w.Sequence).First().WarehouseId;
    }

    public ShipmentWaypoint? GetCurrentWaypoint()
    {
        return _waypoints
            .OrderBy(w => w.Sequence)
            .FirstOrDefault(w => !w.IsCompleted);
    }

    public ShipmentWaypoint? GetNextWaypoint()
    {
        var current = GetCurrentWaypoint();
        if (current == null) return null;

        return _waypoints
            .OrderBy(w => w.Sequence)
            .FirstOrDefault(w => w.Sequence > current.Sequence);
    }

    private void AddStatusHistory(ShipmentStatus status, int? waypointId = null, string? notes = null)
    {
        var history = new ShipmentStatusHistory(status, waypointId, notes);
        _statusHistory.Add(history);
    }
}
