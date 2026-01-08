# Project Organization & Structure

## Solution Structure
The eShop solution follows a microservices architecture with clear separation of concerns:

```
eShop/
├── src/                          # Source code
│   ├── eShop.AppHost/           # Aspire orchestration host
│   ├── eShop.ServiceDefaults/   # Shared service configuration
│   ├── [Service].API/           # Individual microservices
│   ├── [Service].Domain/        # Domain models (DDD)
│   ├── [Service].Infrastructure/ # Data access & external integrations
│   ├── WebApp/                  # Blazor web application
│   ├── ClientApp/               # .NET MAUI mobile app
│   ├── HybridApp/               # Blazor Hybrid app
│   └── WebAppComponents/        # Shared Blazor components
├── tests/                       # Test projects
├── e2e/                         # Playwright end-to-end tests
└── [Solution Files]             # Build configuration
```

## Core Services Architecture

### Microservices
- **Basket.API**: Shopping cart management
- **Catalog.API**: Product catalog and search
- **Identity.API**: Authentication and user management
- **Ordering.API**: Order processing and management
- **Webhooks.API**: Webhook notifications
- **OrderProcessor**: Background order processing
- **PaymentProcessor**: Payment handling

### Shared Libraries
- **eShop.ServiceDefaults**: Common service configuration (health checks, telemetry, resilience)
- **EventBus**: Event-driven messaging abstractions
- **EventBusRabbitMQ**: RabbitMQ implementation
- **IntegrationEventLogEF**: Event sourcing with Entity Framework
- **WebAppComponents**: Reusable Blazor components

### Client Applications
- **WebApp**: Main e-commerce web application (Blazor Server)
- **ClientApp**: Cross-platform mobile app (.NET MAUI)
- **HybridApp**: Hybrid web/native app (Blazor Hybrid)
- **WebhookClient**: Webhook testing client

## Project Naming Conventions

### Services
- **[Domain].API**: REST API services (e.g., `Catalog.API`)
- **[Domain].Domain**: Domain models and business logic
- **[Domain].Infrastructure**: Data access and external integrations
- **[Purpose]Processor**: Background processing services

### Tests
- **[Service].UnitTests**: Unit tests for specific services
- **[Service].FunctionalTests**: Integration/functional tests
- **ClientApp.UnitTests**: Mobile app unit tests

## Configuration Files

### Solution Level
- **eShop.slnx**: Main solution file
- **eShop.Web.slnf**: Filtered solution for web-only development
- **Directory.Build.props**: Global MSBuild properties
- **Directory.Build.targets**: Global MSBuild targets
- **Directory.Packages.props**: Centralized package version management
- **global.json**: .NET SDK version specification
- **nuget.config**: NuGet package sources

### Development
- **.editorconfig**: Code style enforcement
- **playwright.config.ts**: E2E test configuration
- **package.json**: Node.js dependencies for testing

## Service Project Structure
Each API service follows a consistent internal structure:

```
[Service].API/
├── Apis/                        # Minimal API endpoints
├── Application/                 # Application services (CQRS)
├── Extensions/                  # Service registration extensions
├── Infrastructure/              # Data access implementations
├── IntegrationEvents/           # Event handlers and events
├── Model/                       # DTOs and view models
├── Properties/                  # Launch settings
├── Program.cs                   # Application entry point
├── appsettings.json            # Configuration
└── [Service].API.csproj        # Project file
```

## Aspire Orchestration
The **eShop.AppHost** project orchestrates all services:
- Service discovery and communication
- Infrastructure dependencies (PostgreSQL, Redis, RabbitMQ)
- Environment configuration
- Health check coordination
- Development-time service management

## Domain-Driven Design Patterns
Services follow DDD principles where applicable:
- **Domain Layer**: Core business logic and entities
- **Application Layer**: Use cases and application services
- **Infrastructure Layer**: Data persistence and external services
- **API Layer**: HTTP endpoints and controllers

## Event-Driven Architecture
- **Integration Events**: Cross-service communication via RabbitMQ
- **Domain Events**: In-process events within service boundaries
- **Event Handlers**: Asynchronous event processing
- **Event Store**: Persistent event logging with EF Core

## Testing Structure
- **Unit Tests**: Fast, isolated tests with mocking
- **Functional Tests**: Service-level integration tests
- **E2E Tests**: Full application workflow tests with Playwright
- **Test Utilities**: Shared test infrastructure and helpers

## Build & Deployment
- **Central Package Management**: Version consistency across projects
- **MSBuild SDK**: Modern project file format
- **Docker Support**: Containerization for all services
- **Azure Developer CLI**: Cloud deployment automation
- **GitHub Actions**: CI/CD pipeline support