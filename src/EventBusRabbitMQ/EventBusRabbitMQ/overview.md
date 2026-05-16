# EventBusRabbitMQ

Concrete implementation of the IEventBus abstraction using RabbitMQ as the message broker with OpenTelemetry instrumentation and Polly-based resilience.

## Summary

- Provides a production-ready event publication and consumption layer built on top of RabbitMQ.
- Implements `IEventBus` contract with `PublishAsync()` for event publishing and background message consumption.
- Integrates OpenTelemetry for distributed tracing with message header propagation (W3C Trace Context).
- Uses RabbitMQ direct exchange routing with one queue per service instance for event subscriptions.
- Implements graceful error handling and graceful degradation with Polly retry strategies (exponential backoff).
- AOT-compatible with explicit exception handling for reflection-based JSON serialization.
- Implements `IHostedService` to manage RabbitMQ connection lifecycle (startup/shutdown).

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `src/EventBusRabbitMQ` | RabbitMQ implementation of event bus with tracing and resilience | `RabbitMQEventBus.cs`, `RabbitMqDependencyInjectionExtensions.cs` |

## Components

---

COMPONENT_NAME: RabbitMQEventBus
TYPE: Service
PURPOSE: Concrete implementation of IEventBus that publishes and consumes integration events via RabbitMQ.
RESPONSIBILITIES:
  - Publish integration events to RabbitMQ direct exchange with routing by event type name
  - Establish and manage RabbitMQ channel for publishing (per-call) and consuming (persistent)
  - Start background message consumer on application startup (IHostedService)
  - Deserialize incoming messages and dispatch to registered event handlers via service provider
  - Extract and inject OpenTelemetry trace context headers in RabbitMQ messages
  - Apply retry resilience policy with exponential backoff for broker connectivity failures
  - Acknowledge messages only after successful processing (manual acknowledgment)
SOURCE: `RabbitMQEventBus.cs`
CALLS:
  - IConnection — to create channels for publishing and consuming
  - IChannel — to publish messages to exchange and consume from queue
  - IIntegrationEventHandler — to invoke registered event handlers via service provider
  - RabbitMQTelemetry — to access ActivitySource and TextMapPropagator
  - ResiliencePipeline — to execute publish with exponential backoff retry
  - ILogger — to record trace, debug, and error events
  - EventBusSubscriptionInfo — to access event type registry and JSON serialization options
CALLED_BY:
  - Ordering.API (via IEventBus interface) — to publish order-related events
  - Catalog.API (via IEventBus interface) — to publish catalog update events
  - Basket.API (via IEventBus interface) — to publish basket operations
  - PaymentProcessor — to publish payment-related events
  - Webhooks.API — to publish webhook integration events
  - OrderProcessor — to publish grace period confirmation events

---

COMPONENT_NAME: RabbitMQTelemetry
TYPE: Utility
PURPOSE: Encapsulates OpenTelemetry instrumentation (ActivitySource and TextMapPropagator) for RabbitMQ message tracking.
RESPONSIBILITIES:
  - Expose ActivitySource named "EventBusRabbitMQ" for creating distributed trace activities
  - Expose TextMapPropagator (W3C Trace Context) for inject/extract operations on RabbitMQ message headers
SOURCE: `RabbitMQTelemetry.cs`
CALLS:
  - NONE
CALLED_BY:
  - RabbitMQEventBus — to access ActivitySource and Propagator for tracing

---

COMPONENT_NAME: EventBusOptions
TYPE: Utility
PURPOSE: Configuration data transfer object for RabbitMQ event bus settings.
RESPONSIBILITIES:
  - Store subscription client name (queue name prefix)
  - Store retry count for exponential backoff policy configuration
SOURCE: `EventBusOptions.cs`
CALLS:
  - NONE
CALLED_BY:
  - RabbitMQEventBus — to read SubscriptionClientName and RetryCount
  - RabbitMqDependencyInjectionExtensions — during options configuration

---

