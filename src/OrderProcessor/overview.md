# OrderProcessor

Background service that monitors order grace periods and publishes confirmation events for downstream processing in the eShop microservices architecture.

## Summary

- Lightweight background worker service using .NET 10.0 Worker SDK for grace period management.
- Periodically queries PostgreSQL to identify orders exceeding the configured grace period.
- Publishes `GracePeriodConfirmedIntegrationEvent` to RabbitMQ for downstream order validation workflows.
- Implements resilient database access with structured logging and configurable intervals.
- Event-driven architecture with loose coupling to Ordering.API event subscribers.

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `src/OrderProcessor` | Background worker service for grace period expiration detection | `Program.cs`, `Services/GracePeriodManagerService.cs`, `Extensions/Extensions.cs` |

## Components

---

COMPONENT_NAME: GracePeriodManagerService
TYPE: Worker
PURPOSE: Background service that periodically checks for orders exceeding the grace period and publishes confirmation events.
RESPONSIBILITIES:
  - Execute periodic checks every `CheckUpdateTime` seconds to identify expired orders
  - Query PostgreSQL `ordering.orders` table for orders in "Submitted" state with elapsed grace period
  - Create `GracePeriodConfirmedIntegrationEvent` for each expired order
  - Publish integration events to RabbitMQ via `IEventBus`
  - Handle database connection errors gracefully and log fatal connection failures
  - Provide debug-level logging for background task lifecycle events
SOURCE: `Services/GracePeriodManagerService.cs`
CALLS:
  - RabbitMQEventBus (via IEventBus interface) — to publish `GracePeriodConfirmedIntegrationEvent` for each expired order
  - NpgsqlDataSource — to create and open PostgreSQL connections
  - ILogger — to record task lifecycle, debug checks, and error conditions
CALLED_BY:
  - .NET Generic Host (IHostedService) — service container manages lifecycle

---

COMPONENT_NAME: GracePeriodConfirmedIntegrationEvent
TYPE: Utility
PURPOSE: Integration event that signals successful grace period expiration for an order.
RESPONSIBILITIES:
  - Transport order ID across microservice boundary via RabbitMQ
  - Inherit event correlation ID and timestamp from base `IntegrationEvent` class
  - Enable JSON serialization via `[JsonSerializable]` context in `Extensions.cs`
