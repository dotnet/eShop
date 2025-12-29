# EventBusRabbitMQ

## Overview
The EventBusRabbitMQ project implements the `IEventBus` interface using RabbitMQ as the message broker. It provides reliable event publishing and subscription with built-in resilience and observability.

## Technology Stack
- **Message Broker**: RabbitMQ
- **Client Library**: RabbitMQ.Client
- **Resilience**: Polly
- **Observability**: OpenTelemetry

## Key Components

### RabbitMQEventBus
Main implementation of `IEventBus`:
- Publishes events to RabbitMQ exchange
- Manages subscriptions and consumers
- Handles connection management
- Implements resilience policies

### Key Features
- **Direct Exchange**: Events routed by type name
- **Durable Queues**: Messages survive broker restart
- **JSON Serialization**: Events serialized as JSON
- **Retry Policies**: Automatic retry on failure
- **Distributed Tracing**: OpenTelemetry integration

## Project Structure
```
EventBusRabbitMQ/
├── RabbitMQEventBus.cs          # Main implementation
├── RabbitMQTelemetry.cs         # OpenTelemetry integration
├── EventBusOptions.cs           # Configuration options
└── Extensions/
    └── RabbitMQDependencyInjection.cs  # DI extensions
```

## Configuration

### Registration
```csharp
// In service configuration
builder.AddRabbitMqEventBus("eventbus")
    .AddSubscription<OrderStartedIntegrationEvent, OrderStartedHandler>()
    .AddSubscription<ProductPriceChangedIntegrationEvent, PriceChangedHandler>();
```

### Options
```csharp
public class EventBusOptions
{
    public string SubscriptionClientName { get; set; }  // Queue name
    public int RetryCount { get; set; } = 10;           // Retry attempts
}
```

## RabbitMQ Topology

### Exchange
- **Type**: Direct
- **Name**: `eshop_event_bus`
- **Durable**: Yes

### Queues
- Named by subscription client name
- One queue per service
- Bound to exchange with event type routing keys

### Routing
- Routing key = Event type name
- Example: `OrderStartedIntegrationEvent`

## Message Flow
```
Publisher                    RabbitMQ                      Subscriber
    |                           |                              |
    |--Publish(Event)---------->|                              |
    |                           |--Route by type name--------->|
    |                           |                              |--Handle(Event)
    |                           |<--------------Ack------------|
```

## Resilience

### Publisher Retry
```csharp
Policy.Handle<BrokerUnreachableException>()
    .Or<SocketException>()
    .WaitAndRetry(retryCount, attempt =>
        TimeSpan.FromSeconds(Math.Pow(2, attempt)));
```

### Consumer Retry
- Events requeued on handler failure
- Dead letter exchange for poison messages

## OpenTelemetry Integration

### Tracing
- Activity created for each publish/consume
- Trace context propagated in message headers
- Parent-child relationship maintained across services

### Metrics
- Message publish count
- Message consume count
- Error counts

### Headers
```
traceparent: 00-{trace-id}-{span-id}-01
tracestate: (optional state)
```

## Message Format
```json
{
  "Id": "550e8400-e29b-41d4-a716-446655440000",
  "CreationDate": "2024-01-15T10:30:00Z",
  // Event-specific properties...
}
```

## Dependencies
- **EventBus**: Abstract interfaces
- **RabbitMQ.Client**: RabbitMQ client library
- **Polly**: Resilience policies
- **OpenTelemetry**: Distributed tracing
- **System.Text.Json**: JSON serialization

## Usage Example
```csharp
// Publishing
public class OrderService
{
    private readonly IEventBus _eventBus;

    public async Task CreateOrder(Order order)
    {
        // Save order...

        await _eventBus.PublishAsync(new OrderStartedIntegrationEvent(
            order.Id,
            order.BuyerId
        ));
    }
}

// Subscribing
public class BasketOrderStartedHandler
    : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        await _basketService.ClearBasket(@event.BuyerId);
    }
}
```

## Notes
- Connection managed as singleton
- Channels created per-operation for thread safety
- Consider Azure Service Bus for production cloud deployments
