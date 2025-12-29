# Event-Driven Architecture in eShop

## Overview

The eShop application uses an event-driven architecture to enable loose coupling between microservices. This document describes the event-driven patterns, integration events, and communication flows used throughout the system.

## Two Levels of Events

The eShop architecture implements two distinct types of events:

### 1. Domain Events (In-Process)
- **Scope**: Within a single bounded context/service
- **Transport**: MediatR (in-memory)
- **Purpose**: Decouple domain logic within a service
- **Location**: `Ordering.Domain/Events/`

### 2. Integration Events (Cross-Service)
- **Scope**: Between different microservices
- **Transport**: RabbitMQ message broker
- **Purpose**: Enable asynchronous communication between services
- **Location**: Each service's `IntegrationEvents/` folder

## Event Bus Architecture

### Abstraction Layer (`EventBus/`)

```
┌─────────────────────────────────────────┐
│              IEventBus                   │
│  - PublishAsync(IntegrationEvent)        │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│        IIntegrationEventHandler<T>       │
│  - Handle(T @event)                      │
└─────────────────────────────────────────┘
```

### RabbitMQ Implementation (`EventBusRabbitMQ/`)

```
┌─────────────┐     ┌───────────────────┐     ┌─────────────┐
│  Publisher  │────▶│  RabbitMQ Exchange │────▶│  Consumer   │
│  Service    │     │  (Direct Type)     │     │  Service    │
└─────────────┘     └───────────────────┘     └─────────────┘
                           │
                    Routing Key = Event Type Name
```

**Exchange Configuration:**
- Type: Direct
- Name: `eshop_event_bus`
- Durable: Yes

**Queue Configuration:**
- One queue per subscribing service
- Named by service identifier
- Bound with event type routing keys

## Integration Events Catalog

### Order Lifecycle Events

| Event | Publisher | Subscribers | Description |
|-------|-----------|-------------|-------------|
| `OrderStartedIntegrationEvent` | Ordering.API | Basket.API | Order created, triggers basket cleanup |
| `OrderStatusChangedToSubmittedIntegrationEvent` | Ordering.API | WebApp | Order submitted |
| `OrderStatusChangedToAwaitingValidationIntegrationEvent` | Ordering.API | Catalog.API, WebApp | Ready for stock validation |
| `OrderStatusChangedToStockConfirmedIntegrationEvent` | Ordering.API | PaymentProcessor, WebApp | Stock confirmed, ready for payment |
| `OrderStatusChangedToPaidIntegrationEvent` | Ordering.API | Catalog.API, Webhooks.API, WebApp | Payment successful |
| `OrderStatusChangedToShippedIntegrationEvent` | Ordering.API | Webhooks.API, WebApp | Order shipped |
| `OrderStatusChangedToCancelledIntegrationEvent` | Ordering.API | WebApp | Order cancelled |

### Stock Events

| Event | Publisher | Subscribers | Description |
|-------|-----------|-------------|-------------|
| `OrderStockConfirmedIntegrationEvent` | Catalog.API | Ordering.API | All items in stock |
| `OrderStockRejectedIntegrationEvent` | Catalog.API | Ordering.API | Insufficient stock |

### Payment Events

| Event | Publisher | Subscribers | Description |
|-------|-----------|-------------|-------------|
| `OrderPaymentSucceededIntegrationEvent` | PaymentProcessor | Ordering.API | Payment processed |
| `OrderPaymentFailedIntegrationEvent` | PaymentProcessor | Ordering.API | Payment failed |

### Grace Period Events

| Event | Publisher | Subscribers | Description |
|-------|-----------|-------------|-------------|
| `GracePeriodConfirmedIntegrationEvent` | OrderProcessor | Ordering.API | Grace period expired |

### Product Events

| Event | Publisher | Subscribers | Description |
|-------|-----------|-------------|-------------|
| `ProductPriceChangedIntegrationEvent` | Catalog.API | Webhooks.API | Product price updated |

## Order Processing Saga

The order processing workflow demonstrates the event-driven saga pattern:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              ORDER PROCESSING SAGA                               │
└─────────────────────────────────────────────────────────────────────────────────┘

