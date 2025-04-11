namespace Inked.Ordering.API.Application.Commands;

public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public RefundOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<bool> Handle(RefundOrderCommand command, CancellationToken cancellationToken)
    {
        var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToUpdate == null)
        {
            return false;
        }

        orderToUpdate.SetStatusToRefunded();
        return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
