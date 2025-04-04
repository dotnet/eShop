namespace eShop.Ordering.API.Application.Commands;

public record CompleteOrderByShipmentCommand(int OrderNumber, string TrackingNumber, string CarrierName, DateTime ShipmentDate) : IRequest<bool>;
