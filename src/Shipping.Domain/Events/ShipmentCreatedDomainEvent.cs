namespace eShop.Shipping.Domain.Events;

public class ShipmentCreatedDomainEvent : INotification
{
    public Shipment Shipment { get; }

    public ShipmentCreatedDomainEvent(Shipment shipment)
    {
        Shipment = shipment;
    }
}
