# Project Structure

## Solution Organization

The eShop solution follows a clean, domain-driven structure with clear separation between services, shared libraries, and tests.

## Root Directory Layout

```
eShop/
├── src/                    # Source code (microservices & libraries)
├── tests/                  # All test projects
├── e2e/                    # End-to-end Playwright tests
├── artifacts/              # Build outputs (bin, obj, publish)
├── build/                  # Build scripts and configurations
├── img/                    # Documentation images
├── .github/                # GitHub Actions workflows
├── .kiro/                  # Kiro AI assistant configuration
├── eShop.slnx             # Main solution file
├── eShop.Web.slnf         # Web-focused solution filter
└── global.json            # .NET SDK version pinning
```

## Source Structure (`src/`)

### Microservices (APIs)

Each microservice is self-contained with its own database and domain logic:

- **`Basket.API/`** - Shopping basket service
  - `Model/` - Domain models (BasketItem, CustomerBasket)
  - `Repositories/` - Data access (Redis-based)
  - `Grpc/` - gRPC service definitions
  - `IntegrationEvents/` - Event handlers and events
  - `Proto/` - Protobuf definitions

- **`Catalog.API/`** - Product catalog service
  - `Apis/` - Minimal API endpoint definitions
  - `Model/` - Domain models
  - `Infrastructure/` - EF Core DbContext and configurations
  - `Services/` - Business logic
  - `IntegrationEvents/` - Event publishing
  - `Pics/` - Product images

- **`Ordering.API/`** - Order management service
  - `Apis/` - API endpoints
  - `Application/` - Application layer (commands, queries)
  - `Infrastructure/` - Data access and external integrations
  - Uses separate domain projects (see below)

- **`Identity.API/`** - Authentication/authorization service
  - `Configuration/` - IdentityServer configuration
  - `Data/` - Identity database context
  - `Models/` - Identity models
  - `Views/` - Login/consent UI (Razor views)
  - `wwwroot/` - Static assets

- **`Webhooks.API/`** - Webhook management service
  - `Apis/` - Webhook endpoints
  - `Model/` - Webhook subscriptions and events
  - `Services/` - Webhook delivery logic
  - `Infrastructure/` - Database context

### Background Services

- **`OrderProcessor/`** - Background worker for order processing
  - `Events/` - Event handlers
  - `Services/` - Processing logic

- **`PaymentProcessor/`** - Background worker for payment processing
  - `IntegrationEvents/` - Payment event handlers

### Web Applications

- **`WebApp/`** - Main customer-facing web application (Blazor Server)
  - `Components/` - Blazor components
  - `Services/` - HTTP clients for APIs
  - `wwwroot/` - Static assets

- **`WebhookClient/`** - Webhook testing client
  - `Components/` - UI components
  - `Endpoints/` - Webhook receiver endpoints

### Domain Libraries (Ordering)

- **`Ordering.Domain/`** - Core domain logic
  - `AggregatesModel/` - Domain aggregates (Order, Buyer)
  - `Events/` - Domain events
  - `SeedWork/` - Base classes (Entity, ValueObject)
  - `Exceptions/` - Domain exceptions

- **`Ordering.Infrastructure/`** - Infrastructure concerns
  - `EntityConfigurations/` - EF Core mappings
  - `Repositories/` - Repository implementations
  - `Migrations/` - Database migrations
  - `Idempotency/` - Idempotent command handling

### Shared Libraries

- **`eShop.ServiceDefaults/`** - Common service configuration
  - Service discovery setup
  - OpenTelemetry configuration
  - Authentication extensions
  - OpenAPI configuration

- **`EventBus/`** - Event bus abstraction
  - `Abstractions/` - Interfaces (IEventBus, IIntegrationEventHandler)
  - `Events/` - Base event classes
  - `Extensions/` - DI extensions

- **`EventBusRabbitMQ/`** - RabbitMQ implementation
  - RabbitMQ-specific event bus implementation

- **`IntegrationEventLogEF/`** - Event sourcing support
  - Outbox pattern implementation
  - Event log persistence

- **`WebAppComponents/`** - Shared Blazor components
  - Reusable UI components across web apps

- **`Shared/`** - Cross-cutting utilities
  - Activity extensions
  - Database migration helpers