COMPONENT_NAME: RabbitMqDependencyInjectionExtensions
TYPE: Utility
PURPOSE: Extension method providing fluent registration of RabbitMQ event bus in the .NET dependency injection container.
RESPONSIBILITIES:
  - Register RabbitMQ client connection from Aspire configuration
  - Configure OpenTelemetry tracing with EventBusRabbitMQ ActivitySource
  - Bind EventBusOptions from "EventBus" configuration section
  - Register RabbitMQTelemetry as singleton utility
  - Register RabbitMQEventBus as singleton IEventBus implementation
  - Register RabbitMQEventBus as IHostedService for lifecycle management
SOURCE: `RabbitMqDependencyInjectionExtensions.cs`
CALLS:
  - IHostApplicationBuilder.AddRabbitMQClient() — to register RabbitMQ connection
  - IHostApplicationBuilder.AddOpenTelemetry() — to register OpenTelemetry tracing
  - IServiceCollection.Configure<EventBusOptions>() — to bind configuration
  - IServiceCollection.AddSingleton() — to register services
CALLED_BY:
  - Ordering.API (via Program.cs) — during service configuration
  - Catalog.API (via Program.cs) — during service configuration
  - Basket.API (via Program.cs) — during service configuration
  - PaymentProcessor (via Program.cs) — during service configuration
  - OrderProcessor (via Program.cs) — during service configuration
  - Webhooks.API (via Program.cs) — during service configuration

## Component Call Sequences

### Use-Case: Publish Integration Event

STEP 1: Service (e.g., Ordering.API) → RabbitMQEventBus
  OPERATION: PublishAsync(IntegrationEvent @event)
  PURPOSE: Delegate integration event publishing to RabbitMQ event bus implementation
  SOURCE: `RabbitMQEventBus.cs` line 30 (method signature)

STEP 2: RabbitMQEventBus → RabbitMQTelemetry
  OPERATION: Access ActivitySource and Propagator properties
  PURPOSE: Obtain OpenTelemetry instrumentation for tracing and trace context propagation
  SOURCE: `RabbitMQEventBus.cs` line 21-22

STEP 3: RabbitMQEventBus → ResiliencePipeline
  OPERATION: Execute(Func<Task>)
  PURPOSE: Execute publish operation with exponential backoff retry for transient broker failures
  SOURCE: `RabbitMQEventBus.cs` line 58

STEP 4: ResiliencePipeline → IConnection
  OPERATION: CreateChannelAsync()
  PURPOSE: Acquire channel for message publishing to broker
  SOURCE: `RabbitMQEventBus.cs` line 38

STEP 5: IConnection/IChannel → RabbitMQ Broker
  OPERATION: ExchangeDeclareAsync(ExchangeName, "direct") and BasicPublishAsync()
  PURPOSE: Declare direct exchange if not exists and publish message with routing key (event type name)
  SOURCE: `RabbitMQEventBus.cs` lines 43-46, 99-103

### Use-Case: Consume and Handle Integration Event

STEP 1: IHostedService (RabbitMQEventBus) → IConnection
  OPERATION: StartAsync(CancellationToken) → CreateChannelAsync()
  PURPOSE: Initialize consumer channel on application startup
  SOURCE: `RabbitMQEventBus.cs` lines 253-257

STEP 2: IHostedService → RabbitMQ Broker
  OPERATION: ExchangeDeclareAsync() and QueueDeclareAsync()
  PURPOSE: Ensure direct exchange and durable subscription queue exist
  SOURCE: `RabbitMQEventBus.cs` lines 260-273

STEP 3: RabbitMQ Broker → RabbitMQEventBus
  OPERATION: BasicConsumeAsync() with AsyncEventingBasicConsumer
  PURPOSE: Start consuming messages from subscription queue in background
  SOURCE: `RabbitMQEventBus.cs` lines 278-281

STEP 4: RabbitMQ Broker → RabbitMQEventBus
  OPERATION: Fire AsyncEventingBasicConsumer.ReceivedAsync event with BasicDeliverEventArgs
  PURPOSE: Deliver received message to OnMessageReceived handler
  SOURCE: `RabbitMQEventBus.cs` lines 151-189

