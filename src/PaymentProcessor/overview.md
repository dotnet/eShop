# PaymentProcessor

Headless event-driven microservice that simulates payment processing in response to order stock confirmation events.

## Summary

- Subscribes to `OrderStatusChangedToStockConfirmedIntegrationEvent` via RabbitMQ event bus
- Simulates payment processing with configurable success/failure outcome
- Publishes `OrderPaymentSucceededIntegrationEvent` or `OrderPaymentFailedIntegrationEvent` based on configuration
- No HTTP endpoints, database access, or persistent storage
- Built on .NET 10.0 Web SDK with RabbitMQ event broker

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `src/PaymentProcessor` | Headless event processor for order payment simulation | `Program.cs` |

## Components

---

COMPONENT_NAME: PaymentProcessor.Program
TYPE: Service
PURPOSE: Bootstrap and wire the payment processing microservice runtime with event bus configuration.
RESPONSIBILITIES:
  - Configure .NET host with service defaults
  - Register RabbitMQ event bus implementation
  - Subscribe to `OrderStatusChangedToStockConfirmedIntegrationEvent`
  - Configure `PaymentOptions` from application settings
  - Run the async web application
SOURCE: `Program.cs`
CALLS:
  - EventBusBuilderExtensions — to register RabbitMQ event bus and subscription
  - IEventBus — indirectly via handler subscription
CALLED_BY:
  - .NET Runtime

---

COMPONENT_NAME: OrderStatusChangedToStockConfirmedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Processes stock confirmation events and executes payment simulation logic.
RESPONSIBILITIES:
  - Handle incoming `OrderStatusChangedToStockConfirmedIntegrationEvent` messages from event bus
  - Read `PaymentOptions.PaymentSucceeded` flag to determine payment outcome
  - Publish appropriate payment result event based on configuration
  - Log event processing and publication operations
SOURCE: `IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs`
CALLS:
  - IEventBus — to publish payment result events (`OrderPaymentSucceededIntegrationEvent` or `OrderPaymentFailedIntegrationEvent`)
  - ILogger — to record event handling and publication
  - IOptionsMonitor<PaymentOptions> — to read payment success configuration
CALLED_BY:
  - RabbitMQEventBus (EventBusRabbitMQ)

---

COMPONENT_NAME: PaymentOptions
TYPE: Utility
PURPOSE: Configuration container for payment simulation behavior.
RESPONSIBILITIES:
  - Hold boolean flag indicating whether payment simulation should succeed or fail
  - Serve as strongly-typed configuration model bound from `appsettings.json`
SOURCE: `PaymentOptions.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler

---

COMPONENT_NAME: OrderStatusChangedToStockConfirmedIntegrationEvent
TYPE: Utility
PURPOSE: Integration event representing order stock confirmation state change.
RESPONSIBILITIES:
  - Contain `OrderId` payload for identifying the order
  - Extend `IntegrationEvent` base with event correlation ID and timestamp
  - Define event schema for RabbitMQ message deserialization
SOURCE: `IntegrationEvents/Events/OrderStatusChangedToStockConfirmedIntegrationEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler
  - RabbitMQEventBus (EventBusRabbitMQ)

---

COMPONENT_NAME: OrderPaymentSucceededIntegrationEvent
TYPE: Utility
PURPOSE: Integration event signaling successful payment processing.
RESPONSIBILITIES:
  - Contain `OrderId` payload for correlating payment success to original order
  - Extend `IntegrationEvent` base for event broker serialization
  - Notify downstream services that order payment is confirmed
SOURCE: `IntegrationEvents/Events/OrderPaymentSucceededIntegrationEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler

---

COMPONENT_NAME: OrderPaymentFailedIntegrationEvent
TYPE: Utility
PURPOSE: Integration event signaling payment processing failure.
RESPONSIBILITIES:
  - Contain `OrderId` payload for correlating payment failure to original order
  - Extend `IntegrationEvent` base for event broker serialization
  - Notify downstream services that order payment failed
SOURCE: `IntegrationEvents/Events/OrderPaymentFailedIntegrationEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler

---

COMPONENT_NAME: IEventBus
TYPE: API
PURPOSE: Abstract event publishing interface provided by EventBus library.
RESPONSIBILITIES:
  - Expose `PublishAsync(IntegrationEvent @event)` method for event publication
  - Delegate to concrete RabbitMQ implementation at runtime
SOURCE: `(External) EventBus/Abstractions/IEventBus.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler

---

COMPONENT_NAME: IIntegrationEventHandler
TYPE: API
PURPOSE: Generic event handler contract provided by EventBus library.
RESPONSIBILITIES:
  - Define `Handle(IntegrationEvent @event)` async method contract
  - Enable polymorphic dispatch of events to handler implementations
SOURCE: `(External) EventBus/Abstractions/IIntegrationEventHandler.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler (implementation)

---

