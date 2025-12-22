# EventBus

## Purpose
Abstraction layer defining interfaces for event-driven messaging between microservices.

## Key Interfaces
- `IEventBus` - Publish integration events
- `IIntegrationEventHandler<T>` - Handle specific event types
- `IntegrationEvent` - Base class for all events

## Usage
All microservices depend on this project for event publishing/subscribing abstractions. The actual implementation is in EventBusRabbitMQ.

## Pattern
Implements the Event Bus pattern for loose coupling between services.
