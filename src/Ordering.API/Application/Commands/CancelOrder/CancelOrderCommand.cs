namespace eShop.Ordering.API.Application.Commands.CancelOrder;

public record CancelOrderCommand(int OrderNumber) : IRequest<bool>;

