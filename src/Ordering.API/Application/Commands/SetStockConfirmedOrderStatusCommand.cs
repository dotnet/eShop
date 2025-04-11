namespace Inked.Ordering.API.Application.Commands;

public record SetStockConfirmedOrderStatusCommand(int OrderNumber) : IRequest<bool>;
