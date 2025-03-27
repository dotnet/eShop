namespace eShop.Ordering.API.Application.Commands.SetAwaitingValidationOrderStatus;

public record SetAwaitingValidationOrderStatusCommand(int OrderNumber) : IRequest<bool>;
