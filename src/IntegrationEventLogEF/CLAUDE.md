# IntegrationEventLogEF

## Overview
The IntegrationEventLogEF project implements the Transactional Outbox pattern for reliable event publishing. It ensures that integration events are published exactly once, even in the face of failures.

## Purpose
- Guarantee reliable event delivery
- Prevent orphaned events (saved but not published)
- Prevent phantom events (published but transaction rolled back)
- Enable exactly-once semantics

## The Problem It Solves
Without the outbox pattern:
```
1. Save order to database     ✓
2. Publish OrderCreated event  ✗ (network failure)
Result: Order exists but event never published
```

With the outbox pattern:
```
1. Save order AND event log in same transaction  ✓
2. Separate process publishes events from log    ✓
Result: Guaranteed consistency
```

## Key Components

### IntegrationEventLogEntry
Entity that stores events for later publishing:
```csharp
public class IntegrationEventLogEntry
{
    public Guid EventId { get; set; }
    public string EventTypeName { get; set; }
    public string Content { get; set; }  // JSON serialized event
    public EventStateEnum State { get; set; }
    public DateTime CreationTime { get; set; }
    public int TimesSent { get; set; }
    public Guid TransactionId { get; set; }
}
```

### Event States
| State | Description |
|-------|-------------|
| `NotPublished` | Event saved, awaiting publishing |
| `InProgress` | Currently being published |
| `Published` | Successfully published |
| `PublishedFailed` | Publishing failed after retries |

### IntegrationEventLogContext
EF Core DbContext for the event log table.

### IIntegrationEventLogService
Service interface for managing the event log:
```csharp
public interface IIntegrationEventLogService
{
    Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);
    Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction);
    Task MarkEventAsPublishedAsync(Guid eventId);
    Task MarkEventAsInProgressAsync(Guid eventId);
    Task MarkEventAsFailedAsync(Guid eventId);
}
```

## Project Structure
```
IntegrationEventLogEF/
├── IntegrationEventLogEntry.cs       # Entity
├── IntegrationEventLogContext.cs     # DbContext
├── EventStateEnum.cs                 # State enumeration
├── Services/
│   ├── IIntegrationEventLogService.cs
│   └── IntegrationEventLogService.cs
└── Utilities/
    └── ResilientTransaction.cs       # Transaction helper
```

## Usage Pattern

### Saving Events with Business Data
```csharp
public async Task CreateOrder(Order order)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();

    try
    {
        // Save business data
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        // Save event in same transaction
        var @event = new OrderStartedIntegrationEvent(order.Id);
        await _eventLogService.SaveEventAsync(@event, transaction);

        await transaction.CommitAsync();

        // Publish after commit
        await _eventBus.PublishAsync(@event);
        await _eventLogService.MarkEventAsPublishedAsync(@event.Id);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Publishing Pending Events
```csharp
public async Task PublishPendingEvents(Guid transactionId)
{
    var pendingEvents = await _eventLogService
        .RetrieveEventLogsPendingToPublishAsync(transactionId);

    foreach (var entry in pendingEvents)
    {
        try
        {
            await _eventLogService.MarkEventAsInProgressAsync(entry.EventId);
            await _eventBus.PublishAsync(entry.Event);
            await _eventLogService.MarkEventAsPublishedAsync(entry.EventId);
        }
        catch
        {
            await _eventLogService.MarkEventAsFailedAsync(entry.EventId);
        }
    }
}
```

## Database Schema
| Column | Type | Description |
|--------|------|-------------|
| EventId | GUID | Primary key |
| EventTypeName | String | Full type name of event |
| Content | Text | JSON serialized event |
| State | Int | Current state |
| CreationTime | DateTime | When event was created |
| TimesSent | Int | Publish attempt count |
| TransactionId | GUID | Owning transaction ID |

## Used By
- **Ordering.API**: `OrderingIntegrationEventService`
- **Catalog.API**: `CatalogIntegrationEventService`
- **Webhooks.API**: Webhook event publishing

## Dependencies
- **Entity Framework Core**: Data access
- **EventBus**: Integration event types

## Benefits
1. **Atomicity**: Events saved with business data
2. **Reliability**: Events not lost on failure
3. **Idempotency**: Event IDs prevent duplicates
4. **Auditability**: Full event history maintained
5. **Retry**: Failed events can be reprocessed

## Notes
- Table added to existing service DbContext
- Requires same database as business data
- Consider cleanup job for old published events
