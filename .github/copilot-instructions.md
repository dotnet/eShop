# AI Coding Agent Instructions for eShop

This document provides essential knowledge for AI agents to be productive in the `eShop` codebase. Focus on the following key aspects:

## Architecture Overview
- **Microservices**: The codebase is structured as microservices (e.g., `Basket.API`, `Catalog.API`, `Ordering.API`). Each service has its own `appsettings.json`, `Program.cs`, and domain logic.
- **Event-Driven Communication**: Services communicate via events (e.g., `IntegrationEvents` folder in `Basket.API`). RabbitMQ is used for event bus (`EventBusRabbitMQ`).
- **Shared Components**: `eShop.ServiceDefaults` contains shared configurations (logging, resilience, etc.) used across services.

## Developer Workflows
- **Build**: Run `dotnet build` from the root (`eShop.slnx`). For Docker builds, check `build/acr-build/queue-all.ps1`.
- **Testing**: Unit tests are in `tests/` (e.g., `Ordering.UnitTests`). Functional tests use Playwright (`e2e/`).
- **Debugging**: Use `appsettings.Development.json` for local overrides. Services often include `Program.Testing.cs` for test configurations.

## Project-Specific Conventions
- **Global Usings**: Each service has a `GlobalUsings.cs` file for shared namespaces.
- **Event Sourcing**: Check `IntegrationEventLogEF` for event sourcing patterns.
- **API Design**: APIs follow REST conventions (e.g., `Catalog.API/Apis/`).

## Integration Points
- **External Dependencies**: RabbitMQ (`EventBusRabbitMQ`), EF Core (`IntegrationEventLogEF`).
- **Cross-Service Calls**: gRPC is used for inter-service communication (e.g., `Basket.API/Grpc/`).

## Key Files for Reference
- `src/Basket.API/Program.cs`: Example of service setup.
- `src/Catalog.API/CatalogOptions.cs`: Configuration patterns.
- `tests/Ordering.FunctionalTests/`: Functional testing approach.

---
**Note**: Update this document as patterns evolve. Focus on actionable insights, not aspirational practices.