SOURCE: `Events/GracePeriodConfirmedIntegrationEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - GracePeriodManagerService — creates instance and publishes to event bus
  - GracePeriodConfirmedIntegrationEventHandler (Ordering.API) — receives and handles event

---

COMPONENT_NAME: BackgroundTaskOptions
TYPE: Utility
PURPOSE: Configuration model for grace period check parameters.
RESPONSIBILITIES:
  - Store `GracePeriodTime` (minutes) — duration before orders expire
  - Store `CheckUpdateTime` (seconds) — interval between database checks
SOURCE: `BackgroundTaskOptions.cs`
CALLS:
  - NONE
CALLED_BY:
  - GracePeriodManagerService — injects via `IOptions<BackgroundTaskOptions>`
  - Extensions.AddApplicationServices — binds configuration values

---

COMPONENT_NAME: RabbitMQEventBus
TYPE: Service
PURPOSE: Concrete implementation of `IEventBus` that publishes `GracePeriodConfirmedIntegrationEvent` to RabbitMQ.
RESPONSIBILITIES:
  - Establish RabbitMQ channel and publish event to direct exchange
  - Serialize event to JSON with trace context headers for distributed tracing
  - Apply resilience policy (exponential backoff) for broker connectivity failures
  - Acknowledge messages after successful publication
SOURCE: `src/EventBusRabbitMQ/RabbitMQEventBus.cs`
CALLS:
  - EventBusSubscriptionInfo — to access event type registry and JSON serialization options
  - RabbitMQ message broker — to publish messages
CALLED_BY:
  - GracePeriodManagerService (via `IEventBus` interface) — to publish `GracePeriodConfirmedIntegrationEvent`

---

COMPONENT_NAME: NpgsqlDataSource
TYPE: Store
PURPOSE: Managed PostgreSQL connection pool for database access.
RESPONSIBILITIES:
  - Maintain connection pool for `OrderingDB` database
  - Provide `CreateConnection()` method for creating new database connections
  - Handle connection lifetime management and pooling
SOURCE: Aspire.Npgsql (external package via `builder.AddNpgsqlDataSource("orderingdb")`)
CALLS:
  - PostgreSQL database server — to execute SQL queries
CALLED_BY:
  - GracePeriodManagerService — to query orders table

---

COMPONENT_NAME: ILogger
TYPE: API
PURPOSE: Structured logging abstraction for recording task events and errors.
RESPONSIBILITIES:
  - Record debug messages for task state transitions
  - Record info messages for event publishing operations
  - Record error messages for database connection failures
SOURCE: Microsoft.Extensions.Logging (built-in .NET)
CALLS:
  - NONE
CALLED_BY:
  - GracePeriodManagerService — logs lifecycle and operational events

---

COMPONENT_NAME: Extensions.AddApplicationServices
TYPE: Service
PURPOSE: Dependency injection configuration for OrderProcessor services.
RESPONSIBILITIES:
  - Configure RabbitMQ event bus instance via `AddRabbitMqEventBus("eventbus")`
  - Register JSON serialization context for `GracePeriodConfirmedIntegrationEvent`
  - Configure PostgreSQL data source via `AddNpgsqlDataSource("orderingdb")`
  - Bind configuration section to `BackgroundTaskOptions` model
  - Register `GracePeriodManagerService` as hosted service
SOURCE: `Extensions/Extensions.cs`
CALLS:
  - IHostApplicationBuilder — to access service collection and configuration
  - IHostedService registration — to register `GracePeriodManagerService`
CALLED_BY:
  - Program.cs — called during application startup

## Component Call Sequences

### Use-Case: Check and Publish Grace Period Confirmation

STEP 1: .NET Generic Host → GracePeriodManagerService
  OPERATION: StartAsync() (IHostedService implementation)
  PURPOSE: Host initiates background service lifecycle on application startup
  SOURCE: `Services/GracePeriodManagerService.cs`

STEP 2: GracePeriodManagerService → GracePeriodManagerService
  OPERATION: ExecuteAsync(CancellationToken) and CheckConfirmedGracePeriodOrders() loop
  PURPOSE: Main background loop executes periodically every `CheckUpdateTime` seconds
  SOURCE: `Services/GracePeriodManagerService.cs` lines 20-42

STEP 3: GracePeriodManagerService → NpgsqlDataSource
  OPERATION: CreateConnection() and OpenAsync()
  PURPOSE: Acquire database connection from pool and open connection to PostgreSQL
  SOURCE: `Services/GracePeriodManagerService.cs` lines 70-78

STEP 4: GracePeriodManagerService → PostgreSQL
  OPERATION: ExecuteReaderAsync() with SQL query
  PURPOSE: Query `ordering.orders` table for orders in "Submitted" state where `CURRENT_TIMESTAMP - OrderDate >= GracePeriodTime`
  SOURCE: `Services/GracePeriodManagerService.cs` lines 73-76

STEP 5: GracePeriodManagerService → GracePeriodConfirmedIntegrationEvent
  OPERATION: New instance creation with OrderId parameter
  PURPOSE: Create event object for each expired order
  SOURCE: `Services/GracePeriodManagerService.cs` line 55

STEP 6: GracePeriodManagerService → RabbitMQEventBus (via IEventBus)
  OPERATION: PublishAsync(GracePeriodConfirmedIntegrationEvent)
  PURPOSE: Publish grace period confirmation event to RabbitMQ for downstream subscribers
  SOURCE: `Services/GracePeriodManagerService.cs` line 58

STEP 7: RabbitMQEventBus → RabbitMQ Message Broker
  OPERATION: Publish message to direct exchange with routing by event type name
  PURPOSE: Deliver serialized event to all subscribed services (primarily Ordering.API)
  SOURCE: `src/EventBusRabbitMQ/RabbitMQEventBus.cs`

### Use-Case: Handle Database Connection Error

STEP 1: GracePeriodManagerService → NpgsqlDataSource
  OPERATION: CreateConnection() and OpenAsync()
  PURPOSE: Attempt database connection
  SOURCE: `Services/GracePeriodManagerService.cs` lines 70-78

STEP 2: NpgsqlDataSource → PostgreSQL
  OPERATION: TCP connection attempt
  PURPOSE: Establish network connection to postgres instance
  SOURCE: Aspire.Npgsql

STEP 3: NpgsqlException raised
  OPERATION: Exception caught in try-catch
  PURPOSE: Connection failure (e.g., postgres unavailable, network down)
  SOURCE: `Services/GracePeriodManagerService.cs` lines 85-88

STEP 4: GracePeriodManagerService → ILogger
  OPERATION: LogError() with exception and message
  PURPOSE: Record fatal error with stack trace
  SOURCE: `Services/GracePeriodManagerService.cs` line 87

STEP 5: GracePeriodManagerService → CheckConfirmedGracePeriodOrders
  OPERATION: Return empty list (no orders processed)
  PURPOSE: Graceful degradation — loop continues on next interval
  SOURCE: `Services/GracePeriodManagerService.cs` line 89

## Communication Channels

---

CHANNEL_TYPE: MessageQueue
ENDPOINT / EXCHANGE / TOPIC: RabbitMQ direct exchange; routing key = `GracePeriodConfirmedIntegrationEvent`
SOURCE: `Extensions/Extensions.cs` line 6 (AddRabbitMqEventBus configuration)
NOTES: Message format is JSON. Subscription client name (queue name prefix) is `OrderProcessor` as configured in `appsettings.json`. OpenTelemetry trace context headers (W3C Trace Context) are injected into message headers for distributed tracing. Manual acknowledgment; messages acknowledged only after successful processing by subscriber.

---

CHANNEL_TYPE: HTTP (Database)
ENDPOINT / EXCHANGE / TOPIC: PostgreSQL connection via Npgsql data source to `OrderingDB` database
SOURCE: `Extensions/Extensions.cs` line 8 (AddNpgsqlDataSource configuration), `appsettings.Development.json`
NOTES: Connection string: `Host=localhost;Database=OrderingDB;Username=postgres;Password=...` (in development). Connection pool managed by Aspire.Npgsql. Single query executed: `SELECT "Id" FROM ordering.orders WHERE CURRENT_TIMESTAMP - "OrderDate" >= @GracePeriodTime AND "OrderStatus" = 'Submitted'`. Parameter `@GracePeriodTime` bound at runtime from `BackgroundTaskOptions.GracePeriodTime`.

## Dependency Registration and Wiring

---

DI_CONTAINER: Microsoft.Extensions.DependencyInjection (built-in .NET)
REGISTRATION_FILE: `Extensions/Extensions.cs`, method `AddApplicationServices()`

| Abstraction | Implementation | Lifetime | Code Location | Snippet |
|---|---|---|---|---|
| IEventBus | RabbitMQEventBus | Singleton (via AddRabbitMqEventBus) | `Extensions/Extensions.cs` line 6 | `builder.AddRabbitMqEventBus("eventbus").ConfigureJsonOptions(...)` |
| IOptions&lt;BackgroundTaskOptions&gt; | Built-in options pattern | Singleton | `Extensions/Extensions.cs` lines 10-11 | `builder.Services.AddOptions<BackgroundTaskOptions>().BindConfiguration(nameof(BackgroundTaskOptions))` |
| IHostedService | GracePeriodManagerService | Singleton | `Extensions/Extensions.cs` line 13 | `builder.Services.AddHostedService<GracePeriodManagerService>()` |
| NpgsqlDataSource | Aspire.Npgsql managed instance | Singleton | `Extensions/Extensions.cs` line 8 | `builder.AddNpgsqlDataSource("orderingdb")` |
| ILogger&lt;GracePeriodManagerService&gt; | Built-in provider | - | Program.cs (implicit) | `builder.AddBasicServiceDefaults()` |

---

Registration flow:
1. `Program.cs` calls `builder.AddBasicServiceDefaults()` (registers health checks, logging, observability)
2. `Program.cs` calls `builder.AddApplicationServices()` (custom application-specific registration)
3. `AddApplicationServices()` in `Extensions.cs`:
   - Line 6: Registers RabbitMq event bus with JSON serialization context
   - Line 8: Registers PostgreSQL data source with name "orderingdb"
   - Lines 10-11: Registers configuration options from `BackgroundTaskOptions` section
   - Line 13: Registers background service

## Configuration and Secrets

---

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|---|---|---|---|
| Config file (JSON) | `ConnectionStrings.EventBus`, `EventBus.SubscriptionClientName`, `BackgroundTaskOptions.GracePeriodTime`, `BackgroundTaskOptions.CheckUpdateTime`, `Logging.LogLevel.*` | NO | `appsettings.json` |
| Config file (JSON) | `ConnectionStrings.postgres` | YES | `appsettings.Development.json` (username, password) |
| Environment variables | `ConnectionStrings__postgres` (development override) | YES | Implicit via built-in config binding |
| Aspire.Npgsql config | `orderingdb` data source name | NO | Environment/Aspire orchestration configuration |
| RabbitMQ config | `eventbus` connection name, `SubscriptionClientName=OrderProcessor` | NO | `appsettings.json` EventBus section |

---

Production deployment notes:
- `ConnectionStrings.EventBus` should use AMQP URI with TLS (e.g., `amqps://user:password@rabbitmq-host:5671`)
- `ConnectionStrings.postgres` should use secure connection string with SSL/TLS from Azure Key Vault or environment variables
- `appsettings.Development.json` should be excluded from production deployments (only use environment variables)
- Credentials injected via deployment-specific configuration, never hardcoded

