namespace eShop.Ordering.API.Application.Commands;

public record SetPaidOrderStatusCommand(int OrderNumber) : ICommand<bool>;
