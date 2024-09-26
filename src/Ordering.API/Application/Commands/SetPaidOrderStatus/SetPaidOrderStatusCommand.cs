namespace eShop.Ordering.API.Application.Commands.SetPaidOrderStatus;

public record SetPaidOrderStatusCommand(int OrderNumber) : IRequest<bool>;