Step 1: Order Creation
┌──────────────┐                                      ┌──────────────┐
│   WebApp     │──POST /orders─────────────────────▶ │ Ordering.API │
└──────────────┘                                      └──────┬───────┘
                                                             │
                                     OrderStartedIntegrationEvent
                                                             │
                                                             ▼
                                                      ┌──────────────┐
                                                      │  Basket.API  │
                                                      │ (clears cart)│
                                                      └──────────────┘

Step 2: Grace Period (User can cancel)
┌────────────────┐                                    ┌──────────────┐
│ OrderProcessor │──(polls database)─────────────────▶│ Ordering.API │
│  (background)  │                                    │(status=Submit)│
└───────┬────────┘                                    └──────────────┘
        │
        │ (after grace period expires)
        │
        ▼
GracePeriodConfirmedIntegrationEvent
        │
        ▼
┌──────────────┐
│ Ordering.API │──▶ status = AwaitingValidation
└──────────────┘

Step 3: Stock Validation
┌──────────────┐                                      ┌──────────────┐
│ Ordering.API │                                      │  Catalog.API │
└──────┬───────┘                                      └──────▲───────┘
       │                                                     │
       │  OrderStatusChangedToAwaitingValidationIntegrationEvent
       └─────────────────────────────────────────────────────┘
                                                             │
                               ┌─────────────────────────────┤
                               ▼                             ▼
                 OrderStockConfirmedEvent        OrderStockRejectedEvent
                               │                             │
                               ▼                             ▼
                    ┌──────────────┐              ┌──────────────┐
                    │ Ordering.API │              │ Ordering.API │
                    │(StockConfirm)│              │ (Cancelled)  │
                    └──────────────┘              └──────────────┘

Step 4: Payment Processing
┌──────────────┐                                     ┌──────────────────┐
│ Ordering.API │                                     │ PaymentProcessor │
└──────┬───────┘                                     └────────▲─────────┘
       │                                                      │
       │  OrderStatusChangedToStockConfirmedIntegrationEvent  │
       └──────────────────────────────────────────────────────┘
                                                              │
                               ┌──────────────────────────────┤
                               ▼                              ▼
                 OrderPaymentSucceededEvent       OrderPaymentFailedEvent
                               │                              │
                               ▼                              ▼
                    ┌──────────────┐              ┌──────────────┐
                    │ Ordering.API │              │ Ordering.API │
                    │   (Paid)     │              │ (Cancelled)  │
                    └──────────────┘              └──────────────┘

Step 5: Final Notifications
┌──────────────┐
│ Ordering.API │
│   (Paid)     │
└──────┬───────┘
       │
       │  OrderStatusChangedToPaidIntegrationEvent
       │
       ├─────────────────────────────┬────────────────────────┐
       ▼                             ▼                        ▼
