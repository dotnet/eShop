namespace eShop.Catalog.API.IntegrationEvents;

public class CatalogIntegrationEventService : ICatalogIntegrationEventService, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly CatalogContext _catalogContext;
    private readonly IIntegrationEventLogService _eventLogService;
    private readonly ILogger<CatalogIntegrationEventService> _logger;
    private volatile bool disposedValue;

    public CatalogIntegrationEventService(
        ILogger<CatalogIntegrationEventService> logger,
        IEventBus eventBus,
        CatalogContext catalogContext,
        IIntegrationEventLogService integrationEventLogService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _catalogContext = catalogContext ?? throw new ArgumentNullException(nameof(catalogContext));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _eventLogService = integrationEventLogService ?? throw new ArgumentNullException(nameof(integrationEventLogService));
    }

    public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        try
        {
            _logger.LogInformation("Publishing integration event: {IntegrationEventId_published} - ({@IntegrationEvent})", evt.Id, evt);

            await _eventLogService.MarkEventAsInProgressAsync(evt.Id);
            await _eventBus.PublishAsync(evt);
            await _eventLogService.MarkEventAsPublishedAsync(evt.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", evt.Id, evt);
            await _eventLogService.MarkEventAsFailedAsync(evt.Id);
        }
    }

    public async Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt)
    {
        _logger.LogInformation("CatalogIntegrationEventService - Saving changes and integrationEvent: {IntegrationEventId}", evt.Id);

        //Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
        //See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency            
        await ResilientTransaction.New(_catalogContext).ExecuteAsync(async () =>
        {
            // Achieving atomicity between original catalog database operation and the IntegrationEventLog thanks to a local transaction
            await _catalogContext.SaveChangesAsync();
            await _eventLogService.SaveEventAsync(evt, _catalogContext.Database.CurrentTransaction);
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                (_eventLogService as IDisposable)?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
