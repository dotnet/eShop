# eShop – Project Overview & Onboarding Guide

## Business Purpose

**eShop** is a reference .NET application implementing a fully functional e-commerce website ("AdventureWorks"). It demonstrates modern **microservices architecture** patterns using **.NET Aspire** and serves as a best-practice blueprint for building scalable, cloud-ready applications.

---

## Main Features

| Feature | Description |
|---|---|
| Product Catalog | Browse, search, and filter products with AI-powered recommendations |
| Shopping Basket | Cart management backed by Redis |
| Order Processing | Full order lifecycle with grace period and state machine |
| Payment Processing | Event-driven payment handling |
| Authentication | OpenID Connect / JWT via Duende IdentityServer |
| Webhooks | External system integration via webhook subscriptions |
| Web UI | Blazor Interactive Server web application |
| Mobile App | Cross-platform .NET MAUI application |
| AI Integration | Product recommendations via OpenAI / Azure OpenAI / Ollama |

---

## Architecture Overview

This is a **true microservices architecture** orchestrated by **.NET Aspire**.

```
┌─────────────────────────────────────────────────────────────┐
│                      eShop.AppHost                          │
│              (.NET Aspire Orchestration Host)                │
└──────────────────────────┬──────────────────────────────────┘
                           │ Manages
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
│ Identity.API │  │  Catalog.API │  │   Basket.API     │
│  (AuthN/Z)   │  │  (Products)  │  │  (Cart + Redis)  │
└──────────────┘  └──────────────┘  └──────────────────┘
        ┌──────────────────┬──────────────────┐
        ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
│ Ordering.API │  │ Webhooks.API │  │  OrderProcessor  │
│  (Orders)    │  │  (Webhooks)  │  │ PaymentProcessor │
└──────────────┘  └──────────────┘  └──────────────────┘
        ┌──────────────────┬──────────────────┐
        ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
│   WebApp     │  │  HybridApp   │  │  WebhookClient   │
│  (Blazor)    │  │   (MAUI)     │  │  (Test Client)   │
└──────────────┘  └──────────────┘  └──────────────────┘

Infrastructure: PostgreSQL + pgvector | Redis | RabbitMQ
```

### Key Architecture Patterns

- **CQRS** – Commands/Queries separated via MediatR (see `Ordering.API/Application/`)
- **Domain-Driven Design (DDD)** – Aggregates, Value Objects, Domain Events in `Ordering.Domain`
- **Event-Driven** – Integration events published over RabbitMQ between services
- **Transactional Outbox** – `IntegrationEventLogEF` ensures reliable event delivery
- **Repository Pattern** – Abstracted data access in `Ordering.Infrastructure`
- **gRPC** – High-performance communication for Basket service
- **Idempotency** – Duplicate request detection via `IdentifiedCommand<T>`

---

## Key Technologies & Frameworks

| Category | Technology | Version |
|---|---|---|
| Runtime | .NET SDK | 10.0 |
| Orchestration | .NET Aspire | 13.1.0 |
| Web Framework | ASP.NET Core / Blazor | 10.0 |
| Mobile | .NET MAUI | Latest |
| CQRS / Mediator | MediatR | 13.0.0 |
| ORM | Entity Framework Core | 10.0.1 |
| Database | PostgreSQL + pgvector | – |
| Cache | Redis | – |
| Message Bus | RabbitMQ | – |
| Identity | Duende IdentityServer | 7.3.2 |
| gRPC | gRPC.AspNetCore | 2.71.0 |
| Validation | FluentValidation | 12.0.0 |
| Observability | OpenTelemetry | 1.14.0 |
| API Versioning | Asp.Versioning | 8.1.0 |
| API Docs | Scalar / OpenAPI | 2.8.6 |
| AI | Azure OpenAI / Ollama | Beta |
| Testing | MSTest + Playwright | – |

---

## Repository Structure