┌──────────────┐            ┌──────────────┐          ┌──────────────┐
│  Catalog.API │            │ Webhooks.API │          │    WebApp    │
│(update stock)│            │  (notify)    │          │ (update UI)  │
└──────────────┘            └──────────────┘          └──────────────┘
```

## Transactional Outbox Pattern

The eShop uses the Transactional Outbox pattern to ensure reliable event publishing:

```
┌────────────────────────────────────────────────────────────────┐
│                         SERVICE                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    TRANSACTION                           │   │
│  │  1. Save Business Data  ─────▶  orders table            │   │
│  │  2. Save Event Log      ─────▶  IntegrationEventLog     │   │
│  │  3. Commit Transaction                                   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              AFTER COMMIT                                │   │
│  │  4. Publish to RabbitMQ                                  │   │
│  │  5. Mark event as Published                              │   │
│  └─────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────┘
```

**Event States:**
1. `NotPublished` - Saved but not yet sent
2. `InProgress` - Currently being published
3. `Published` - Successfully sent to broker
4. `PublishedFailed` - Failed after retries

**Benefits:**
- Atomicity: Events saved with business data
- Reliability: No lost events on failure
- Idempotency: Event IDs prevent duplicates

## Event Handler Registration

Services register their subscriptions during startup:

```csharp
// Ordering.API/Extensions/Extensions.cs
private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
{
    eventBus.AddSubscription<GracePeriodConfirmedIntegrationEvent,
                             GracePeriodConfirmedIntegrationEventHandler>();
    eventBus.AddSubscription<OrderStockConfirmedIntegrationEvent,
                             OrderStockConfirmedIntegrationEventHandler>();
    eventBus.AddSubscription<OrderStockRejectedIntegrationEvent,
                             OrderStockRejectedIntegrationEventHandler>();
    eventBus.AddSubscription<OrderPaymentFailedIntegrationEvent,
                             OrderPaymentFailedIntegrationEventHandler>();
    eventBus.AddSubscription<OrderPaymentSucceededIntegrationEvent,
                             OrderPaymentSucceededIntegrationEventHandler>();
}
```

## Creating New Integration Events

### 1. Define the Event

```csharp
// IntegrationEvents/Events/MyNewIntegrationEvent.cs
public record MyNewIntegrationEvent(
    int EntityId,
    string Description
) : IntegrationEvent;
```

### 2. Create the Handler

```csharp
// IntegrationEvents/EventHandling/MyNewIntegrationEventHandler.cs
public class MyNewIntegrationEventHandler
    : IIntegrationEventHandler<MyNewIntegrationEvent>
{
    public async Task Handle(MyNewIntegrationEvent @event)
    {
        // Handle the event
        _logger.LogInformation(
            "Handling {EventName} for entity {EntityId}",
            nameof(MyNewIntegrationEvent),
            @event.EntityId
        );
    }
}
```

### 3. Register the Subscription

```csharp
// Extensions.cs
eventBus.AddSubscription<MyNewIntegrationEvent, MyNewIntegrationEventHandler>();
```

### 4. Publish the Event

```csharp
// In your service
await _eventBus.PublishAsync(new MyNewIntegrationEvent(
    EntityId: entity.Id,
    Description: "Something happened"
));
```

## OpenTelemetry Integration

The event bus includes distributed tracing:

```
┌──────────────────────────────────────────────────────────────┐
│                     TRACE PROPAGATION                         │
│                                                               │
│  Publisher                   RabbitMQ              Consumer   │
│  ┌─────────┐                ┌─────────┐           ┌─────────┐ │
│  │ Span A  │──traceparent──▶│ Message │──────────▶│ Span B  │ │
│  │(publish)│    header      │ Headers │           │(consume)│ │
│  └─────────┘                └─────────┘           └─────────┘ │
│      │                                                 │      │
│      └────────────── Parent-Child Link ────────────────┘      │
└──────────────────────────────────────────────────────────────┘
```

## Resilience Patterns

### Publisher Retry Policy
```csharp
Policy.Handle<BrokerUnreachableException>()
    .Or<SocketException>()
    .WaitAndRetry(
        retryCount: 10,
        sleepDurationProvider: attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))
    );
```

### Consumer Error Handling
- Failed messages are requeued for retry
- Dead letter exchange for poison messages
- Configurable retry limits

## Service Communication Matrix

| Service | Publishes | Subscribes To |
|---------|-----------|---------------|
| **Ordering.API** | OrderStarted, OrderStatusChanged* (6) | GracePeriodConfirmed, OrderStock*, OrderPayment* |
| **Catalog.API** | OrderStockConfirmed/Rejected, ProductPriceChanged | OrderStatusChangedToAwaitingValidation, OrderStatusChangedToPaid |
| **Basket.API** | - | OrderStarted |
| **PaymentProcessor** | OrderPaymentSucceeded/Failed | OrderStatusChangedToStockConfirmed |
| **OrderProcessor** | GracePeriodConfirmed | - |
| **Webhooks.API** | - | ProductPriceChanged, OrderStatusChangedToShipped/Paid |
| **WebApp** | - | OrderStatusChanged* (6) |

## Best Practices

1. **Event Naming**: Use past tense (`OrderStarted`, not `StartOrder`)
2. **Event Content**: Include only necessary data, avoid large payloads
3. **Idempotency**: Handlers should be idempotent (safe to process twice)
4. **Versioning**: Consider event versioning for backward compatibility
5. **Monitoring**: Use OpenTelemetry for observability
6. **Outbox Pattern**: Use for critical business events
7. **Dead Letters**: Implement dead letter queues for failed messages
