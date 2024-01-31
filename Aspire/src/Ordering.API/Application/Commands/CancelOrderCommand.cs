namespace eShop.Ordering.API.Application.Commands;

public record CancelOrderCommand(int OrderNumber) : IRequest<bool>;

