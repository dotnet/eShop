namespace eShop.Ordering.API.Application.Commands;

// Regular CommandHandler
public class CompleteOrderByShipmentCommandHandler : IRequestHandler<CompleteOrderByShipmentCommand, bool>
{
    private readonly IOrderRepository _orderRepository;

    public CompleteOrderByShipmentCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Handler which processes the command when
    /// the system executes complete order from external API
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<bool> Handle(CompleteOrderByShipmentCommand command, CancellationToken cancellationToken)
    {
        var orderToCompleteOrderByShipment = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToCompleteOrderByShipment == null)
        {
            return false;
        }

        if (orderToCompleteOrderByShipment.OrderStatus != OrderStatus.Shipped)
        {
            return false; // Sadece "Shipped" (tedarikçi tarafından kargo firmasına gönderildi) olan siparişler tamamlanabilir.
        }

        orderToCompleteOrderByShipment.SetCompletedStatus();
        return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

// Use for Idempotency in Command process
public class CompleteOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CompleteOrderByShipmentCommand, bool>
{
    public CompleteOrderIdentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CompleteOrderByShipmentCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true; // Ignore duplicate requests for processing order completion.
    }
}
