namespace eShop.Shipping.Domain.Events;

public class ShipmentStatusChangedDomainEvent : INotification
{
    public Shipment Shipment { get; }

    public ShipmentStatusChangedDomainEvent(Shipment shipment)
    {
        Shipment = shipment;
    }
}
