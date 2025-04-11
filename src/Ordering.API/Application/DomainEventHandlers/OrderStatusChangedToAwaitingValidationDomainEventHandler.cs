namespace Inked.Ordering.API.Application.DomainEventHandlers;

public class OrderStatusChangedToAwaitingValidationDomainEventHandler
    : INotificationHandler<OrderStatusChangedToAwaitingValidationDomainEvent>
{
    private readonly IBuyerRepository _buyerRepository;
    private readonly ILogger _logger;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly IOrderRepository _orderRepository;

    public OrderStatusChangedToAwaitingValidationDomainEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderStatusChangedToAwaitingValidationDomainEventHandler> logger,
        IBuyerRepository buyerRepository,
        IOrderingIntegrationEventService orderingIntegrationEventService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _buyerRepository = buyerRepository;
        _orderingIntegrationEventService = orderingIntegrationEventService;
    }

    public async Task Handle(OrderStatusChangedToAwaitingValidationDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        OrderingApiTrace.LogOrderStatusUpdated(_logger, domainEvent.OrderId, OrderStatus.AwaitingValidation);

        var order = await _orderRepository.GetAsync(domainEvent.OrderId);
        var buyer = await _buyerRepository.FindByIdAsync(order.BuyerId.Value);

        var orderStockList = domainEvent.OrderItems
            .Select(orderItem => new OrderStockItem(orderItem.ProductId, orderItem.Units));

        var integrationEvent = new OrderStatusChangedToAwaitingValidationIntegrationEvent(order.Id, order.OrderStatus,
            buyer.Name, buyer.IdentityGuid, orderStockList);
        await _orderingIntegrationEventService.AddAndSaveEventAsync(integrationEvent);
    }
}
