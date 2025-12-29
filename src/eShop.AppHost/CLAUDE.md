# eShop.AppHost

## Overview
The eShop.AppHost is the .NET Aspire orchestration host for the eShop application. It defines and configures all services, dependencies, and infrastructure components for local development and deployment.

## Technology Stack
- **Framework**: .NET Aspire 13
- **Orchestration**: Aspire App Model
- **Containers**: Docker (for infrastructure)

## Purpose
- Orchestrates all microservices
- Manages service discovery
- Configures infrastructure (databases, message brokers)
- Provides unified development experience
- Enables deployment to Azure Container Apps

## Key Components

### Infrastructure Resources
| Resource | Type | Description |
|----------|------|-------------|
| `rabbitmq` | RabbitMQ | Message broker for event bus |
| `postgres` | PostgreSQL | Database server |
| `redis` | Redis | In-memory cache for baskets |
| `catalogdb` | PostgreSQL Database | Catalog data with pgvector |
| `identitydb` | PostgreSQL Database | Identity/auth data |
| `orderingdb` | PostgreSQL Database | Order data |
| `webhooksdb` | PostgreSQL Database | Webhook subscriptions |

### Services
| Service | Type | Description |
|---------|------|-------------|
| `identity-api` | API | OAuth2/OIDC authentication |
| `basket-api` | API | Shopping cart management |
| `catalog-api` | API | Product catalog |
| `ordering-api` | API | Order management |
| `order-processor` | Worker | Grace period processing |
| `payment-processor` | Worker | Payment processing |
| `webhooks-api` | API | Webhook management |
| `webapp` | Web | Blazor frontend |
| `webhooks-client` | Web | Webhook demo client |

## Project Structure
```
eShop.AppHost/
├── Program.cs               # Service composition
├── appsettings.json         # Configuration
├── aspire-manifest.json     # Deployment manifest
└── eShop.AppHost.csproj     # Project file
```

## Service Composition (Program.cs)
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var rabbitmq = builder.AddRabbitMQ("eventbus");
var redis = builder.AddRedis("redis");
var postgres = builder.AddPostgres("postgres");

// Databases
var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderingDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");

// Identity API
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb);

// Microservices with dependencies...
```

## Service Discovery
Aspire provides automatic service discovery:
- Services reference each other by name
- URLs resolved at runtime
- No hardcoded endpoints needed

## Environment Variables
Aspire injects connection strings and service URLs:
- `ConnectionStrings__redis`
- `ConnectionStrings__eventbus`
- `services__catalog-api__https__0`

## Running the Application
```bash
cd src/eShop.AppHost
dotnet run
```

This starts:
1. All infrastructure containers
2. All microservices
3. Aspire Dashboard for monitoring

## Aspire Dashboard
Access at: `https://localhost:port`
- View all services and their status
- Distributed tracing
- Logs aggregation
- Metrics visualization

## Deployment
```bash
# Deploy to Azure
azd init
azd up
```

Deploys to:
- Azure Container Apps (services)
- Azure PostgreSQL (databases)
- Azure Redis Cache
- Azure Service Bus (instead of RabbitMQ)

## Dependencies
- **.NET Aspire 13**: Orchestration framework
- **Docker**: For local infrastructure
- All service projects

## Configuration
```json
{
  "Parameters": {
    "postgres-password": "...",
    "rabbitmq-password": "..."
  }
}
```

## Notes
- All services start together with proper dependency order
- Health checks ensure services are ready
- OpenTelemetry configured for all services
- Single command to run entire application
