namespace eShop.Shipping.Domain.Events;

public class ShipperAssignedDomainEvent : INotification
{
    public Shipment Shipment { get; }
    public int ShipperId { get; }

    public ShipperAssignedDomainEvent(Shipment shipment, int shipperId)
    {
        Shipment = shipment;
        ShipperId = shipperId;
    }
}
