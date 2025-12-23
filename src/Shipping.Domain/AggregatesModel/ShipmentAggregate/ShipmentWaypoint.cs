namespace eShop.Shipping.Domain.AggregatesModel.ShipmentAggregate;

public class ShipmentWaypoint : Entity
{
    public int ShipmentId { get; private set; }
    public int WarehouseId { get; private set; }
    public string WarehouseName { get; private set; } = string.Empty;
    public int Sequence { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public DateTime? DepartedAt { get; private set; }

    public bool IsCompleted => DepartedAt.HasValue;

    protected ShipmentWaypoint() { }

    public ShipmentWaypoint(int warehouseId, string warehouseName, int sequence)
    {
        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
        Sequence = sequence;
    }

    public void MarkArrived()
    {
        if (ArrivedAt.HasValue)
            throw new ShippingDomainException("Already arrived at this waypoint");

        ArrivedAt = DateTime.UtcNow;
    }

    public void MarkDeparted()
    {
        if (!ArrivedAt.HasValue)
            throw new ShippingDomainException("Must arrive before departing");

        if (DepartedAt.HasValue)
            throw new ShippingDomainException("Already departed from this waypoint");

        DepartedAt = DateTime.UtcNow;
    }

    internal void SetShipmentId(int shipmentId)
    {
        ShipmentId = shipmentId;
    }
}