COMPONENT_NAME: RabbitMQEventBus
TYPE: API
PURPOSE: Concrete RabbitMQ implementation of event bus (from EventBusRabbitMQ library).
RESPONSIBILITIES:
  - Bridge PaymentProcessor to RabbitMQ broker for event publication
  - Deserialize incoming events and invoke registered handlers
  - Manage RabbitMQ connection and message publishing
SOURCE: `(External) EventBusRabbitMQ/RabbitMQEventBus.cs`
CALLS:
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler — to invoke handler when event arrives
CALLED_BY:
  - Program (via dependency injection)
  - OrderStatusChangedToStockConfirmedIntegrationEventHandler (via IEventBus interface)

## Component Call Sequences

### Use-Case: Process Stock Confirmation and Simulate Payment

STEP 1: RabbitMQEventBus → OrderStatusChangedToStockConfirmedIntegrationEventHandler
  OPERATION: Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event)
  PURPOSE: Route incoming event from RabbitMQ queue to handler implementation
  SOURCE: `IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs`

STEP 2: OrderStatusChangedToStockConfirmedIntegrationEventHandler → IOptionsMonitor<PaymentOptions>
  OPERATION: CurrentValue.PaymentSucceeded
  PURPOSE: Read configuration to determine payment simulation outcome
  SOURCE: `IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs:22`

STEP 3: OrderStatusChangedToStockConfirmedIntegrationEventHandler → OrderPaymentSucceededIntegrationEvent | OrderPaymentFailedIntegrationEvent
  OPERATION: Instantiate(OrderId)
  PURPOSE: Create appropriate payment result event based on configuration flag
  SOURCE: `IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs:17-24`

STEP 4: OrderStatusChangedToStockConfirmedIntegrationEventHandler → IEventBus
  OPERATION: PublishAsync(orderPaymentIntegrationEvent)
  PURPOSE: Publish payment result event to RabbitMQ for notification of dependent services
  SOURCE: `IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs:26`

STEP 5: IEventBus → RabbitMQEventBus
  OPERATION: PublishAsync(event)
  PURPOSE: Route to concrete RabbitMQ implementation via dependency injection
  SOURCE: `IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs:26`

STEP 6: RabbitMQEventBus → RabbitMQ Broker
  OPERATION: BasicPublish(exchange, routingKey, messageBody)
  PURPOSE: Send payment result event to message broker for fanout to subscribed services
  SOURCE: `(External) EventBusRabbitMQ/RabbitMQEventBus.cs`

---

### Use-Case: Application Startup and Event Subscription

STEP 1: .NET Runtime → Program.cs
  OPERATION: WebApplication.CreateBuilder(args)
  PURPOSE: Initialize .NET web application builder with configuration sources
  SOURCE: `Program.cs:1`

STEP 2: Program.cs → AddServiceDefaults()
  OPERATION: AddServiceDefaults()
  PURPOSE: Configure standard eShop service infrastructure (logging, health checks, telemetry)
  SOURCE: `Program.cs:3`

STEP 3: Program.cs → AddRabbitMqEventBus()
  OPERATION: AddRabbitMqEventBus("EventBus")
  PURPOSE: Register RabbitMQ event bus implementation with dependency container
  SOURCE: `Program.cs:5`

STEP 4: AddRabbitMqEventBus() → AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent, OrderStatusChangedToStockConfirmedIntegrationEventHandler>()
  OPERATION: AddSubscription<TEvent, THandler>()
  PURPOSE: Register event-handler pair and configure event type registry for deserialization
  SOURCE: `Program.cs:6`

STEP 5: Program.cs → AddOptions<PaymentOptions>()
  OPERATION: BindConfiguration(nameof(PaymentOptions))
  PURPOSE: Bind PaymentOptions configuration section from appsettings.json to IOptions<PaymentOptions>
  SOURCE: `Program.cs:8-9`

STEP 6: Program.cs → app.RunAsync()
  OPERATION: RunAsync()
  PURPOSE: Start application runtime and RabbitMQ connection for event subscription and message delivery
  SOURCE: `Program.cs:13`

## Communication Channels

CHANNEL_TYPE: MessageQueue
ENDPOINT / EXCHANGE / TOPIC: `eshop_event_bus` (direct exchange); subscription queue: `PaymentProcessor`
SOURCE: `appsettings.json` (`ConnectionStrings.EventBus` = `amqp://localhost`)
NOTES: RabbitMQ AMQP protocol; connection string configured in `appsettings.json`; event subscription established via `AddSubscription<>()` in Program.cs; OrderStatusChangedToStockConfirmedIntegrationEvent consumed; OrderPaymentSucceededIntegrationEvent and OrderPaymentFailedIntegrationEvent published

## Dependency Registration and Wiring

DI_CONTAINER: Built-in .NET Service Collection and Dependency Injection

REGISTRATION_FILE: `Program.cs`

Registration entries:

1. Lifetime: Transient
   Abstraction: `IEventBus`
   Concrete: `RabbitMQEventBus`
   Code:
   ```csharp
   builder.AddRabbitMqEventBus("EventBus")
       .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent, 
                        OrderStatusChangedToStockConfirmedIntegrationEventHandler>();
   ```

