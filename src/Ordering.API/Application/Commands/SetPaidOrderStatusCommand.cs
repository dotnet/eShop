namespace Inked.Ordering.API.Application.Commands;

public record SetPaidOrderStatusCommand(int OrderNumber) : IRequest<bool>;
