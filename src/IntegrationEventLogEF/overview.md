# IntegrationEventLogEF

Shared library implementing the Outbox pattern for reliable, transactionally-consistent integration event publishing using Entity Framework Core with PostgreSQL.

## Summary

- Provides reusable EF Core abstractions for persisting integration events atomically alongside domain changes using the Outbox pattern.
- Tracks event publication state transitions (NotPublished → InProgress → Published or PublishedFailed) to enable reliable delivery guarantees.
- Supplies `IntegrationEventLogEntry` domain model and `IIntegrationEventLogService` interface for consuming services (Ordering.API, Catalog.API).
- Supports resilient transactional execution with EF Core resiliency strategies to ensure atomicity between business operations and event log writes.
- Uses Npgsql.EntityFrameworkCore.PostgreSQL driver for PostgreSQL persistence.

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|-------------|
| `src/IntegrationEventLogEF` | EF Core integration event logging library | `Services/IntegrationEventLogService.cs`, `IntegrationEventLogEntry.cs`, `IntegrationLogExtensions.cs` |
| `src/IntegrationEventLogEF/Services` | Service abstractions and implementations | `IIntegrationEventLogService.cs`, `IntegrationEventLogService.cs` |
| `src/IntegrationEventLogEF/Utilities` | Transactional execution helpers | `ResilientTransaction.cs` |

## Components

---

COMPONENT_NAME: IntegrationEventLogEntry
TYPE: Store
PURPOSE: Represents a single integration event record persisted in the IntegrationEventLog table.
RESPONSIBILITIES:
  - Serializes `IntegrationEvent` domain events to JSON for persistence
  - Tracks event publication lifecycle state and retry count
  - Stores event metadata: ID, creation time, type name, transaction ID
  - Provides JSON deserialization to reconstruct typed `IntegrationEvent` instances
  - Validates required fields via `[Required]` annotations on EventTypeName and Content
SOURCE: `src/IntegrationEventLogEF/IntegrationEventLogEntry.cs`
CALLS:
  - NONE (data model only)
CALLED_BY:
  - IntegrationEventLogService

---

COMPONENT_NAME: EventStateEnum
TYPE: Utility
PURPOSE: Defines the state machine transitions for integration event publication lifecycle.
RESPONSIBILITIES:
  - Enumerates event states: NotPublished (0), InProgress (1), Published (2), PublishedFailed (3)
  - Supports idempotency and retry logic by tracking publication attempts
SOURCE: `src/IntegrationEventLogEF/EventStateEnum.cs`
CALLS:
  - NONE (enumeration only)
CALLED_BY:
  - IntegrationEventLogEntry
  - IntegrationEventLogService

---

COMPONENT_NAME: IntegrationLogExtensions
TYPE: Utility
PURPOSE: Provides EF Core fluent configuration for the IntegrationEventLog table mapping.
RESPONSIBILITIES:
  - Configures DbSet entity mapping for `IntegrationEventLogEntry` via extension method
  - Establishes table name as "IntegrationEventLog" and primary key on EventId
  - Enables seamless model builder integration by consuming services
SOURCE: `src/IntegrationEventLogEF/IntegrationLogExtensions.cs`
CALLS:
  - NONE (configuration extension only)
CALLED_BY:
  - Ordering.Infrastructure/OrderingContext
  - Catalog.API/CatalogContext

---

COMPONENT_NAME: ResilientTransaction
TYPE: Utility
PURPOSE: Wraps database transactions with EF Core resiliency strategies to handle transient failures.
RESPONSIBILITIES:
  - Creates EF Core execution strategies that retry on transient database errors
  - Executes scoped actions within an explicit database transaction
  - Commits transaction on action success; rolls back on failure
  - Abstracts resiliency implementation detail from consuming code
SOURCE: `src/IntegrationEventLogEF/Utilities/ResilientTransaction.cs`
CALLS:
  - NONE (utility only)
CALLED_BY:
  - CatalogIntegrationEventService
  - OrderingIntegrationEventService

---

