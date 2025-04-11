namespace Inked.Submission.API.IntegrationEvents;

public interface ISubmissionIntegrationEventService
{
    Task SaveEventAndSubmissionContextChangesAsync(IntegrationEvent evt);
    Task PublishThroughEventBusAsync(IntegrationEvent evt);
}
