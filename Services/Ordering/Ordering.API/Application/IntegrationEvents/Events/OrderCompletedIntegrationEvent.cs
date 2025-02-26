namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.DomainEventHandlers.OrderCompleted
{
    public class OrderCompletedDomainEventHandler : INotificationHandler<OrderCompletedDomainEvent>
    {
        private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
        private readonly ILogger<OrderCompletedDomainEventHandler> _logger;

        public OrderCompletedDomainEventHandler(
            IOrderingIntegrationEventService orderingIntegrationEventService,
            ILogger<OrderCompletedDomainEventHandler> logger)
        {
            _orderingIntegrationEventService = orderingIntegrationEventService;
            _logger = logger;
        }

        public async Task Handle(OrderCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Order {OrderId} has been completed - Publishing integration event", domainEvent.Order.Id);

            var order = domainEvent.Order;
            var integrationEvent = new OrderCompletedIntegrationEvent(
                order.Id, 
                order.OrderStatus.Name,
                order.GetBuyerName());

            await _orderingIntegrationEventService.AddAndSaveEventAsync(integrationEvent);
        }
    }
} 