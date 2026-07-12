using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Infrastructure.Repositories;
using eShop.Catalog.API.IntegrationEvents;
using eShop.EventBus.Events;
using eShop.IntegrationEventLogEF;
using eShop.IntegrationEventLogEF.Services;

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace eShop.Catalog.FunctionalTests.Mocks;

internal sealed class NoOpCatalogIntegrationEventService(IServiceProvider serviceProvider) : ICatalogIntegrationEventService
{
    public Task PublishThroughEventBusAsync(IntegrationEvent evt) => Task.CompletedTask;

    public Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt)
    {
        var repository = serviceProvider.GetService<ICatalogRepository>();
        if (repository is not null)
        {
            return repository.SaveChangesAsync();
        }

        var context = serviceProvider.GetService<CatalogContext>();
        if (context is not null)
        {
            return context.SaveChangesAsync();
        }

        return Task.CompletedTask;
    }
}

internal sealed class NoOpIntegrationEventLogService : IIntegrationEventLogService
{
    public Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId) => Task.FromResult(Enumerable.Empty<IntegrationEventLogEntry>());
    public Task SaveEventAsync(IntegrationEvent evt, IDbContextTransaction transaction) => Task.CompletedTask;
    public Task MarkEventAsFailedAsync(Guid eventId) => Task.CompletedTask;
    public Task MarkEventAsInProgressAsync(Guid eventId) => Task.CompletedTask;
    public Task MarkEventAsPublishedAsync(Guid eventId) => Task.CompletedTask;
}
