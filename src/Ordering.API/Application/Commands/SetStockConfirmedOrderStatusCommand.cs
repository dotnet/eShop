namespace eShop.Ordering.API.Application.Commands;

public record SetStockConfirmedOrderStatusCommand(int OrderNumber) : ICommand<bool>;
