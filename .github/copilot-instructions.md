# eShop AI Coding Agent Instructions

## Architecture Overview

This is a .NET 9 microservices e-commerce reference application built with **[.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)**. The architecture follows event-driven patterns with service-to-service communication via RabbitMQ event bus.

### Core Services
- **Catalog.API**: Product catalog with AI-powered semantic search using vector embeddings
- **Basket.API**: Shopping cart service backed by Redis
- **Ordering.API**: Order processing with CQRS/Event Sourcing patterns (see `src/Ordering.API/Application/`)
- **Identity.API**: Authentication service with OpenID Connect
- **WebApp**: Blazor Server UI consuming all APIs

### Key Infrastructure
- **eShop.AppHost**: Aspire orchestrator defining all service dependencies and configuration
- **eShop.ServiceDefaults**: Shared infrastructure (OpenTelemetry, health checks, service discovery)
- **EventBus**: Custom event bus abstraction with RabbitMQ implementation
- **Mobile BFF**: YARP reverse proxy for mobile clients (see `Extensions.cs` routing config)

## Development Workflows

### Running the Application
```powershell
# Primary method - starts entire distributed app
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Test configuration (HTTP endpoints for CI)
$env:ESHOP_USE_HTTP_ENDPOINTS=1; dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

### Testing Strategy
- **Unit Tests**: `tests/*UnitTests/` - Standard xUnit tests. Using XUnit framework is COMPULSORY
- **Functional Tests**: `tests/*FunctionalTests/` - Use Aspire test containers (require Docker)
- **E2E Tests**: `e2e/*.spec.ts` - Playwright tests with automatic app startup
- **Test Command**: `dotnet test` (functional tests need Docker running)

### AI/ML Integration
- **Embedding Generation**: Catalog items automatically get vector embeddings for semantic search
- **OpenAI/Azure OpenAI**: Toggle `useOpenAI = true` in `src/eShop.AppHost/Program.cs`
- **Ollama Support**: Toggle `useOllama = true` for local LLM inference

## Project-Specific Patterns

### API Design
- **Minimal APIs**: All APIs use ASP.NET Core minimal APIs pattern (see `src/Catalog.API/Apis/CatalogApi.cs`)
- **API Versioning**: v1/v2 endpoints with query parameter versioning (`?api-version=2.0`)
- **Typed Results**: Use `Results<T1, T2>` for multiple return types
- **Service Pattern**: APIs use `[AsParameters] CatalogServices services` dependency injection

### Service Registration
```csharp
// Standard pattern in each API's Extensions.cs
builder.AddServiceDefaults();           // Common infrastructure
builder.AddApplicationServices();       // API-specific services  
builder.AddDefaultOpenApi(withApiVersioning);
```

### Event-Driven Architecture
- **Integration Events**: Cross-service events in `IntegrationEvents/` folders
- **Domain Events**: Internal service events using MediatR (Ordering service)
- **Event Subscription**: Use `AddSubscription<TEvent, THandler>()` in service registration
- **Transactional Outbox**: Events saved with entity changes for guaranteed delivery

### Database Patterns
- **PostgreSQL**: Primary database with pgvector extension for embeddings
- **Entity Framework**: Code-first with automatic migrations in development
- **Context Seeding**: Each service has `*ContextSeed.cs` for sample data
- **Health Checks**: Database connectivity automatically monitored

### Configuration Management
- **Aspire Configuration**: Service dependencies and connection strings in `AppHost/Program.cs`
- **Service Discovery**: Automatic service-to-service communication via Aspire
- **Environment Variables**: Identity URLs and feature flags injected via Aspire

## File Organization

### Service Structure Template
```
src/[Service].API/
├── Program.cs              # Minimal startup
├── Extensions/
│   └── Extensions.cs       # Service registration
├── Apis/
│   └── [Service]Api.cs     # Minimal API endpoints
├── Model/                  # Data models
├── IntegrationEvents/      # Cross-service events
└── Services/              # Business logic
```

### Shared Libraries
- **eShop.ServiceDefaults**: Import this in every service for consistent infrastructure
- **EventBus**: Import for event publishing/subscribing
- **IntegrationEventLogEF**: Import for transactional outbox pattern

### Testing Structure
```
tests/[Service].FunctionalTests/
├── [Service]ApiTests.cs    # API integration tests
└── TestFixture.cs          # Aspire test host setup
```

## Common Issues & Solutions

### Docker Requirements
- **Aspire Dependency**: All functional tests require Docker Desktop running
- **Container Lifetime**: Use `ContainerLifetime.Persistent` for databases to avoid recreation

### Service Communication
- **Internal vs External**: Use `GetEndpoint("http")` for service-to-service, external endpoints for UI
- **Health Check Dependencies**: Use `.WaitFor()` to ensure proper startup order
- **Identity Integration**: All services need `Identity__Url` environment variable

### AI Features Debug
- **Embedding Issues**: Check `CatalogAI.IsEnabled` and vector extension installation
- **Semantic Search**: Falls back to name search when AI is disabled
- **Performance**: Embedding generation happens synchronously on item creation/update

### Build Configuration
- **Solution Files**: Use `eShop.Web.slnf` (web focus) or `eShop.slnx` (full solution)
- **Global Settings**: `Directory.Build.props` enables warnings as errors for all projects
- **SDK Version**: Requires .NET 9 preview SDK (see `global.json`)