# Basket.API

## Overview
The Basket API is a microservice responsible for managing shopping baskets/carts in the eShop application. It provides both REST and gRPC endpoints for basket operations.

## Technology Stack
- **Framework**: ASP.NET Core 10
- **Data Store**: Redis (in-memory cache)
- **Communication**: REST API + gRPC
- **Authentication**: JWT Bearer tokens

## Key Components

### API Endpoints
- `GET /api/v1/basket/{buyerId}` - Get basket by buyer ID
- `POST /api/v1/basket` - Update/create basket
- `DELETE /api/v1/basket/{buyerId}` - Delete basket
- `POST /api/v1/basket/checkout` - Checkout basket

### gRPC Service
- Protocol definition: `Protos/basket.proto`
- Enables service-to-service communication with lower latency

### Models
- `CustomerBasket` - Main basket entity containing items
- `BasketItem` - Individual item in the basket

## Event Integration

### Subscribed Events
| Event | Handler | Action |
|-------|---------|--------|
| `OrderStartedIntegrationEvent` | `OrderStartedIntegrationEventHandler` | Clears the user's basket after order is created |

### Published Events
None - Basket.API is primarily a consumer of events.

## Dependencies
- **Redis**: For basket data persistence
- **Identity.API**: For authentication
- **RabbitMQ**: For event bus communication

## Project Structure
```
Basket.API/
├── Grpc/                    # gRPC service implementations
├── IntegrationEvents/       # Event handlers
│   └── EventHandling/       # Integration event handlers
├── Model/                   # Domain models
├── Protos/                  # gRPC protocol definitions
├── Extensions.cs            # Service configuration
└── Program.cs               # Application entry point
```

## Configuration
Key settings in `appsettings.json`:
- Redis connection string (via Aspire service discovery)
- Identity API URL for authentication
- RabbitMQ connection for event bus

## Running Locally
This service is orchestrated by `eShop.AppHost` via .NET Aspire. Run the AppHost project to start all services with proper configuration.
