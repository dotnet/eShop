namespace eShop.Shipping.Domain.AggregatesModel.ShipmentAggregate;

public class ShipmentStatusHistory : Entity
{
    public int ShipmentId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int? WaypointId { get; private set; }
    public string? Notes { get; private set; }

    protected ShipmentStatusHistory() { }

    public ShipmentStatusHistory(ShipmentStatus status, int? waypointId = null, string? notes = null)
    {
        Status = status;
        Timestamp = DateTime.UtcNow;
        WaypointId = waypointId;
        Notes = notes;
    }

    internal void SetShipmentId(int shipmentId)
    {
        ShipmentId = shipmentId;
    }
}
