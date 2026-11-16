namespace eShop.Ordering.API.Application.Commands;

// Regular CommandHandler
public class SetAwaitingValidationOrderStatusCommandHandler : ICommandHandler<SetAwaitingValidationOrderStatusCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public SetAwaitingValidationOrderStatusCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Handler which processes the command when
    /// graceperiod has finished
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<bool> Handle(SetAwaitingValidationOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToUpdate == null)
        {
            return false;
        }

        orderToUpdate.SetAwaitingValidationStatus();
        return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
