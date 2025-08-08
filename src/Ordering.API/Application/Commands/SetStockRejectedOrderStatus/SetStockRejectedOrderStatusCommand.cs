namespace eShop.Ordering.API.Application.Commands.SetStockRejectedOrderStatus;

public record SetStockRejectedOrderStatusCommand(int OrderNumber, List<int> OrderStockItems) : IRequest<bool>;