## Persistence and Data Access

---

DATABASE: PostgreSQL (OrderingDB)
DATA_ACCESS: Native SQL queries via Npgsql (ADO.NET driver); no ORM
MIGRATIONS_PATH: NONE (OrderProcessor has read-only access; migrations managed by Ordering.Infrastructure)
REPOSITORY_PATTERN: NO

Query Details:
- Table: `ordering.orders`
- Query type: SELECT (read-only)
- Query logic: Find orders in "Submitted" state with expired grace period
- SQL: `SELECT "Id" FROM ordering.orders WHERE CURRENT_TIMESTAMP - "OrderDate" >= @GracePeriodTime AND "OrderStatus" = 'Submitted'`
- Parameter binding: `@GracePeriodTime` bound from `BackgroundTaskOptions.GracePeriodTime` (converted to `TimeSpan` in minutes)
- Connection pooling: Managed by Aspire.Npgsql with default pool settings
- Error handling: Exceptions caught and logged; returns empty list on failure (graceful degradation)

---

Data consistency:
- No transactions (read-only queries do not require ACID guarantees per OrderProcessor)
- Orders table is written by Ordering.Infrastructure (via EF Core)
- OrderProcessor reads current state; no stale data concerns due to periodic check intervals
- Grace period comparison uses `CURRENT_TIMESTAMP` server-side for consistency

