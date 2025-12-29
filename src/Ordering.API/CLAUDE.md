# Ordering.API

## Overview
The Ordering API is the core order management microservice in the eShop application. It handles order creation, status management, and orchestrates the order processing workflow through domain events and integration events.

## Technology Stack
- **Framework**: ASP.NET Core 10
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10
- **CQRS/Mediator**: MediatR
- **Validation**: FluentValidation
- **Architecture**: Domain-Driven Design (DDD)

## Key Components

### API Endpoints
- `GET /api/v1/orders` - Get orders for current user
- `GET /api/v1/orders/{orderId}` - Get order by ID
- `GET /api/v1/orders/cardtypes` - Get available card types
- `POST /api/v1/orders` - Create new order (from basket checkout)
- `PUT /api/v1/orders/cancel` - Cancel order
- `PUT /api/v1/orders/ship` - Ship order

### Commands (CQRS)
- `CreateOrderCommand` - Creates a new order
- `CancelOrderCommand` - Cancels an order
- `ShipOrderCommand` - Marks order as shipped
- `SetStockConfirmedOrderStatusCommand` - Updates status after stock confirmation
- `SetPaidOrderStatusCommand` - Updates status after payment
- `SetAwaitingValidationOrderStatusCommand` - Sets awaiting validation status

### Queries
- `GetOrdersFromUserQuery` - Get all orders for a user
- `GetOrderByIdQuery` - Get specific order details
- `GetCardTypesQuery` - Get payment card types

## Event Integration

### Subscribed Events
| Event | Handler | Action |
|-------|---------|--------|
| `GracePeriodConfirmedIntegrationEvent` | Handler | Transitions order from submitted to awaiting validation |
| `OrderStockConfirmedIntegrationEvent` | Handler | Updates order status to stock confirmed |
| `OrderStockRejectedIntegrationEvent` | Handler | Cancels order due to insufficient stock |
| `OrderPaymentSucceededIntegrationEvent` | Handler | Updates order status to paid |
| `OrderPaymentFailedIntegrationEvent` | Handler | Cancels order due to payment failure |

### Published Events
| Event | Trigger |
|-------|---------|
| `OrderStartedIntegrationEvent` | When order is created |
| `OrderStatusChangedToAwaitingValidationIntegrationEvent` | When order moves to awaiting validation |
| `OrderStatusChangedToStockConfirmedIntegrationEvent` | When stock is confirmed |
| `OrderStatusChangedToPaidIntegrationEvent` | When payment succeeds |
| `OrderStatusChangedToShippedIntegrationEvent` | When order is shipped |
| `OrderStatusChangedToCancelledIntegrationEvent` | When order is cancelled |
| `OrderStatusChangedToSubmittedIntegrationEvent` | When order is submitted |

## Order Status Flow
```
Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped
                    ↓                   ↓           ↓
                Cancelled           Cancelled   Cancelled
```

## Dependencies
- **PostgreSQL**: Order database
- **Ordering.Domain**: Domain models and events
- **Ordering.Infrastructure**: Data access layer
- **Identity.API**: For authentication
- **RabbitMQ**: For event bus communication

## Project Structure
```
Ordering.API/
├── Application/
│   ├── Behaviors/           # MediatR pipeline behaviors
│   ├── Commands/            # CQRS commands
│   ├── DomainEventHandlers/ # Domain event handlers
│   ├── IntegrationEvents/   # Integration events
│   │   ├── Events/          # Event definitions
│   │   └── EventHandling/   # Event handlers
│   ├── Models/              # DTOs and view models
│   ├── Queries/             # CQRS queries
│   └── Validations/         # FluentValidation validators
├── Infrastructure/
│   ├── Migrations/          # EF Core migrations
│   └── Services/            # Infrastructure services
├── Extensions.cs            # Service configuration
└── Program.cs               # Application entry point
```

## Key Services
- `OrderingIntegrationEventService` - Manages integration event publishing with outbox pattern
- `IdentityService` - Extracts user identity from JWT

## Configuration
Key settings:
- PostgreSQL connection (via Aspire)
- Identity API URL
- RabbitMQ connection for event bus

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.
