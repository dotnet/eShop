namespace eShop.Ordering.API.Application.Commands.ShipOrder;

public record ShipOrderCommand(int OrderNumber) : IRequest<bool>;
