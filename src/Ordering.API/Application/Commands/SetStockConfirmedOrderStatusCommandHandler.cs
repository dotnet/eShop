namespace eShop.Ordering.API.Application.Commands;

// Regular CommandHandler
public class SetStockConfirmedOrderStatusCommandHandler : ICommandHandler<SetStockConfirmedOrderStatusCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public SetStockConfirmedOrderStatusCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Handler which processes the command when
    /// Stock service confirms the request
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<bool> Handle(SetStockConfirmedOrderStatusCommand command, CancellationToken cancellationToken)
    {
        // Simulate a work time for confirming the stock
        await Task.Delay(10000, cancellationToken);

        var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToUpdate == null)
        {
            return false;
        }

        orderToUpdate.SetStockConfirmedStatus();
        return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
