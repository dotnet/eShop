namespace eShop.Shipping.Domain.Events;

public class ShipmentCompletedDomainEvent : INotification
{
    public Shipment Shipment { get; }

    public ShipmentCompletedDomainEvent(Shipment shipment)
    {
        Shipment = shipment;
    }
}
