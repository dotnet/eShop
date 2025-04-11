namespace Inked.Submission.API.IntegrationEvents;

public sealed class SubmissionIntegrationEventService(
    ILogger<SubmissionIntegrationEventService> logger,
    IEventBus eventBus,
    SubmissionContext catalogContext,
    IIntegrationEventLogService integrationEventLogService)
    : ISubmissionIntegrationEventService, IDisposable
{
    private volatile bool disposedValue;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        try
        {
            logger.LogInformation(
                "Publishing integration event: {IntegrationEventId_published} - ({@IntegrationEvent})", evt.Id, evt);

            await integrationEventLogService.MarkEventAsInProgressAsync(evt.Id);
            await eventBus.PublishAsync(evt);
            await integrationEventLogService.MarkEventAsPublishedAsync(evt.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})",
                evt.Id, evt);
            await integrationEventLogService.MarkEventAsFailedAsync(evt.Id);
        }
    }

    public async Task SaveEventAndSubmissionContextChangesAsync(IntegrationEvent evt)
    {
        logger.LogInformation(
            "CatalogIntegrationEventService - Saving changes and integrationEvent: {IntegrationEventId}", evt.Id);

        //Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
        //See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency            
        await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
        {
            // Achieving atomicity between original catalog database operation and the IntegrationEventLog thanks to a local transaction
            await catalogContext.SaveChangesAsync();
            await integrationEventLogService.SaveEventAsync(evt, catalogContext.Database.CurrentTransaction);
        });
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                (integrationEventLogService as IDisposable)?.Dispose();
            }

            disposedValue = true;
        }
    }
}