STEP 5: OnMessageReceived → RabbitMQTelemetry
  OPERATION: Extract trace context from message headers and start activity
  PURPOSE: Resume distributed trace from publisher's context
  SOURCE: `RabbitMQEventBus.cs` lines 154-163

STEP 6: OnMessageReceived → RabbitMQEventBus
  OPERATION: ProcessEvent(eventName, message)
  PURPOSE: Deserialize message and dispatch to registered event handlers
  SOURCE: `RabbitMQEventBus.cs` line 177

STEP 7: ProcessEvent → IServiceProvider
  OPERATION: GetKeyedServices<IIntegrationEventHandler>(eventType)
  PURPOSE: Resolve all handlers registered for this event type
  SOURCE: `RabbitMQEventBus.cs` line 203

STEP 8: ProcessEvent → IIntegrationEventHandler
  OPERATION: Handle(integrationEvent)
  PURPOSE: Execute business logic for each registered handler (e.g., update inventory, notify customer)
  SOURCE: `RabbitMQEventBus.cs` lines 205-207

STEP 9: OnMessageReceived → IChannel
  OPERATION: BasicAckAsync(deliveryTag, false)
  PURPOSE: Acknowledge successful message processing, removing from queue
  SOURCE: `RabbitMQEventBus.cs` line 215

## Communication Channels

---

CHANNEL_TYPE: MessageQueue (AMQP 0.9.1)
ENDPOINT: RabbitMQ Broker (connection name: "eventbus" via Aspire)
EXCHANGE: eshop_event_bus
EXCHANGE_TYPE: direct
NOTES: Direct exchange routes messages to queues using routing key matching event type name (class name); each service instance has one durable queue with pattern matching all its subscribed event types
SOURCE: `RabbitMQEventBus.cs` lines 46, 261, 265

---

CHANNEL_TYPE: HTTP (indirect dependency)
ENDPOINT: RabbitMQ Management API (optional, not used by component)
NOTES: Aspire may use for health checks and connection pooling
SOURCE: Aspire.RabbitMQ.Client package reference

## Dependency Registration and Wiring

---

DI_CONTAINER: .NET built-in service collection (`IServiceCollection`)
REGISTRATION_FILE: `RabbitMqDependencyInjectionExtensions.cs` → `AddRabbitMqEventBus()`

### Registration 1: RabbitMQ Connection

LIFETIME: Singleton
ABSTRACTION: `IConnection` (RabbitMQ.Client)
IMPLEMENTATION: Managed by Aspire RabbitMQ client extension
CODE:
```csharp
builder.AddRabbitMQClient(connectionName);
```
NOTES: Connection registered via `IHostApplicationBuilder.AddRabbitMQClient()` from Aspire.RabbitMQ.Client; connection string configured via environment or appsettings.json with connection name "eventbus"

### Registration 2: OpenTelemetry Tracing

LIFETIME: Singleton (global)
ABSTRACTION: OpenTelemetry tracing extension
IMPLEMENTATION: `.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource())`
CODE:
```csharp
builder.Services.AddOpenTelemetry()
   .WithTracing(tracing => {
       tracing.AddSource(RabbitMQTelemetry.ActivitySourceName);
   });
```
NOTES: Registers ActivitySource "EventBusRabbitMQ" for distributed tracing; propagates W3C Trace Context headers

### Registration 3: EventBusOptions Configuration

LIFETIME: Scoped (options pattern)
ABSTRACTION: `IOptions<EventBusOptions>`
IMPLEMENTATION: Bound from configuration section "EventBus"
CODE:
```csharp
builder.Services.Configure<EventBusOptions>(
    builder.Configuration.GetSection(SectionName));
```
NOTES: Configuration format: `{ "EventBus": { "SubscriptionClientName": "...", "RetryCount": 10 } }`

### Registration 4: RabbitMQTelemetry Utility

