namespace Inked.Ordering.API.Application.Commands;

public record RequestReturnCommand(int OrderNumber) : IRequest<bool>;
