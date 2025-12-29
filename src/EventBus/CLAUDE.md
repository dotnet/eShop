# EventBus

## Overview
The EventBus project defines the abstractions and base types for event-driven communication between microservices in the eShop application. It provides a transport-agnostic interface for publishing and subscribing to integration events.

## Purpose
- Define common interfaces for event bus implementations
- Provide base `IntegrationEvent` type
- Enable loose coupling between services
- Support multiple transport implementations (RabbitMQ, Azure Service Bus, etc.)

## Key Components

### Interfaces

#### IEventBus
Main interface for publishing events:
```csharp
public interface IEventBus
{
    Task PublishAsync(IntegrationEvent @event);
}
```

#### IIntegrationEventHandler<T>
Interface for event handlers:
```csharp
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);
}
```

#### IEventBusBuilder
Builder interface for configuring subscriptions:
```csharp
public interface IEventBusBuilder
{
    IEventBusBuilder AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;
}
```

### Base Types

#### IntegrationEvent
Base class for all integration events:
```csharp
public record IntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime CreationDate { get; } = DateTime.UtcNow;
}
```

## Project Structure
```
EventBus/
├── Abstractions/
│   ├── IEventBus.cs              # Publish interface
│   ├── IIntegrationEventHandler.cs  # Handler interface
│   └── IEventBusBuilder.cs       # Builder interface
├── Events/
│   └── IntegrationEvent.cs       # Base event type
└── Extensions/
    └── EventBusBuilderExtensions.cs  # Extension methods
```

## Usage Pattern

### Publishing Events
```csharp
public class CatalogService
{
    private readonly IEventBus _eventBus;

    public async Task UpdatePrice(int productId, decimal newPrice)
    {
        // Update product...

        // Publish event
        await _eventBus.PublishAsync(
            new ProductPriceChangedIntegrationEvent(productId, newPrice)
        );
    }
}
```

### Subscribing to Events
```csharp
// In service configuration
builder.AddRabbitMqEventBus("eventbus")
    .AddSubscription<ProductPriceChangedIntegrationEvent,
                     ProductPriceChangedHandler>();
```

### Implementing Handlers
```csharp
public class ProductPriceChangedHandler
    : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    public async Task Handle(ProductPriceChangedIntegrationEvent @event)
    {
        // Handle the event
        Console.WriteLine($"Product {event.ProductId} price changed to {event.NewPrice}");
    }
}
```

## Design Principles

### Transport Agnostic
- Abstractions don't depend on specific message broker
- Implementations can be swapped (RabbitMQ ↔ Azure Service Bus)

### Event Sourcing Ready
- Events have unique IDs and timestamps
- Enables event replay and auditing

### Loose Coupling
- Publishers don't know about subscribers
- Subscribers don't know about publishers
- Services communicate through events

## Dependencies
- No external dependencies (pure abstractions)
- Referenced by: All services, `EventBusRabbitMQ`

## Implementations
- **EventBusRabbitMQ**: RabbitMQ implementation (see EventBusRabbitMQ project)
- Azure Service Bus implementation can be added following the same pattern

## Notes
- This is an abstraction layer only
- Actual message sending/receiving is in implementation projects
- Follow the interface when creating custom implementations
