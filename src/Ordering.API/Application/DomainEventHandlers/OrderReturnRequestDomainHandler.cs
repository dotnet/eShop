using Inked.WebApp.Services.OrderStatus.IntegrationEvents;

namespace Inked.Ordering.API.Application.DomainEventHandlers;

public class OrderReturnRequestedDomainEventHandler : INotificationHandler<OrderReturnRequestedDomainEvent>
{
    private readonly IBuyerRepository _buyerRepository;
    private readonly ILogger _logger;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly IOrderRepository _orderRepository;

    public OrderReturnRequestedDomainEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderReturnRequestedDomainEventHandler> logger,
        IBuyerRepository buyerRepository,
        IOrderingIntegrationEventService orderingIntegrationEventService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _buyerRepository = buyerRepository ?? throw new ArgumentNullException(nameof(buyerRepository));
        _orderingIntegrationEventService = orderingIntegrationEventService;
    }

    public async Task Handle(OrderReturnRequestedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetAsync(domainEvent.Order.Id);
        var buyer = await _buyerRepository.FindByIdAsync(order.BuyerId.Value);

        var integrationEvent =
            new OrderReturnRequestedIntegrationEvent(order.Id, order.OrderStatus, buyer.Name, buyer.IdentityGuid);
        await _orderingIntegrationEventService.AddAndSaveEventAsync(integrationEvent);
    }
}
