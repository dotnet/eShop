# EventBus

Abstraction layer and contracts for AOT-compatible event publishing and subscription across the eShop microservices using a pluggable message broker implementation.

## Summary

- Provides generic, type-safe abstractions (`IEventBus`, `IIntegrationEventHandler<T>`) for event-driven communication across microservices.
- Base `IntegrationEvent` record class with event correlation ID and timestamp metadata.
- Fluent builder API (`IEventBusBuilder`) for registering event-handler subscriptions in the dependency container.
- Designed for AOT compatibility with explicit dynamic reflection annotations and JSON type resolution configuration.
- Concrete implementations (e.g., `EventBusRabbitMQ`) use keyed transient services to support multiple handlers per event type.

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `src/EventBus` | Core event bus abstractions and registration logic | `Abstractions/IEventBus.cs`, `Extensions/EventBusBuilderExtensions.cs` |

## Components

---

COMPONENT_NAME: IEventBus
TYPE: API
PURPOSE: Defines the contract for publishing integration events to message brokers.
RESPONSIBILITIES:
  - Expose `PublishAsync(IntegrationEvent @event)` method for event publication
  - Serve as the public boundary between application code and message broker implementation
SOURCE: `Abstractions/IEventBus.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderService (Ordering.API)
  - CatalogService (Catalog.API)
  - BasketService (Basket.API)
  - PaymentProcessor
  - WebhooksService (Webhooks.API)

---

COMPONENT_NAME: IntegrationEvent
TYPE: Utility
PURPOSE: Base record class for all domain events that cross microservice boundaries.
RESPONSIBILITIES:
  - Provide standard event identity and correlation properties (`Id`, `CreationDate`)
  - Serve as the root type for runtime type resolution by message brokers
  - Enable JSON serialization with `[JsonInclude]` attributes for fields
SOURCE: `Events/IntegrationEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderStartedIntegrationEvent (Basket.API)
  - OrderStatusChangedToAwaitingValidationIntegrationEvent (Catalog.API)
  - OrderStatusChangedToPaidIntegrationEvent (Catalog.API)
  - OrderStatusChangedToShippedIntegrationEvent (Ordering.API)
  - OrderStockConfirmedIntegrationEvent (Catalog.API)
  - OrderStockRejectedIntegrationEvent (Ordering.API)
  - OrderPaymentSucceededIntegrationEvent (PaymentProcessor)
  - OrderPaymentFailedIntegrationEvent (PaymentProcessor)
  - ProductPriceChangedIntegrationEvent (Catalog.API)
  - All other `IIntegrationEvent` implementations

---

COMPONENT_NAME: IIntegrationEventHandler
TYPE: API
PURPOSE: Defines the contract for handling integration events within a microservice.
RESPONSIBILITIES:
  - Expose `Handle(IntegrationEvent @event)` async method for non-generic event handling
  - Provide generic variant `IIntegrationEventHandler<TIntegrationEvent>` for type-safe handlers
  - Enable polymorphic dispatch by message brokers to appropriate handler implementation
SOURCE: `Abstractions/IIntegrationEventHandler.cs`
CALLS:
  - NONE
CALLED_BY:
  - RabbitMQEventBus (EventBusRabbitMQ)
  - IKeyedServiceProvider (built-in .NET DI)

---

COMPONENT_NAME: EventBusSubscriptionInfo
TYPE: Utility
PURPOSE: Configuration container for event-type registry and JSON serialization options.
RESPONSIBILITIES:
  - Maintain bidirectional mapping of event type names (strings) to CLR `Type` objects
  - Store JSON serialization options for all event deserialization across the broker
  - Provide AOT-safe default resolver with reflection-safe type info combining
SOURCE: `Abstractions/EventBusSubscriptionInfo.cs`
CALLS:
  - NONE
CALLED_BY:
  - RabbitMQEventBus (EventBusRabbitMQ)
  - EventBusBuilderExtensions

---

COMPONENT_NAME: IEventBusBuilder
TYPE: Gateway
PURPOSE: Fluent configuration builder for registering event subscriptions and JSON options.
RESPONSIBILITIES:
  - Expose `Services` property to access `IServiceCollection` for manual registration if needed
  - Support method chaining for multiple `.AddSubscription<T, TH>()` calls
  - Preserve fluent API in extension methods for clean consumer code
SOURCE: `Abstractions/IEventBusBuilder.cs`
CALLS:
  - NONE
CALLED_BY:
  - EventBusBuilderExtensions

---

COMPONENT_NAME: EventBusBuilderExtensions
TYPE: Service
PURPOSE: Provide fluent extension methods for registering event subscriptions and JSON configuration.
RESPONSIBILITIES:
  - Register event type + handler pair as keyed transient service in the DI container
  - Maintain centralized event-type registry for message broker deserialization
  - Configure JSON serialization options for AOT-safe reflection
  - Support `AddSubscription<T, TH>()` for generic event-handler pairs
  - Support `ConfigureJsonOptions(Action<JsonSerializerOptions> configure)` for customization
