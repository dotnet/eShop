namespace Inked.Ordering.API.Application.Commands;

public record RefundOrderCommand(int OrderNumber) : IRequest<bool>;
