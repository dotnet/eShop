# PaymentProcessor

## Overview
The PaymentProcessor is a background worker service that handles payment processing for orders. It listens for stock confirmation events and simulates payment processing, publishing success or failure events.

## Technology Stack
- **Framework**: .NET 10 Worker Service
- **Messaging**: RabbitMQ via EventBus

## Purpose
When an order's stock is confirmed by the Catalog service, the PaymentProcessor:
1. Receives the `OrderStatusChangedToStockConfirmedIntegrationEvent`
2. Processes the payment (simulated in this reference implementation)
3. Publishes either `OrderPaymentSucceededIntegrationEvent` or `OrderPaymentFailedIntegrationEvent`

## Key Components

### Event Handler
- `OrderStatusChangedToStockConfirmedIntegrationEventHandler`
- Receives stock confirmation events
- Simulates payment processing
- Publishes payment result events

## Event Integration

### Subscribed Events
| Event | Handler | Action |
|-------|---------|--------|
| `OrderStatusChangedToStockConfirmedIntegrationEvent` | Handler | Processes payment for the order |

### Published Events
| Event | Trigger |
|-------|---------|
| `OrderPaymentSucceededIntegrationEvent` | Payment processing succeeds |
| `OrderPaymentFailedIntegrationEvent` | Payment processing fails |

## Project Structure
```
PaymentProcessor/
├── IntegrationEvents/
│   ├── Events/
│   │   ├── OrderPaymentSucceededIntegrationEvent.cs
│   │   ├── OrderPaymentFailedIntegrationEvent.cs
│   │   └── OrderStatusChangedToStockConfirmedIntegrationEvent.cs
│   └── EventHandling/
│       └── OrderStatusChangedToStockConfirmedIntegrationEventHandler.cs
├── Extensions.cs            # Service configuration
├── Program.cs               # Application entry point
└── appsettings.json         # Configuration
```

## Workflow
```
1. Catalog.API confirms stock availability
2. Ordering.API publishes OrderStatusChangedToStockConfirmedIntegrationEvent
3. PaymentProcessor receives event
4. PaymentProcessor simulates payment:
   - Success → publishes OrderPaymentSucceededIntegrationEvent
   - Failure → publishes OrderPaymentFailedIntegrationEvent
5. Ordering.API receives payment result
6. Order status updated to Paid or Cancelled
```

## Payment Simulation
In this reference implementation, the PaymentProcessor always succeeds. In a real application, this would:
- Integrate with payment gateways (Stripe, PayPal, etc.)
- Validate payment information
- Handle retries and failures
- Manage refunds

## Dependencies
- **RabbitMQ**: For receiving and publishing integration events
- **eShop.ServiceDefaults**: Shared service configuration

## Configuration
Key settings in `appsettings.json`:
- RabbitMQ connection (via Aspire service discovery)

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.

## Notes
- Stateless worker that can be scaled horizontally
- In production, implement actual payment gateway integration
- Consider idempotency for payment processing
- Handle payment timeouts and retries gracefully