SOURCE: `Extensions/EventBusBuilderExtensions.cs`
CALLS:
  - IEventBusBuilder — to access `Services` property
  - IServiceCollection — to register keyed services
  - IOptions<EventBusSubscriptionInfo> — to configure event-type registry
CALLED_BY:
  - Basket.API (Extensions.cs)
  - Catalog.API (Extensions.cs)
  - Ordering.API (Extensions.cs)
  - PaymentProcessor (Program.cs)
  - Webhooks.API (Extensions.cs)

---

COMPONENT_NAME: GenericTypeExtensions
TYPE: Utility
PURPOSE: Format generic type names for readable display and logging.
RESPONSIBILITIES:
  - Convert generic type `Handler<OrderStartedIntegrationEvent>` to readable string `Handler<OrderStartedIntegrationEvent>`
  - Support both Type and object instances for reflection-based introspection
SOURCE: `Extensions/GenericTypeExtensions.cs`
CALLS:
  - NONE
CALLED_BY:
  - RabbitMQEventBus (for logging and telemetry)

## Component Call Sequences

### Use-Case: Publish Integration Event (e.g., Order Placed)

STEP 1: Ordering.API → IEventBus
  OPERATION: PublishAsync(OrderStatusChangedToAwaitingValidationIntegrationEvent)
  PURPOSE: Initiate event publication to notify dependent services that an order requires validation
  SOURCE: `src/Ordering.API/Application/DomainEventHandlers/`

STEP 2: IEventBus → RabbitMQEventBus
  OPERATION: PublishAsync(event)
  PURPOSE: Route to concrete implementation (RabbitMQ) via dependency injection
  SOURCE: `src/EventBusRabbitMQ/RabbitMQEventBus.cs`

STEP 3: RabbitMQEventBus → RabbitMQ Direct Exchange
  OPERATION: BasicPublish(exchange: "eshop_event_bus", routingKey: "OrderStatusChangedToAwaitingValidationIntegrationEvent")
  PURPOSE: Place message on RabbitMQ broker for fanout to all bound queues
  SOURCE: `src/EventBusRabbitMQ/RabbitMQEventBus.cs`

STEP 4: RabbitMQ → Catalog.API (Consumer)
  OPERATION: Dequeue message from bound queue
  PURPOSE: Deliver event to interested subscriber
  SOURCE: `src/Catalog.API/Extensions/Extensions.cs`

STEP 5: RabbitMQEventBus → IIntegrationEventHandler<OrderStatusChangedToAwaitingValidationIntegrationEvent>
  OPERATION: Invoke handler via IKeyedServiceProvider.GetKeyedService<IIntegrationEventHandler>(typeof(OrderStatusChangedToAwaitingValidationIntegrationEvent))
  PURPOSE: Execute business logic in response to event
  SOURCE: `src/EventBusRabbitMQ/RabbitMQEventBus.cs`

---

### Use-Case: Subscribe to Integration Event on Service Startup

STEP 1: Service Host → Service Extensions Configuration
  OPERATION: builder.AddRabbitMqEventBus("eventbus")
  PURPOSE: Create and return `IEventBusBuilder` instance for fluent configuration
  SOURCE: `src/*/Extensions/Extensions.cs` (each service)

STEP 2: EventBusBuilderExtensions → EventBusBuilderExtensions
  OPERATION: AddSubscription<TEvent, THandler>()
  PURPOSE: Register event type and handler implementation as keyed transient service
  SOURCE: `Extensions/EventBusBuilderExtensions.cs`

STEP 3: EventBusBuilderExtensions → EventBusSubscriptionInfo
  OPERATION: Configure event-type name-to-Type registry entry
  PURPOSE: Enable message broker to deserialize JSON events by type name at runtime
  SOURCE: `Extensions/EventBusBuilderExtensions.cs`

STEP 4: Service Host → RabbitMQEventBus → RabbitMQ
  OPERATION: Connect to RabbitMQ server and declare queues/bindings
  PURPOSE: Create durable subscription on broker side for event delivery
  SOURCE: `src/EventBusRabbitMQ/RabbitMQEventBus.cs`

## Communication Channels

| CHANNEL_TYPE | ENDPOINT / EXCHANGE / TOPIC | SOURCE | NOTES |
|---|---|---|---|
| MessageQueue | Direct exchange: `eshop_event_bus` | `src/EventBusRabbitMQ/RabbitMQEventBus.cs` | RabbitMQ default; routing key = event type name (e.g., `OrderStartedIntegrationEvent`) |
| MessageQueue | Dynamic queue binding | `src/EventBusRabbitMQ/RabbitMQEventBus.cs` | Each service instance creates a unique queue bound to exchange with event type filters |

## Dependency Registration and Wiring

| PROPERTY | VALUE |
|---|---|
| DI_CONTAINER | Microsoft.Extensions.DependencyInjection (built-in .NET) |

**Registration Method:**
METHOD: `EventBusBuilderExtensions.AddSubscription<T, TH>()`
LOCATION: `Extensions/EventBusBuilderExtensions.cs` (lines 20–37)
SCOPE: Keyed Transient

**Registration Details:**