```
eShop/
├── src/
│   ├── eShop.AppHost/            # ▶ PRIMARY ENTRY POINT – Aspire orchestration
│   ├── eShop.ServiceDefaults/    # Shared service config (health, auth, OpenAPI)
│   ├── Identity.API/             # Authentication & authorization
│   ├── Catalog.API/              # Product catalog + AI recommendations
│   ├── Basket.API/               # Shopping cart (Redis + gRPC)
│   ├── Ordering.API/             # Order management (CQRS, REST)
│   ├── Ordering.Domain/          # DDD aggregates & domain events
│   ├── Ordering.Infrastructure/  # EF Core persistence & repositories
│   ├── Webhooks.API/             # External webhook management
│   ├── OrderProcessor/           # Background: order grace period state machine
│   ├── PaymentProcessor/         # Background: payment event handling
│   ├── WebApp/                   # Blazor Interactive Server frontend
│   ├── WebAppComponents/         # Shared Razor component library
│   ├── HybridApp/                # .NET MAUI mobile app
│   ├── ClientApp/                # Mobile client app
│   ├── WebhookClient/            # Webhook test client
│   ├── EventBus/                 # Event bus abstraction
│   ├── EventBusRabbitMQ/         # RabbitMQ event bus implementation
│   └── IntegrationEventLogEF/    # Outbox pattern implementation
├── tests/
│   ├── Basket.UnitTests/
│   ├── Ordering.UnitTests/
│   ├── Ordering.FunctionalTests/
│   ├── Catalog.FunctionalTests/
│   └── ClientApp.UnitTests/
├── e2e/                          # Playwright end-to-end tests
├── docs/                         # Documentation
├── .github/workflows/            # CI/CD pipelines
├── eShop.slnx                    # Full solution
├── eShop.Web.slnf                # Web projects solution filter
├── Directory.Packages.props      # Centralized NuGet package versions
└── global.json                   # SDK version pin
```

---

## Important Entry Points & Components

### Starting the Application

```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

The **Aspire Dashboard** URL is printed to the console — use it to monitor all services, traces, logs, and metrics.

### Key Files

| File | Purpose |
|---|---|
| `src/eShop.AppHost/Program.cs` | Service composition – wires all microservices, databases, and message brokers |
| `src/WebApp/Program.cs` | Blazor web app startup |
| `src/Ordering.API/Application/` | CQRS commands, queries, handlers, and MediatR pipeline behaviors |
| `src/Ordering.Domain/AggregatesModel/` | DDD aggregates: `OrderAggregate`, `BuyerAggregate` |
| `src/EventBusRabbitMQ/RabbitMQEventBus.cs` | Event bus implementation |
| `src/eShop.ServiceDefaults/Extensions.cs` | Common extensions applied to all services |
| `Directory.Packages.props` | Central place to update NuGet dependency versions |

### Key API Endpoints

| Service | Endpoint | Description |
|---|---|---|
| Ordering.API | `POST /api/orders/` | Create a new order |
| Ordering.API | `GET /api/orders/{id}` | Get order by ID |
| Ordering.API | `PUT /api/orders/cancel` | Cancel an order |
| Ordering.API | `PUT /api/orders/ship` | Ship an order |
| Catalog.API | `GET /api/catalog/items` | List catalog items |
| Basket.API | gRPC `BasketService` | Manage shopping basket |
| Identity.API | `/.well-known/openid-configuration` | OIDC discovery endpoint |

---

## CI/CD

| Workflow | Trigger | What it does |
|---|---|---|
| `pr-validation.yml` | PR / push to `main` | Build + test all web projects |
| `pr-validation-maui.yml` | PR / push to `main` | Build + test MAUI app |
| `playwright.yml` | Scheduled / manual | End-to-end browser tests |
| `markdownlint.yml` | PR | Lint documentation |

---

## Deployment

```bash
# Azure (via Azure Developer CLI)
azd init
azd up
```

Aspire auto-detects all services and provisions Azure Container Apps, PostgreSQL, Redis, and Service Bus.
