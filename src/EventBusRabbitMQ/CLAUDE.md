# EventBusRabbitMQ

## Purpose
RabbitMQ implementation of the EventBus abstraction. Handles message serialization, routing, and delivery.

## Tech Stack
- RabbitMQ.Client
- System.Text.Json for serialization

## Key Components
- `RabbitMQEventBus` - IEventBus implementation
- Message serialization/deserialization
- Connection management with retry logic
- Subscription management

## Configuration
Configured via Aspire service discovery. Connection string provided by AppHost.

## Features
- Automatic reconnection
- Message acknowledgment
- Dead letter handling
- Subscription persistence