ABSTRACTION: `IIntegrationEventHandler`
CONCRETE: `TH : class, IIntegrationEventHandler<T>`
KEY: `typeof(T)` (event type as key)
SCOPE: Transient
CODE SNIPPET:
```csharp
eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T));

eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
{
    o.EventTypes[typeof(T).Name] = typeof(T);
});
```

**Example Consumer Registration:**
```csharp
builder.AddRabbitMqEventBus("eventbus")
    .AddSubscription<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>()
    .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>()
```

## Configuration and Secrets

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|---|---|---|---|
| Options (IOptions<EventBusSubscriptionInfo>) | EventTypes (Dictionary<string, Type>) | NO | In-memory at runtime; configured via `AddSubscription<>()` |
| Options (IOptions<EventBusSubscriptionInfo>) | JsonSerializerOptions | NO | In-memory; configurable via `ConfigureJsonOptions()` |

**Configuration Method:**
JSON serialization behavior is configured via `EventBusBuilderExtensions.ConfigureJsonOptions()`:
```csharp
eventBusBuilder.ConfigureJsonOptions(options =>
{
    // Customize options (e.g., PropertyNameCaseInsensitive, WriteIndented, etc.)
});
```

**Default Behavior:**
- Reflection enabled: Uses default JSON type info resolver
- AOT mode: Uses empty type info resolver combination (no dynamic deserialization)

## Persistence and Data Access

| PROPERTY | VALUE |
|---|---|
| DATABASE | NONE |
| DATA_ACCESS | NONE |
| MIGRATIONS_PATH | NONE |
| REPOSITORY_PATTERN | NO |

**Note:** EventBus is a pure abstraction layer with no data persistence. Message persistence is handled by the broker implementation (RabbitMQ in eShop).

## Patterns and Architecture Notes

| PATTERN | EVIDENCE | SNIPPET |
|---|---|---|
| **Pub/Sub (Observer)** | Core abstraction `IEventBus.PublishAsync()` and `IIntegrationEventHandler<T>` | Decouples publishers (Ordering.API) from subscribers (Catalog.API, Webhooks.API) |
| **Keyed Dependency Injection** | Multiple handlers per event type registered with `AddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T))` | Enables `IKeyedServiceProvider.GetKeyedService<IIntegrationEventHandler>(typeof(EventType))` for multi-handler dispatch |
| **Builder Pattern** | `IEventBusBuilder` interface with fluent extension methods | Allows `.AddSubscription<>().AddSubscription<>()...` chaining for readable configuration |
| **Generic Handler** | `IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler` | Type-safe handler implementation with covariant event parameter; default method for non-generic dispatch |
| **AOT Compatibility** | `[DynamicallyAccessedMembers]`, explicit `JsonTypeInfoResolver`, and `IsAotCompatible` project property | Enables native AOT compilation without runtime code generation |
| **Configuration as Options** | `IOptions<EventBusSubscriptionInfo>` | Type-safe, statically validated event registry using MS.Extensions.Options |

**Code Snippets:**

1. **Generic Handler with Type Covariance:**
```csharp
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
    Task IIntegrationEventHandler.Handle(IntegrationEvent @event) => 
        Handle((TIntegrationEvent)@event);
}
```

2. **Keyed Subscription Registration:**
```csharp
eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler, TH>(typeof(T));
eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
{
    o.EventTypes[typeof(T).Name] = typeof(T);
});
```

3. **AOT-Safe Type Resolver:**
```csharp
internal static readonly JsonSerializerOptions DefaultSerializerOptions = new()
{
    TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault 
        ? CreateDefaultTypeResolver() 
        : JsonTypeInfoResolver.Combine()
};
```

## Security and Operational Considerations

| ASPECT | DETAILS |
|---|---|
| AUTHN_AUTHZ | No authentication/authorization at the EventBus abstraction layer. Security delegated to message broker implementation (RabbitMQ credentials, SSL/TLS). Configuration file: `src/EventBusRabbitMQ/RabbitMqDependencyInjectionExtensions.cs` |
| KNOWN_RISKS | No risks identified in abstraction layer. See `src/EventBusRabbitMQ/overview.md` for broker-specific security concerns. |
| OBSERVABILITY | Logging and OpenTelemetry instrumentation implemented in concrete broker (RabbitMQ). No built-in logging in abstraction. Tracing: `src/EventBusRabbitMQ/RabbitMQTelemetry.cs` |
| DEPLOYMENT | Abstraction layer is compiled as NuGet package (`EventBus.csproj`). No separate deployment unit. Deployed as reference in consumer services (Basket.API, Catalog.API, Ordering.API, PaymentProcessor, Webhooks.API). |

**Cross-Cutting Concerns:**
- **Event ID Correlation:** `IntegrationEvent.Id` (Guid) enables distributed tracing across microservices.
- **Timestamp Metadata:** `IntegrationEvent.CreationDate` (UTC) supports idempotency and causality tracking.
- **AOT Compatibility:** Enables deployment to environments with restricted JIT compilation (e.g., Azure Container Instances, serverless).