LIFETIME: Singleton
ABSTRACTION: NONE (utility, directly used)
IMPLEMENTATION: `RabbitMQTelemetry` class
CODE:
```csharp
builder.Services.AddSingleton<RabbitMQTelemetry>();
```
NOTES: Provides ActivitySource and TextMapPropagator for message tracing

### Registration 5: RabbitMQEventBus (IEventBus implementation)

LIFETIME: Singleton
ABSTRACTION: `IEventBus`
IMPLEMENTATION: `RabbitMQEventBus`
CODE:
```csharp
builder.Services.AddSingleton<IEventBus, RabbitMQEventBus>();
```
NOTES: Single instance shared across entire application; constructor-injected with logger, service provider, options, and telemetry

### Registration 6: IHostedService for Lifecycle Management

LIFETIME: Singleton
ABSTRACTION: `IHostedService`
IMPLEMENTATION: `RabbitMQEventBus` (cast to IHostedService)
CODE:
```csharp
builder.Services.AddSingleton<IHostedService>(
    sp => (RabbitMQEventBus)sp.GetRequiredService<IEventBus>());
```
NOTES: Makes RabbitMQEventBus implement IHostedService; StartAsync() fires on application start to begin message consumption, StopAsync() fires on shutdown

## Configuration and Secrets

---

SOURCE_TYPE: Configuration file / Environment variable
KEYS:
  - `EventBus:SubscriptionClientName` — name of the subscription queue (e.g., "Ordering.API", "Catalog.API"); used as queue name prefix
  - `EventBus:RetryCount` — maximum retry attempts for exponential backoff (default 10)
  - `ConnectionStrings:eventbus` — RabbitMQ connection string (Aspire managed)
SENSITIVE: YES (connection string contains credentials)
LOCATION: `appsettings.json` / `appsettings.{Environment}.json` / environment variables (prefixed with "DOTNET_" or connection provider specific)
NOTES: Aspire resolves "eventbus" connection name from environment or service manifest

---

SOURCE_TYPE: Environment variable (runtime)
KEYS:
  - `OTEL_EXPORTER_OTLP_ENDPOINT` — OpenTelemetry collector endpoint (if using OTLP export)
  - `OTEL_TRACES_EXPORTER` — trace exporter (e.g., "otlp", "jaeger")
SENSITIVE: NO
LOCATION: Application environment variables
NOTES: Optional; required only if distributed tracing is configured externally

## Persistence and Data Access

DATABASE: NONE (event bus only — events are transient; no persistence)
DATA_ACCESS: NONE
MIGRATIONS_PATH: NONE
REPOSITORY_PATTERN: NO
NOTES: Event bus is a messaging component; event content is stored transiently in RabbitMQ memory/disk (depending on broker configuration). Durable queue setting (line 267) ensures events survive broker restart but not application shutdown.

## Patterns and Architecture Notes

---

PATTERN: Publisher-Subscriber (Pub/Sub)
EVIDENCE: `RabbitMQEventBus.PublishAsync()` and message consumer in `StartAsync()`
SNIPPET:
```csharp
// Publisher side
await channel.BasicPublishAsync(
    exchange: ExchangeName,
    routingKey: routingKey,
    basicProperties: properties,
    body: body);

// Subscriber side
var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
consumer.ReceivedAsync += OnMessageReceived;
await _consumerChannel.BasicConsumeAsync(
    queue: _queueName,
    autoAck: false,
    consumer: consumer);
```

---

PATTERN: Repository/Event Handler Lookup
EVIDENCE: Service provider keyed service resolution for handlers
SNIPPET:
```csharp
foreach (var handler in 
    scope.ServiceProvider.GetKeyedServices<IIntegrationEventHandler>(eventType))
{
    await handler.Handle(integrationEvent);
}
```
NOTES: Uses .NET keyed service provider to resolve multiple handlers per event type; enables decoupling of handler registration from bus logic

---