COMPONENT_NAME: IIntegrationEventLogService
TYPE: Service
PURPOSE: Defines the contract for integration event log operations.
RESPONSIBILITIES:
  - Specifies async API for saving, retrieving, and updating event publication state
  - Enables loose coupling between event publishers and event persistence
  - Supports transaction-scoped event retrieval and state transitions
SOURCE: `src/IntegrationEventLogEF/Services/IIntegrationEventLogService.cs`
CALLS:
  - NONE (interface definition)
CALLED_BY:
  - OrderingIntegrationEventService
  - CatalogIntegrationEventService
  - DI container registrations in Ordering.API, Catalog.API

---

COMPONENT_NAME: IntegrationEventLogService
TYPE: Service
PURPOSE: Persists, retrieves, and manages the publication state of integration events with EF Core.
RESPONSIBILITIES:
  - Saves new integration events to the log with TransactionId for grouping
  - Retrieves all unpublished events for a given transaction
  - Marks events as InProgress, Published, or PublishedFailed with TimesSent increment
  - Deserializes JSON-persisted events to typed IntegrationEvent instances
  - Discovers event types at construction time via assembly reflection
  - Manages DbContext lifecycle and disposal
SOURCE: `src/IntegrationEventLogEF/Services/IntegrationEventLogService.cs`
CALLS:
  - NONE (database access via DbContext.Set<IntegrationEventLogEntry>)
CALLED_BY:
  - OrderingIntegrationEventService — to persist and track integration events
  - CatalogIntegrationEventService — to persist and track integration events
  - Event publishing background workers (future)

---

## Component Call Sequences

### Use-Case: Save Integration Event Atomically with Domain Change

STEP 1: OrderingIntegrationEventService → IntegrationEventLogService
  OPERATION: SaveEventAsync(event, transaction)
  PURPOSE: Persists integration event to the log within the same database transaction as the original domain change
  SOURCE: `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`

STEP 2: IntegrationEventLogService → ResilientTransaction
  OPERATION: ExecuteAsync(action)
  PURPOSE: Wraps the SaveEventAsync with resilient transaction execution to handle transient database failures
  SOURCE: `src/IntegrationEventLogEF/Services/IntegrationEventLogService.cs` (implicit in transaction handling)

### Use-Case: Publish Pending Events and Track Publication State

STEP 1: OrderingIntegrationEventService → IntegrationEventLogService
  OPERATION: RetrieveEventLogsPendingToPublishAsync(transactionId)
  PURPOSE: Fetches all NotPublished events for a transaction, deserialized to typed IntegrationEvent instances
  SOURCE: `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`

STEP 2: OrderingIntegrationEventService → IntegrationEventLogService
  OPERATION: MarkEventAsInProgressAsync(eventId)
  PURPOSE: Transitions event state to InProgress before publishing to guard against duplicate publishes
  SOURCE: `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`

STEP 3: OrderingIntegrationEventService → EventBus.PublishAsync
  OPERATION: PublishAsync(event)
  PURPOSE: Publishes the typed event to message broker (RabbitMQ)
  SOURCE: `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`

STEP 4: OrderingIntegrationEventService → IntegrationEventLogService
  OPERATION: MarkEventAsPublishedAsync(eventId) or MarkEventAsFailedAsync(eventId)
  PURPOSE: Updates event state based on publish success/failure for delivery tracking
  SOURCE: `src/Ordering.API/Application/IntegrationEvents/OrderingIntegrationEventService.cs`

## Communication Channels

| CHANNEL_TYPE | ENDPOINT / EXCHANGE / TOPIC | SOURCE | NOTES |
|--------------|---------------------------|--------|-------|
| Database | IntegrationEventLog table | `src/IntegrationEventLogEF/ServiceIntegrationEventLogService.cs` | PostgreSQL row-level persistence. Accessed via EF Core `DbSet<IntegrationEventLogEntry>` |

## Dependency Registration and Wiring

