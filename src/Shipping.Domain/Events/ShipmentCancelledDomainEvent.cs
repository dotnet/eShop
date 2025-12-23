namespace eShop.Shipping.Domain.Events;

public class ShipmentCancelledDomainEvent : INotification
{
    public Shipment Shipment { get; }
    public int ReturnWarehouseId { get; }

    public ShipmentCancelledDomainEvent(Shipment shipment, int returnWarehouseId)
    {
        Shipment = shipment;
        ReturnWarehouseId = returnWarehouseId;
    }
}