### Orchestration

- **`eShop.AppHost/`** - .NET Aspire orchestration
  - `Program.cs` - Service composition and configuration
  - `Extensions.cs` - Custom Aspire extensions
  - Defines all service dependencies and relationships

## Test Structure (`tests/`)

### Unit Tests
- **`Basket.UnitTests/`** - Basket service unit tests
- **`Ordering.UnitTests/`** - Ordering domain and application tests
  - `Domain/` - Domain model tests
  - `Application/` - Command/query handler tests

### Functional/Integration Tests
- **`Catalog.FunctionalTests/`** - Catalog API integration tests
- **`Ordering.FunctionalTests/`** - Ordering API integration tests

### BDD Tests
- **`Catalog.BddTests/`** - Behavior-driven tests
  - `Features/` - Gherkin feature files
  - `StepDefinitions/` - Step implementations
  - `Drivers/` - Test drivers
  - `Support/` - Test infrastructure

### E2E Tests (`e2e/`)
- Playwright-based browser tests
- `*.spec.ts` - Test specifications
- `login.setup.ts` - Authentication setup

## Common Project Patterns

### Typical API Project Structure
```
{Service}.API/
├── Apis/                   # Endpoint definitions
├── Application/            # Application layer (optional)
├── Model/                  # Domain models
├── Infrastructure/         # Data access, external services
├── Services/               # Business logic
├── IntegrationEvents/      # Event handlers and events
├── Extensions/             # Service registration extensions
├── Program.cs              # Application entry point
├── appsettings.json        # Configuration
└── GlobalUsings.cs         # Global using directives
```

### Configuration Files

Each project typically includes:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `Properties/launchSettings.json` - Launch profiles
- `GlobalUsings.cs` - Common using statements

## Naming Conventions

### Projects
- APIs: `{Domain}.API` (e.g., `Catalog.API`)
- Domain: `{Domain}.Domain` (e.g., `Ordering.Domain`)
- Infrastructure: `{Domain}.Infrastructure`
- Tests: `{Domain}.{TestType}Tests` (e.g., `Catalog.FunctionalTests`)

### Folders
- PascalCase for all folder names
- Plural for collections (e.g., `Models/`, `Services/`, `Apis/`)
- Singular for single-purpose folders (e.g., `Infrastructure/`, `Application/`)

### Files
- PascalCase for C# files matching class names
- `Program.cs` - Application entry point
- `GlobalUsings.cs` - Global using directives
- `Extensions.cs` - Extension methods

## Key Architectural Patterns

### Microservices
- Each service owns its database
- Communication via HTTP/gRPC and async messaging (RabbitMQ)
- Service discovery via .NET Aspire

### Domain-Driven Design (Ordering)
- Separate Domain, Application, and Infrastructure layers
- Aggregates, Entities, Value Objects in Domain
- Repository pattern for data access
- Domain events for side effects

### Event-Driven Architecture
- Integration events for cross-service communication
- Outbox pattern for reliable event publishing
- Event handlers in `IntegrationEvents/EventHandling/`

### Backend for Frontend (BFF)
- YARP reverse proxy (`mobile-bff`)
- Aggregates multiple backend services
- Simplifies mobile client integration

### Minimal APIs
- Endpoint definitions in `Apis/` folders
- Extension methods for route registration
- Example: `MapCatalogApi()`, `MapOrderingApi()`

## Build Artifacts

The `artifacts/` directory contains:
- `bin/` - Compiled binaries
- `obj/` - Intermediate build files
- `publish/` - Published applications

This is managed by `UseArtifactsOutput` in `Directory.Build.props`.

## Configuration Hierarchy

1. `Directory.Build.props` - Global build properties
2. `Directory.Packages.props` - Centralized package versions
3. `tests/Directory.Build.props` - Test-specific properties
4. Individual `.csproj` files - Project-specific settings

## Adding New Services

When adding a new microservice:
1. Create project in `src/{Service}.API/`
2. Add to `eShop.slnx`
3. Register in `eShop.AppHost/Program.cs`
4. Add database if needed (via Aspire)
5. Configure service defaults via `AddServiceDefaults()`
6. Create corresponding test project in `tests/`