## Patterns and Architecture Notes

---

| PATTERN | EVIDENCE | SNIPPET |
|---|---|---|
| **BackgroundService/IHostedService** | Worker service implementing managed lifecycle | `GracePeriodManagerService : BackgroundService` (line 8 of `Services/GracePeriodManagerService.cs`) implements `ExecuteAsync()` for long-running background work |
| **Dependency Injection** | Constructor injection of all dependencies (IOptions, IEventBus, ILogger, NpgsqlDataSource) | `public GracePeriodManagerService(IOptions<BackgroundTaskOptions> options, IEventBus eventBus, ILogger<GracePeriodManagerService> logger, NpgsqlDataSource dataSource)` lines 11-14 |
| **Integration Event Pattern** | Event-driven communication via `IntegrationEvent` base class | `GracePeriodConfirmedIntegrationEvent : IntegrationEvent` publishes to RabbitMQ for cross-service subscribers |
| **Options Pattern** | Configuration binding via `IOptions<T>` and `BindConfiguration()` | `builder.Services.AddOptions<BackgroundTaskOptions>().BindConfiguration(nameof(BackgroundTaskOptions))` (Extensions.cs line 11) + `IOptions<BackgroundTaskOptions> options` constructor injection |
| **Polling with Configurable Intervals** | Periodic background check implemented as configurable `CheckUpdateTime` | `await Task.Delay(delayTime, stoppingToken)` (line 37) where `delayTime = TimeSpan.FromSeconds(_options.CheckUpdateTime)` |
| **Graceful Error Handling** | Try-catch with logging and empty result for database failures | Lines 85-89 in `Services/GracePeriodManagerService.cs` catch `NpgsqlException`, log error, return empty list |
| **JSON Serialization for AOT** | Explicit `[JsonSerializable]` context for Native AOT compatibility | `[JsonSerializable(typeof(GracePeriodConfirmedIntegrationEvent))]` in `Extensions.cs` line 22 |