2. Lifetime: Keyed Transient
   Abstraction: `IIntegrationEventHandler<OrderStatusChangedToStockConfirmedIntegrationEvent>`
   Concrete: `OrderStatusChangedToStockConfirmedIntegrationEventHandler`
   Code:
   ```csharp
   .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent, 
                    OrderStatusChangedToStockConfirmedIntegrationEventHandler>()
   ```

3. Lifetime: Scoped
   Abstraction: `IOptions<PaymentOptions>`
   Concrete: `OptionsManager<PaymentOptions>` (framework-provided)
   Code:
   ```csharp
   builder.Services.AddOptions<PaymentOptions>()
       .BindConfiguration(nameof(PaymentOptions));
   ```

4. Lifetime: Singleton (from AddServiceDefaults)
   Abstraction: `ILogger<T>`, `IHealthChecksBuilder`, Telemetry services
   Concrete: Framework implementations
   Code:
   ```csharp
   builder.AddServiceDefaults();
   ```

## Configuration and Secrets

SOURCE_TYPE: Configuration file
KEYS: `Logging:LogLevel:Default`, `Logging:LogLevel:Microsoft.AspNetCore`, `ConnectionStrings:EventBus`, `EventBus:SubscriptionClientName`, `PaymentOptions:PaymentSucceeded`
SENSITIVE: NO
LOCATION: `appsettings.json`

---

SOURCE_TYPE: Configuration file (development overrides)
KEYS: `Logging:LogLevel:Default`
SENSITIVE: NO
LOCATION: `appsettings.Development.json`

---

SOURCE_TYPE: Environment variables (implicit)
KEYS: Any of the above keys can be overridden via environment variables in format `Section__Key`
SENSITIVE: Potentially YES for `ConnectionStrings:EventBus` (may contain RabbitMQ credentials in production)
LOCATION: .NET configuration provider chain

## Persistence and Data Access

DATABASE: NONE

DATA_ACCESS: NONE

MIGRATIONS_PATH: NONE

REPOSITORY_PATTERN: NO

## Patterns and Architecture Notes

PATTERN: Event-Driven Architecture
EVIDENCE: `Program.cs:5-6` — event bus registration and subscription; `IntegrationEvents/EventHandling/OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs` — event handler implementation
SNIPPET:
```csharp
builder.AddRabbitMqEventBus("EventBus")
    .AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent, 
                     OrderStatusChangedToStockConfirmedIntegrationEventHandler>();
```

---

PATTERN: Dependency Injection (IoC Container)
EVIDENCE: `Program.cs:1-9` — service registration and configuration binding
SNIPPET:
```csharp
builder.AddServiceDefaults();
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration(nameof(PaymentOptions));
```

---

PATTERN: Configuration Options (Options Pattern)
EVIDENCE: `PaymentOptions.cs` — configuration class; `Program.cs:8-9` — binding from configuration; `OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs:21` — consumption via `IOptionsMonitor<>`
SNIPPET:
```csharp
if (options.CurrentValue.PaymentSucceeded) { ... }
```

---

PATTERN: Transient Event Handler Registration
EVIDENCE: `Program.cs:6` — `AddSubscription<>()` registers handler; `EventBusRabbitMQ` uses keyed service lookup for handler instantiation per event
SNIPPET:
```csharp
.AddSubscription<OrderStatusChangedToStockConfirmedIntegrationEvent, 
                 OrderStatusChangedToStockConfirmedIntegrationEventHandler>()
```

## Security and Operational Considerations

AUTHN_AUTHZ: None configured in PaymentProcessor
MECHANISM: Event bus connection credentials (if any) in RabbitMQ connection string `amqp://localhost` (no credentials in default configuration)
SOURCE: `appsettings.json:ConnectionStrings.EventBus`

KNOWN_RISKS:
- RabbitMQ connection string `amqp://localhost` hardcoded in `appsettings.json` — production deployment must override via environment variable or configuration service
- No authentication/authorization for message consumption — any service connected to RabbitMQ can receive published events
- Payment simulation always succeeds if `PaymentOptions.PaymentSucceeded = true` — no actual payment gateway integration or fraud detection
- No input validation on incoming `OrderStatusChangedToStockConfirmedIntegrationEvent` beyond schema validation

OBSERVABILITY: Logging via ILogger
FRAMEWORK: Microsoft.Extensions.Logging
HEALTH_CHECKS: Configured via `AddServiceDefaults()` in eShop.ServiceDefaults
ENDPOINTS:
- Health check endpoint: `/health` (configured by `app.MapDefaultEndpoints()` in `Program.cs:11`)

DEPLOYMENT: 
- DOCKERFILE: Not included in PaymentProcessor directory; deployment via standard .NET containerization
- DOCKER_COMPOSE: eShop uses orchestrated deployment; check root docker-compose or deploy files
- KUBERNETES: eShop follows container patterns; see root deployment manifests
- BUILD_OUTPUT: `artifacts/bin/PaymentProcessor/` (MSBuild target framework net10.0)
