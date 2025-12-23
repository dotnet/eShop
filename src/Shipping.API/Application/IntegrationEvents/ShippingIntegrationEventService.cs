using Microsoft.EntityFrameworkCore.Storage;

namespace eShop.Shipping.API.Application.IntegrationEvents;

public class ShippingIntegrationEventService : IShippingIntegrationEventService, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ShippingContext _shippingContext;
    private readonly IIntegrationEventLogService _eventLogService;
    private readonly ILogger<ShippingIntegrationEventService> _logger;
    private volatile bool _disposedValue;

    public ShippingIntegrationEventService(
        ILogger<ShippingIntegrationEventService> logger,
        IEventBus eventBus,
        ShippingContext shippingContext,
        IIntegrationEventLogService eventLogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _shippingContext = shippingContext ?? throw new ArgumentNullException(nameof(shippingContext));
        _eventLogService = eventLogService ?? throw new ArgumentNullException(nameof(eventLogService));
    }

    public async Task PublishEventsThroughEventBusAsync(Guid transactionId)
    {
        var pendingLogEvents = await _eventLogService.RetrieveEventLogsPendingToPublishAsync(transactionId);

        foreach (var logEvt in pendingLogEvents)
        {
            _logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", logEvt.EventId, logEvt.IntegrationEvent);

            try
            {
                await _eventLogService.MarkEventAsInProgressAsync(logEvt.EventId);
                await _eventBus.PublishAsync(logEvt.IntegrationEvent);
                await _eventLogService.MarkEventAsPublishedAsync(logEvt.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing integration event: {IntegrationEventId}", logEvt.EventId);
                await _eventLogService.MarkEventAsFailedAsync(logEvt.EventId);
            }
        }
    }

    public async Task AddAndSaveEventAsync(IntegrationEvent evt)
    {
        _logger.LogInformation("Enqueuing integration event {IntegrationEventId} to repository ({@IntegrationEvent})", evt.Id, evt);

        await _eventLogService.SaveEventAsync(evt, _shippingContext.GetCurrentTransaction()!);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                (_eventLogService as IDisposable)?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
