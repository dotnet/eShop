namespace Inked.Ordering.API.Application.Commands;

public class RequestReturnCommandHandler : IRequestHandler<RequestReturnCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public RequestReturnCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<bool> Handle(RequestReturnCommand command, CancellationToken cancellationToken)
    {
        var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToUpdate == null)
        {
            return false;
        }

        orderToUpdate.SetReturnRequestedStatus();
        return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
