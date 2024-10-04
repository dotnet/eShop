namespace eShop.Ordering.API.Application.Commands.SetStockConfirmedOrderStatus;

public record SetStockConfirmedOrderStatusCommand(int OrderNumber) : IRequest<bool>;