PATTERN: Resilience (Retry with Exponential Backoff)
EVIDENCE: `CreateResiliencePipeline()` using Polly
SNIPPET:
```csharp
var retryOptions = new RetryStrategyOptions {
    ShouldHandle = new PredicateBuilder()
        .Handle<BrokerUnreachableException>()
        .Handle<SocketException>(),
    MaxRetryAttempts = retryCount,
    DelayGenerator = (context) => 
        ValueTask.FromResult(
            TimeSpan.FromSeconds(Math.Pow(2, context.AttemptNumber)))
};
```
NOTES: Retries on broker-specific exceptions with exponential backoff (2^attempt seconds); default max attempts: 10

---

PATTERN: Distributed Tracing (W3C Trace Context)
EVIDENCE: OpenTelemetry ActivitySource and TextMapPropagator
SNIPPET:
```csharp
using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);
_propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), 
    properties, InjectTraceContextIntoBasicProperties);
```
NOTES: Injects trace headers into RabbitMQ message properties for end-to-end trace correlation; extracts on receive to resume trace context

---

PATTERN: Hosted Service Lifecycle
EVIDENCE: `IHostedService` implementation with `StartAsync()` and `StopAsync()`
NOTES: Ensures message consumer starts after DI container initialization and stops during graceful shutdown

---

PATTERN: AOT Compatibility
EVIDENCE: `[UnconditionalSuppressMessage]` annotations on JSON serialization methods
SNIPPET:
```csharp
[UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", 
    Justification = "JsonSerializer.IsReflectionEnabledByDefault is false")]
private IntegrationEvent DeserializeMessage(string message, Type eventType)
{
    return JsonSerializer.Deserialize(message, eventType, 
        _subscriptionInfo.JsonSerializerOptions) as IntegrationEvent;
}
```
NOTES: Acknowledges that JSON deserialization requires dynamic type info; safe for AOT due to explicit JsonSerializerOptions configuration in EventBusSubscriptionInfo

## Security and Operational Considerations

---

AUTHN_AUTHZ: Inherited from IConnection (RabbitMQ client authentication)
MECHANISM: Configurable via RabbitMQ connection string (username/password via URI or environment)
CONFIG_FILE: `appsettings.json` / environment variable (ConnectionStrings:eventbus)
NOTES: Authentication delegated to Aspire RabbitMQ client; no additional auth layer in event bus

---

KNOWN_RISKS:

- Dead Letter Exchange (DLX) not configured: Failed messages are acknowledged and lost (line 214 comment acknowledges this). Use RabbitMQ DLX for production robustness.
- No circuit breaker: Retry policy alone does not prevent cascading failures; consider Polly circuit breaker in high-load scenarios.
- Unencrypted trace propagation via message headers: W3C Trace Context headers are visible in RabbitMQ message properties; sensitive business data should not be embedded in event payload.
- No message expiration policy: Old messages may consume broker memory; consider TTL policies on exchange/queue.
- Synchronous exception propagation: Exceptions in event handlers may propagate to caller; consider exception boundaries or Dead Letter Queue routing.

---

OBSERVABILITY:

- Logging Level: Trace, Debug, Information, Warning available via `ILogger<RabbitMQEventBus>`
- Log Locations: Trace logs for channel creation, message serialization; Warning logs for event resolution failures
- Distributed Tracing: OpenTelemetry ActivitySource ("EventBusRabbitMQ") with semantic convention tags (messaging.system, messaging.destination.name, messaging.operation)
- Health Checks: None built-in; relies on IConnection health (Aspire managed)
- Metrics: No built-in metrics; OpenTelemetry tracer provides latency data via spans

SOURCE: `RabbitMQEventBus.cs` lines 32-38 (logging), lines 56-113 (tracing)

---

DEPLOYMENT:

Docker and orchestration manifests not included in this component; consult host application (Ordering.API, Catalog.API, etc.) or AppHost project for deployment configuration.

SUPPORTING_FILES:
  - RabbitMQ broker configuration: Aspire service manifest (see AppHost project, typically `eShop.AppHost/Program.cs`)
  - Health checks: Aspire manages via AddRabbitMQClient()
  - Environment variables: Standard .NET configuration (appsettings.json, environment overrides)
