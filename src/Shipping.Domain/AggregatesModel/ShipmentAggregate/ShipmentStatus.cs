namespace eShop.Shipping.Domain.AggregatesModel.ShipmentAggregate;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShipmentStatus
{
    Created = 1,
    ShipperAssigned = 2,
    PickedUpFromWarehouse = 3,
    InTransitToWarehouse = 4,
    ArrivedAtWarehouse = 5,
    DeliveringToCustomer = 6,
    Delivered = 7,
    Cancelled = 8,
    ReturnedToWarehouse = 9
}
