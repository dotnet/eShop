namespace eShop.Ordering.API.Application.DomainEventHandlers;

public class OrderStatusChangedToPaidDomainEventHandler : INotificationHandler<OrderStatusChangedToPaidDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger _logger;
    private readonly IBuyerRepository _buyerRepository;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;

    public OrderStatusChangedToPaidDomainEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderStatusChangedToPaidDomainEventHandler> logger,
        IBuyerRepository buyerRepository,
        IOrderingIntegrationEventService orderingIntegrationEventService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _buyerRepository = buyerRepository ?? throw new ArgumentNullException(nameof(buyerRepository));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
    }

    public async Task Handle(OrderStatusChangedToPaidDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        OrderingApiTrace.LogOrderStatusUpdated(_logger, domainEvent.OrderId, OrderStatus.Paid);

        var order = await _orderRepository.GetAsync(domainEvent.OrderId);
        var buyer = await _buyerRepository.FindByIdAsync(order.BuyerId.Value);

        var orderStockList = domainEvent.OrderItems
            .Select(orderItem => new OrderStockItem(orderItem.ProductId, orderItem.Units));

        var integrationEvent = new OrderStatusChangedToPaidIntegrationEvent(
            domainEvent.OrderId,
            order.OrderStatus,
            buyer.Name,
            buyer.IdentityGuid,
            orderStockList);

        await _orderingIntegrationEventService.AddAndSaveEventAsync(integrationEvent);
    }
}