- DI_CONTAINER: Built-in .NET Dependency Injection (IServiceCollection)
- REGISTRATION_FILES:
  - `src/Ordering.API/Extensions/Extensions.cs` — line 24
  - `src/Catalog.API/Extensions/Extensions.cs` (via inherited pattern)

| Lifetime | Interface | Implementation | Purpose |
|----------|-----------|----------------|---------|
| Transient | IIntegrationEventLogService | IntegrationEventLogService<OrderingContext> | Single-use event logging per request in Ordering service |
| Transient | IIntegrationEventLogService | IntegrationEventLogService<CatalogContext> | Single-use event logging per request in Catalog service |

**Registration Code Snippet (Ordering.API):**

```csharp
services.AddTransient<IIntegrationEventLogService, 
    IntegrationEventLogService<OrderingContext>>();
```

## Configuration and Secrets

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|-------------|------|-----------|----------|
| Connection String | `orderingdb`, `catalogdb` | YES | `appsettings.json` per API service |
| EF Core Options | `UseNpgsql()`, `AddNpgsqlDbContext()` | NO | Service startup configuration in Extensions.cs |

## Persistence and Data Access

- DATABASE: PostgreSQL (Npgsql)
- DATA_ACCESS: Entity Framework Core 10.0 with EF Core PostgreSQL provider
- MIGRATIONS_PATH: Managed by consuming services (Ordering.Infrastructure, Catalog.API). The IntegrationEventLogEF library is migration-agnostic; consuming services own migration responsibility.
- REPOSITORY_PATTERN: YES
  - **Abstraction:** `IIntegrationEventLogService`
  - **Implementation:** `IntegrationEventLogService<TContext>`
  - **Persistence Location:** `src/IntegrationEventLogEF/Services/IntegrationEventLogService.cs`

## Patterns and Architecture Notes

| PATTERN | EVIDENCE | SNIPPET |
|---------|----------|---------|
| Outbox Pattern | Atomic event persistence alongside domain changes to guarantee no lost events | `SaveEventAsync(event, transaction)` persists event within the same transaction as domain saves |
| Repository Pattern | `IIntegrationEventLogService` abstracts `DbSet<IntegrationEventLogEntry>` queries | `RetrieveEventLogsPendingToPublishAsync()`, `MarkEventAsPublishedAsync()`, etc. |
| State Machine | Event states (NotPublished → InProgress → Published/PublishedFailed) enforced via `EventStateEnum` | `MarkEventAsInProgressAsync()`, `MarkEventAsPublishedAsync()`, `MarkEventAsFailedAsync()` |
| Generic repository pattern | `IntegrationEventLogService<TContext>` decouples from specific DbContext implementations | Works with `OrderingContext`, `CatalogContext`, or any DbContext that includes IntegrationEventLog table |
| Transient Resiliency | EF Core execution strategies retry transient database failures | `ResilientTransaction.ExecuteAsync()` runs action within execution strategy |
| Type Discovery | Event types loaded at service construction via assembly reflection | Constructor filters assembly types by "IntegrationEvent" suffix |

## Security and Operational Considerations

- AUTHN_AUTHZ: NO authentication/authorization in this library component. Security enforced at service boundary (Ordering.API, Catalog.API) via authorization middleware.
- KNOWN_RISKS:
  - **Risk:** Event deserialization failure if event type not found in assembly. **Mitigation:** Logged as warning; event state remains InProgress and will be retried.
  - **Risk:** Database connection failures during transaction commit. **Mitigation:** Handled by EF Core resiliency strategy; transient failures trigger automatic retry.
  - **Risk:** Orphaned InProgress events if service crashes post-publish, pre-state-update. **Mitigation:** Publish retry logic in consuming service (OrderingIntegrationEventService) will reattempt state transitions.
- OBSERVABILITY: Logging delegated to consuming services. ILogger<IntegrationEventLogService> injected for trace information. State transitions (InProgress, Published, Failed) should be logged by caller.
- DEPLOYMENT: No specific Dockerfile or manifest required. Library is NuGet-style dependency included in consuming service projects (Ordering.API, Catalog.API).

