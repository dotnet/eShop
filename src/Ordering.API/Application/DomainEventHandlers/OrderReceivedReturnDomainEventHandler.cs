// File: src/Ordering.API/Application/DomainEventHandlers/OrderReceivedReturnDomainEventHandler.cs

namespace Inked.Ordering.API.Application.DomainEventHandlers;

public class OrderReceivedReturnDomainEventHandler : INotificationHandler<OrderReceivedReturnDomainEvent>
{
    private readonly ILogger<OrderReceivedReturnDomainEventHandler> _logger;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;

    public OrderReceivedReturnDomainEventHandler(
        IOrderingIntegrationEventService orderingIntegrationEventService,
        ILogger<OrderReceivedReturnDomainEventHandler> logger)
    {
        _orderingIntegrationEventService = orderingIntegrationEventService;
        _logger = logger;
    }

    public async Task Handle(OrderReceivedReturnDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderId} received return.", domainEvent.Order.Id);

        var integrationEvent =
            new OrderStatusChangedToReceivedReturnIntegrationEvent(domainEvent.Order.Id,
                domainEvent.Order.Buyer.IdentityGuid);
        await _orderingIntegrationEventService.AddAndSaveEventAsync(integrationEvent);
    }
}
