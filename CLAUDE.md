# eShop Reference Application

## Overview

eShop is a reference .NET application implementing a cloud-native, microservices-based e-commerce platform. It demonstrates modern architecture patterns including Domain-Driven Design (DDD), CQRS, Event-Driven Architecture, and containerized deployment using .NET Aspire.

## Technology Stack

| Category | Technology |
|----------|------------|
| **Runtime** | .NET 10 |
| **Orchestration** | .NET Aspire 13 |
| **Web Framework** | ASP.NET Core, Blazor Server |
| **Database** | PostgreSQL (with pgvector for AI) |
| **Cache** | Redis |
| **Message Broker** | RabbitMQ |
| **Authentication** | Duende IdentityServer (OAuth2/OIDC) |
| **ORM** | Entity Framework Core 10 |
| **CQRS/Mediator** | MediatR |
| **Observability** | OpenTelemetry |

## Architecture Diagram

```
                                    ┌─────────────────────────────────────────────────────────────┐
                                    │                        CLIENTS                               │
                                    │    ┌──────────┐    ┌──────────┐    ┌──────────────────┐     │
                                    │    │  WebApp  │    │   MAUI   │    │  Webhook Client  │     │
                                    │    │ (Blazor) │    │  (Mobile)│    │                  │     │
                                    │    └────┬─────┘    └────┬─────┘    └────────┬─────────┘     │
                                    └─────────┼───────────────┼───────────────────┼───────────────┘
                                              │               │                   │
                                              └───────────────┼───────────────────┘
                                                              │
                                    ┌─────────────────────────┼─────────────────────────────────────┐
                                    │                  IDENTITY (OAuth2/OIDC)                       │
                                    │                    ┌────┴────┐                                │
                                    │                    │Identity │                                │
                                    │                    │   API   │                                │
                                    │                    └─────────┘                                │
                                    └───────────────────────────────────────────────────────────────┘
                                                              │
              ┌───────────────────────────────────────────────┼───────────────────────────────────────────────┐
              │                                        API GATEWAY                                             │
              │         ┌─────────────────────┬───────────────┴───────────────┬─────────────────────┐         │
              │         ▼                     ▼                               ▼                     ▼         │
              │   ┌───────────┐         ┌───────────┐                   ┌───────────┐         ┌───────────┐   │
              │   │  Basket   │         │  Catalog  │                   │ Ordering  │         │ Webhooks  │   │
              │   │   API     │         │   API     │                   │   API     │         │   API     │   │
              │   └─────┬─────┘         └─────┬─────┘                   └─────┬─────┘         └─────┬─────┘   │
              │         │                     │                               │                     │         │
              └─────────┼─────────────────────┼───────────────────────────────┼─────────────────────┼─────────┘
                        │                     │                               │                     │
              ┌─────────┼─────────────────────┼───────────────────────────────┼─────────────────────┼─────────┐
              │         │              DATA STORES                            │                     │         │
              │         ▼                     ▼                               ▼                     ▼         │
              │   ┌───────────┐         ┌───────────┐                   ┌───────────┐         ┌───────────┐   │
              │   │   Redis   │         │ PostgreSQL│                   │ PostgreSQL│         │ PostgreSQL│   │
              │   │  (cache)  │         │(catalogdb)│                   │(orderingdb)│        │(webhooksdb)│  │
              │   └───────────┘         └───────────┘                   └───────────┘         └───────────┘   │
              └───────────────────────────────────────────────────────────────────────────────────────────────┘

              ┌───────────────────────────────────────────────────────────────────────────────────────────────┐
              │                                      EVENT BUS (RabbitMQ)                                      │
              │                                                                                                │
              │   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐  │
              │   │  Ordering   │◄──►│   Catalog   │◄──►│   Basket    │◄──►│  Payment    │◄──►│   Order     │  │
              │   │    API      │    │    API      │    │    API      │    │  Processor  │    │  Processor  │  │
              │   └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘  │
              │                                                                                                │
              └───────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
eShop/
├── src/
│   ├── eShop.AppHost/           # .NET Aspire orchestration host
│   ├── eShop.ServiceDefaults/   # Shared service configuration
│   │
│   ├── Basket.API/              # Shopping cart microservice
│   ├── Catalog.API/             # Product catalog microservice
│   ├── Ordering.API/            # Order management microservice
│   ├── Ordering.Domain/         # DDD domain models
│   ├── Ordering.Infrastructure/ # Data access layer
│   ├── Identity.API/            # OAuth2/OIDC authentication
│   │
│   ├── OrderProcessor/          # Background worker (grace period)
│   ├── PaymentProcessor/        # Background worker (payments)
│   │
│   ├── WebApp/                  # Blazor Server frontend
│   ├── WebAppComponents/        # Shared Blazor components
│   ├── Webhooks.API/            # Webhook management
│   ├── WebhookClient/           # Webhook demo client
│   │
│   ├── EventBus/                # Event bus abstractions
│   ├── EventBusRabbitMQ/        # RabbitMQ implementation
│   └── IntegrationEventLogEF/   # Transactional outbox pattern
│
├── tests/                       # Unit and integration tests
├── e2e/                         # Playwright E2E tests
└── docs/                        # Documentation
```

## Microservices