## Security and Operational Considerations

---

AUTHN_AUTHZ: No HTTP endpoints (background service only). RabbitMQ authentication via connection string (AMQP URI with embedded credentials or external secrets). PostgreSQL authentication via connection string (username/password). Configured via environment-specific `appsettings.json` files.
FILE: `appsettings.json` (EventBus.SubscriptionClientName config), `appsettings.Development.json` (database credentials), `Extensions/Extensions.cs` (DI wiring)

---

KNOWN_RISKS:
- **Password in development config**: `appsettings.Development.json` contains cleartext PostgreSQL password (`"Password=yourWeak(!)Password"`). Mitigation: Never deploy development config to production; use Azure Key Vault or environment variables for secrets in production.
- **No input validation on configuration**: `GracePeriodTime` and `CheckUpdateTime` values not validated (e.g., no bounds checking for negative or zero values). Mitigation: Add validation in `BackgroundTaskOptions` setter or configuration binding.
- **No rate limiting or throttling**: If PostgreSQL query is slow or fails frequently, no backoff mechanism beyond periodic intervals. Mitigation: Consider implementing exponential backoff if database unavailable.
- **No message delivery guarantees**: RabbitMQ acknowledgment model uses manual acknowledgment, but if handler crashes, message may be lost. Mitigation: Ensure idempotent event handling in Ordering.API subscriber.

---

OBSERVABILITY:
- **Logging framework**: Microsoft.Extensions.Logging with structured logging (ILogger&lt;T&gt;)
- **Log levels**: Debug for lifecycle, Info for operations, Error for failures
- **Structured fields**: Event ID, event object (@id, @event properties), exception details
- **Health checks**: Implicit via `AddBasicServiceDefaults()` (health endpoint at `/health`)
- **Correlation IDs**: Inherited from `IntegrationEvent.Id` for distributed tracing
- **Distributed tracing**: OpenTelemetry trace context headers injected by RabbitMQEventBus (W3C Trace Context)
- **Activity Source**: `ActivitySource` named "EventBusRabbitMQ" (from EventBusRabbitMQ.RabbitMQTelemetry)
- **Metrics**: Basic host metrics via `AddBasicServiceDefaults()` (generic host metrics)

Log output examples:
- `GracePeriodManagerService is starting.` (Debug)
- `GracePeriodManagerService background task is doing background work.` (Debug)
- `Checking confirmed grace period orders` (Debug)
- `Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})` (Information)
- `Fatal error establishing database connection` (Error with exception)

---

DEPLOYMENT:
- **Docker image**: `Dockerfile` (standard .NET Worker SDK build)
- **Container runtime**: Docker / Kubernetes
- **Orchestration**: Aspire-based (.NET Aspire) or Kubernetes manifests
- **Health endpoint**: `http://localhost:16888/health` (port from `launchSettings.json`)
- **Configuration method**: Environment variables for secrets, ConfigMap for non-sensitive config (Kubernetes)
- **Related files**: 
  - `OrderProcessor.csproj` (project file with package references)
  - `appsettings.json` and `appsettings.Development.json` (configuration)
  - `Properties/launchSettings.json` (launch profile with port 16888)
