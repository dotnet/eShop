# eShop Reference Application

## Overview
A reference .NET application implementing an e-commerce website using a services-based microservices architecture with .NET Aspire.

## Tech Stack
- **.NET 10** with C# 13
- **.NET Aspire 13** for orchestration and service discovery
- **PostgreSQL** with pgvector for databases
- **Redis** for caching
- **RabbitMQ** for event-driven messaging
- **Entity Framework Core** for data access
- **Blazor** for web UI

## Project Structure
```
src/
├── eShop.AppHost/          # Aspire orchestrator (startup project)
├── eShop.ServiceDefaults/  # Shared Aspire configurations
├── Basket.API/             # Shopping basket microservice
├── Catalog.API/            # Product catalog microservice
├── Identity.API/           # Authentication/authorization service
├── Ordering.API/           # Order management API
├── Ordering.Domain/        # Domain models for ordering
├── Ordering.Infrastructure/# Data access for ordering
├── OrderProcessor/         # Background order processing
├── PaymentProcessor/       # Payment handling service
├── WebApp/                 # Main Blazor web application
├── WebAppComponents/       # Shared Blazor components
├── Webhooks.API/           # Webhook management service
├── WebhookClient/          # Webhook consumer client
├── EventBus/               # Event bus abstractions
├── EventBusRabbitMQ/       # RabbitMQ implementation
├── IntegrationEventLogEF/  # Integration event logging
├── ClientApp/              # Mobile/desktop client
└── HybridApp/              # MAUI hybrid application
tests/
├── Basket.UnitTests/
├── Catalog.FunctionalTests/
├── ClientApp.UnitTests/
├── Ordering.FunctionalTests/
└── Ordering.UnitTests/
```

## Running Locally
```powershell
# Ensure Docker Desktop is running
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```
Or use the provided `runLocal.ps1` script.

## Key Patterns
- **Clean Architecture**: Domain/Infrastructure/API separation in Ordering service
- **CQRS**: Command Query Responsibility Segregation in Ordering
- **Event-Driven**: Integration events via RabbitMQ
- **Domain-Driven Design**: Rich domain models in Ordering.Domain

## Databases
- `catalogdb` - Product catalog (PostgreSQL with pgvector)
- `identitydb` - User authentication data
- `orderingdb` - Orders and related data
- `webhooksdb` - Webhook subscriptions

## Configuration
- Use `appsettings.json` for local settings
- Use User Secrets for sensitive data (connection strings, API keys)
- OpenAI/Ollama integration available via flags in AppHost Program.cs

## Creating New Services (Claude Commands)

### New React UI Service
```
/new-react-ui Admin
```
Creates a React frontend with:
- Vite + React + TypeScript
- shadcn/ui components
- TanStack Query (data fetching)
- TanStack Router (routing)
- Tailwind CSS

### New Backend Service
```
/new-backend-service Inventory
```
Creates Clean Architecture projects:
- {Name}.API - ASP.NET Core Minimal APIs with MediatR
- {Name}.Domain - Entities, Value Objects, Domain Events
- {Name}.Infrastructure - EF Core, Repositories

### New DB Migrator
```
/new-db-migrator Shipping
```
Creates a DB Migrator for existing services.

## Available Commands
- `/new-react-ui [Name]` - Create React UI with shadcn + TanStack
- `/new-backend-service [Name]` - Create Clean Architecture backend
- `/new-db-migrator [Name]` - Create DB Migrator project

## Scripts
- `runLocal.ps1` - Run Aspire host locally