| Service | Purpose | Database | Documentation |
|---------|---------|----------|---------------|
| **Basket.API** | Shopping cart management | Redis | [CLAUDE.md](src/Basket.API/CLAUDE.md) |
| **Catalog.API** | Product catalog with AI search | PostgreSQL + pgvector | [CLAUDE.md](src/Catalog.API/CLAUDE.md) |
| **Ordering.API** | Order lifecycle management | PostgreSQL | [CLAUDE.md](src/Ordering.API/CLAUDE.md) |
| **Identity.API** | Authentication & authorization | PostgreSQL | [CLAUDE.md](src/Identity.API/CLAUDE.md) |
| **Webhooks.API** | External notifications | PostgreSQL | [CLAUDE.md](src/Webhooks.API/CLAUDE.md) |
| **OrderProcessor** | Grace period background job | - | [CLAUDE.md](src/OrderProcessor/CLAUDE.md) |
| **PaymentProcessor** | Payment processing | - | [CLAUDE.md](src/PaymentProcessor/CLAUDE.md) |

## Key Patterns

### Domain-Driven Design (DDD)
- **Aggregates**: Order, Buyer in Ordering domain
- **Value Objects**: Address, OrderStatus
- **Domain Events**: OrderStarted, OrderShipped, etc.
- **Repository Pattern**: IOrderRepository, IBuyerRepository
- Documentation: [Ordering.Domain/CLAUDE.md](src/Ordering.Domain/CLAUDE.md)

### CQRS (Command Query Responsibility Segregation)
- **Commands**: CreateOrderCommand, CancelOrderCommand
- **Queries**: GetOrdersFromUserQuery, GetOrderByIdQuery
- **Mediator**: MediatR for dispatching
- Documentation: [Ordering.API/CLAUDE.md](src/Ordering.API/CLAUDE.md)

### Event-Driven Architecture
- **Integration Events**: Cross-service communication via RabbitMQ
- **Domain Events**: In-process events via MediatR
- **Transactional Outbox**: Reliable event publishing
- Documentation: [EVENT-DRIVEN-ARCHITECTURE.md](docs/EVENT-DRIVEN-ARCHITECTURE.md)

## Order Processing Flow

```
1. User checkouts basket → Ordering.API creates order
2. OrderStartedEvent → Basket.API clears cart
3. Grace period expires → OrderProcessor triggers validation
4. Catalog.API validates stock → Confirms or rejects
5. PaymentProcessor processes payment → Succeeds or fails
6. Order status updated → Notifications sent
```

See [EVENT-DRIVEN-ARCHITECTURE.md](docs/EVENT-DRIVEN-ARCHITECTURE.md) for detailed saga diagram.

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- Visual Studio 2022 / VS Code / Rider

### Running the Application

```bash
# Clone the repository
git clone <repository-url>

# Navigate to the AppHost project
cd src/eShop.AppHost

# Run with .NET Aspire
dotnet run
```

This starts:
- All microservices
- Infrastructure (PostgreSQL, Redis, RabbitMQ)
- Aspire Dashboard for monitoring

### Accessing the Application
- **WebApp**: https://localhost:{port} (see Aspire Dashboard)
- **Aspire Dashboard**: https://localhost:18888

## Deployment

### Azure Container Apps
```bash
# Install Azure Developer CLI
winget install Microsoft.Azd

# Initialize and deploy
azd init
azd up
```

## Service Documentation

Each service has its own CLAUDE.md with detailed information:

| Project | Documentation |
|---------|---------------|
| eShop.AppHost | [CLAUDE.md](src/eShop.AppHost/CLAUDE.md) |
| eShop.ServiceDefaults | [CLAUDE.md](src/eShop.ServiceDefaults/CLAUDE.md) |
| Basket.API | [CLAUDE.md](src/Basket.API/CLAUDE.md) |
| Catalog.API | [CLAUDE.md](src/Catalog.API/CLAUDE.md) |
| Ordering.API | [CLAUDE.md](src/Ordering.API/CLAUDE.md) |
| Ordering.Domain | [CLAUDE.md](src/Ordering.Domain/CLAUDE.md) |
| Ordering.Infrastructure | [CLAUDE.md](src/Ordering.Infrastructure/CLAUDE.md) |
| Identity.API | [CLAUDE.md](src/Identity.API/CLAUDE.md) |
| OrderProcessor | [CLAUDE.md](src/OrderProcessor/CLAUDE.md) |
| PaymentProcessor | [CLAUDE.md](src/PaymentProcessor/CLAUDE.md) |
| Webhooks.API | [CLAUDE.md](src/Webhooks.API/CLAUDE.md) |
| WebApp | [CLAUDE.md](src/WebApp/CLAUDE.md) |
| EventBus | [CLAUDE.md](src/EventBus/CLAUDE.md) |
| EventBusRabbitMQ | [CLAUDE.md](src/EventBusRabbitMQ/CLAUDE.md) |
| IntegrationEventLogEF | [CLAUDE.md](src/IntegrationEventLogEF/CLAUDE.md) |

## Additional Documentation

- [Event-Driven Architecture](docs/EVENT-DRIVEN-ARCHITECTURE.md) - Integration events, sagas, and messaging patterns

## Development Guidelines

### Adding a New Microservice
1. Create new ASP.NET Core project
2. Reference `eShop.ServiceDefaults`
3. Add to `eShop.AppHost` orchestration
4. Configure database and event bus subscriptions
5. Create CLAUDE.md documentation

### Adding New Integration Events
1. Define event record inheriting `IntegrationEvent`
2. Create handler implementing `IIntegrationEventHandler<T>`
3. Register subscription in service Extensions
4. Use outbox pattern for reliability

### Code Conventions
- Follow .NET naming conventions
- Use records for events and DTOs
- Implement CQRS for complex domains
- Add OpenTelemetry instrumentation
