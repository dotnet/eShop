# eShop Reference Application
Multi-service cloud-native .NET microservices e-commerce platform with CQRS ordering, event-driven architecture, and distributed orchestration via .NET Aspire.

---

## Summary

- **System purpose**: Enterprise-grade e-commerce platform demonstrating cloud-native architecture with microservices, CQRS, domain-driven design, and event-driven integration.
- **Services**: 17 components (5 APIs, 2 workers, 2 Blazor frontends, 8 shared libraries, 1 orchestrator).
- **Communication styles**: Synchronous (HTTP REST, gRPC), asynchronous (RabbitMQ events), identity services (OAuth2/OIDC).
- **Deployment model**: Microservices with containerized orchestration via .NET Aspire.
- **Key pattern**: Order processing uses multi-step saga with event-driven state transitions across multiple services.

---

## Technology Stack

RUNTIME:
- .NET 10.0.100
- C#
- ASP.NET Core minimal APIs

API_FRAMEWORK:
- REST (OpenAPI/Swagger)
- gRPC (Protocol Buffers)
- YARP (reverse proxy)
- Asp.Versioning

DATABASE:
- PostgreSQL (ankane/pgvector image)
- pgvector (semantic search embeddings)
- Entity Framework Core 10.0
- Npgsql

MESSAGE_BROKER:
- RabbitMQ (AMQP 0.9.1)
- MediatR (CQRS dispatch)

CACHE:
- Redis

FRONTEND:
- Blazor Server
- Razor Components

IDENTITY:
- Duende IdentityServer 7
- ASP.NET Identity
- JWT Bearer tokens
- OpenID Connect Authorization Code flow

OBSERVABILITY:
- OpenTelemetry (traces, metrics, logs)
- W3C Trace Context headers
- Structured logging

ORCHESTRATION:
- .NET Aspire 13.1.0

TESTING:
- Playwright
- Microsoft.Testing.Platform
- xUnit

RESILIENCE:
- Polly (exponential backoff, retry policies)
- OpenTelemetry tracing in RabbitMQ publisher

OPTIONAL_AI:
- OpenAI API
- Azure OpenAI
- Ollama (local LLM)

---

## Services

### SERVICE_NAME: Basket.API

TYPE: API

PURPOSE: Shopping cart CRUD operations and cart-clearing on order placement; manages basket state with TTL expiration.

OVERVIEW_REF: NONE

ENTRY_POINT: src/Basket.API/Program.cs

EXPOSES:
  - PROTOCOL: gRPC
    ENDPOINT_OR_TOPIC: /basket.Basket/GetBasket, /basket.Basket/UpdateBasket, /basket.Basket/DeleteBasket
    DESCRIPTION: Cart state management (GetBasket, UpdateBasket, DeleteBasket with BasketItem list)

CONSUMES:
  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.started
    FROM_SERVICE: Ordering.API
    DESCRIPTION: Clears customer basket after successful order placement

---

### SERVICE_NAME: Catalog.API

TYPE: API

PURPOSE: Product catalog browsing, semantic search with pgvector embeddings, and inventory stock validation for orders.

OVERVIEW_REF: NONE

ENTRY_POINT: src/Catalog.API/Program.cs

EXPOSES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: GET /items, /items/{id}, /items/by-ids, /items/by-tag
    DESCRIPTION: REST endpoints for browsing products and retrieving catalog with pagination and filtering

  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: POST /search/semantic
    DESCRIPTION: Embeddings-based product search using pgvector

CONSUMES:
  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.validation.confirmed
    FROM_SERVICE: Ordering.API
    DESCRIPTION: Validates stock availability and decrements inventory for confirmed orders

  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.paid
    FROM_SERVICE: Ordering.API
    DESCRIPTION: Updates inventory stock (decrements) upon payment confirmation

---

### SERVICE_NAME: Identity.API

TYPE: API

PURPOSE: OAuth2/OIDC identity provider, user authentication, and JWT token issuance for all APIs.

OVERVIEW_REF: NONE

ENTRY_POINT: src/Identity.API/Program.cs

EXPOSES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: GET /.well-known/openid-configuration, /connect/authorize, /connect/token
    DESCRIPTION: OAuth2/OpenID Connect discovery and token endpoints

  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: GET /identity
    DESCRIPTION: User profile endpoint (returns current user claims)

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: Ordering.API

TYPE: API

PURPOSE: Order lifecycle management with CQRS and state-machine orchestration (6 states: awaiting validation, stock confirmed, paid, shipped, cancelled, rejected).

OVERVIEW_REF: NONE

ENTRY_POINT: src/Ordering.API/Program.cs

EXPOSES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: POST /orders
    DESCRIPTION: Create new order (routes to Ordering.Domain command handler)

  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: GET /orders, /orders/{id}, /orders/byuser
    DESCRIPTION: Query order history and order details (CQRS read model)

CONSUMES:
  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.grace-period.confirmed
    FROM_SERVICE: OrderProcessor
    DESCRIPTION: Advances order state from AwaitingValidation to StockConfirmed after grace period expiration

  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.payment.succeeded, order.payment.failed
    FROM_SERVICE: PaymentProcessor
    DESCRIPTION: Updates order status based on payment attempt outcome

  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.stock.confirmed, order.stock.rejected
    FROM_SERVICE: Catalog.API
    DESCRIPTION: Accepts or rejects order based on inventory availability

---

### SERVICE_NAME: Webhooks.API

TYPE: API

PURPOSE: Webhook subscription management and dispatch of outbound HTTP webhooks to external subscribers on order and pricing events.

OVERVIEW_REF: NONE

ENTRY_POINT: src/Webhooks.API/Program.cs

EXPOSES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: POST /subscriptions, GET /subscriptions/{id}, DELETE /subscriptions/{id}
    DESCRIPTION: CRUD operations for webhook subscriptions (webhook URL, event type, optional token)

  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: GET /subscriptions/{id}/grant-url
    DESCRIPTION: OAuth2-like grant URL for third-party authorization with CORS origin validation

CONSUMES:
  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.shipped, order.paid, product.price.changed
    FROM_SERVICE: Ordering.API, Catalog.API
    DESCRIPTION: Triggers HTTP POST delivery to subscriber endpoints (with optional bearer token)

---

### SERVICE_NAME: OrderProcessor

TYPE: Worker

PURPOSE: Detects order grace period expiration via polling and publishes grace-period-confirmed event to advance order state.

OVERVIEW_REF: NONE

ENTRY_POINT: src/OrderProcessor/Program.cs

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: MessageQueue (polling)
    ENDPOINT_OR_TOPIC: order.awaiting.validation
    FROM_SERVICE: Ordering.API (persisted in database)
    DESCRIPTION: Queries for orders exceeding grace period, publishes confirmation event

---

### SERVICE_NAME: PaymentProcessor

TYPE: Worker

PURPOSE: Simulates payment authorization for orders; listens for stock-confirmed events and publishes success/failure outcomes.

OVERVIEW_REF: NONE

ENTRY_POINT: src/PaymentProcessor/Program.cs

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: MessageQueue
    ENDPOINT_OR_TOPIC: order.stock.confirmed
    FROM_SERVICE: Catalog.API
    DESCRIPTION: Receives notification that order stock is reserved; simulates payment and publishes result

---

### SERVICE_NAME: WebApp

TYPE: Frontend

PURPOSE: Customer-facing Blazor Server shopping application; product browsing, basket management, order placement, AI chatbot.

OVERVIEW_REF: NONE

ENTRY_POINT: src/WebApp/Program.cs

EXPOSES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: / (web UI)
    DESCRIPTION: Blazor Server interactive UI for product browsing, basket, checkout, order tracking

CONSUMES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: GET /items, POST /search/semantic
    FROM_SERVICE: Catalog.API
    DESCRIPTION: Fetch product catalog and perform semantic search

  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: POST /orders, GET /orders
    FROM_SERVICE: Ordering.API
    DESCRIPTION: Place orders and retrieve order history

  - PROTOCOL: gRPC
    ENDPOINT_OR_TOPIC: /basket.Basket/GetBasket, UpdateBasket
    FROM_SERVICE: Basket.API
    DESCRIPTION: Manage shopping cart

  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: /connect/authorize, /connect/token
    FROM_SERVICE: Identity.API
    DESCRIPTION: OAuth2 authentication and authorization

---

### SERVICE_NAME: WebhookClient

TYPE: Frontend

PURPOSE: Webhook subscriber management UI; allows users to register subscriptions, monitor delivery logs, and verify webhook authenticity.

OVERVIEW_REF: NONE

ENTRY_POINT: src/WebhookClient/Program.cs

EXPOSES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: / (web UI)
    DESCRIPTION: Blazor Server UI for webhook subscription and delivery monitoring

CONSUMES:
  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: GET /subscriptions, POST /subscriptions
    FROM_SERVICE: Webhooks.API
    DESCRIPTION: Retrieve and manage webhook subscriptions

  - PROTOCOL: HTTP
    ENDPOINT_OR_TOPIC: /connect/authorize, /connect/token
    FROM_SERVICE: Identity.API
    DESCRIPTION: OAuth2 authentication

---

### SERVICE_NAME: EventBus

TYPE: Library

PURPOSE: Abstract event bus contracts and interfaces (IEventBus, IIntegrationEventHandler) independent of transport or broker.

OVERVIEW_REF: NONE

ENTRY_POINT: src/EventBus/EventBus.csproj

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: EventBusRabbitMQ

TYPE: Library

PURPOSE: RabbitMQ concrete implementation of EventBus with OpenTelemetry tracing, Polly resilience, and AMQP 0.9.1 topology.

OVERVIEW_REF: NONE

ENTRY_POINT: src/EventBusRabbitMQ/EventBusRabbitMQ.csproj

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: IntegrationEventLogEF

TYPE: Library

PURPOSE: Outbox pattern implementation for reliable distributed transaction support; persists integration events to database before publishing.

OVERVIEW_REF: NONE

ENTRY_POINT: src/IntegrationEventLogEF/IntegrationEventLogEF.csproj

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: Ordering.Domain

TYPE: Library

PURPOSE: Domain-driven design aggregates (Order, Buyer, PaymentMethod) and domain events (7 types: OrderPlaced, OrderValidated, etc.).

OVERVIEW_REF: NONE

ENTRY_POINT: src/Ordering.Domain/Ordering.Domain.csproj

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: Ordering.Infrastructure

TYPE: Library

PURPOSE: Data access layer using Entity Framework Core; includes repositories, idempotency handlers, and database migrations for orderingdb.

OVERVIEW_REF: NONE

ENTRY_POINT: src/Ordering.Infrastructure/Ordering.Infrastructure.csproj

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: WebAppComponents

TYPE: Library

PURPOSE: Shared Razor components and utilities for WebApp (CatalogListItem, CatalogSearch, UI helpers).

OVERVIEW_REF: NONE

ENTRY_POINT: src/WebAppComponents/WebAppComponents.csproj

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: eShop.ServiceDefaults

TYPE: Library

PURPOSE: Cross-cutting concerns and configuration defaults: OpenTelemetry observability, health checks, resilience policies, JWT validation.

OVERVIEW_REF: NONE

ENTRY_POINT: src/eShop.ServiceDefaults/eShop.ServiceDefaults.csproj

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

### SERVICE_NAME: eShop.AppHost

TYPE: Orchestrator

PURPOSE: .NET Aspire application host; defines and orchestrates all services, infrastructure (Redis, RabbitMQ, PostgreSQL), service discovery, and environment variable injection.

OVERVIEW_REF: NONE

ENTRY_POINT: src/eShop.AppHost/Program.cs

EXPOSES:
  - PROTOCOL: NONE

CONSUMES:
  - PROTOCOL: NONE

---

## Service Communication Map

INTERACTION_ID: 1
FROM_SERVICE: WebApp
TO_SERVICE: Catalog.API
PROTOCOL: HTTP
CHANNEL: GET /items, GET /items/{id}, POST /search/semantic
DIRECTION: Request-Response
PURPOSE: Customer browses product catalog with optional semantic search
CONTRACT_REF: src/Catalog.API/Catalog.API.json

---

INTERACTION_ID: 2
FROM_SERVICE: WebApp
TO_SERVICE: Basket.API
PROTOCOL: gRPC
CHANNEL: /basket.Basket/GetBasket, /basket.Basket/UpdateBasket, /basket.Basket/DeleteBasket
DIRECTION: Request-Response
PURPOSE: Manage shopping cart state (add item, remove item, view cart)
CONTRACT_REF: src/Basket.API/Proto/basket.proto

---

INTERACTION_ID: 3
FROM_SERVICE: WebApp
TO_SERVICE: Ordering.API
PROTOCOL: HTTP
CHANNEL: POST /orders (CreateOrderCommand), GET /orders (OrderQuery)
DIRECTION: Request-Response
PURPOSE: Customer places order and retrieves order history
CONTRACT_REF: src/Ordering.API (MediatR commands/queries)

---

INTERACTION_ID: 4
FROM_SERVICE: WebApp
TO_SERVICE: Identity.API
PROTOCOL: HTTP
CHANNEL: GET /.well-known/openid-configuration, POST /connect/token, GET /connect/authorize
DIRECTION: Request-Response
PURPOSE: OpenID Connect authentication and JWT token acquisition
CONTRACT_REF: Duende IdentityServer 7 standard endpoints

---

INTERACTION_ID: 5
FROM_SERVICE: Ordering.API
TO_SERVICE: Basket.API
PROTOCOL: MessageQueue
CHANNEL: order.started (topic/queue)
DIRECTION: Event
PURPOSE: Notify Basket.API to clear customer basket after successful order confirmation
CONTRACT_REF: src/Ordering.Domain/Events/OrderStartedIntegrationEvent.cs

---

INTERACTION_ID: 6
FROM_SERVICE: Ordering.API
TO_SERVICE: Catalog.API
PROTOCOL: MessageQueue
CHANNEL: order.validation.requested (implicit via state transition)
DIRECTION: Event
PURPOSE: Request catalog to validate stock availability for order items
CONTRACT_REF: src/Ordering.Domain/Events/OrderStatusChangedToAwaitingValidationIntegrationEvent.cs

---

INTERACTION_ID: 7
FROM_SERVICE: Catalog.API
TO_SERVICE: Ordering.API
PROTOCOL: MessageQueue
CHANNEL: order.stock.confirmed, order.stock.rejected
DIRECTION: Event
PURPOSE: Catalog publishes stock validation result (reserved or rejected)
CONTRACT_REF: src/Catalog.API/IntegrationEvents/ (OrderStockConfirmedIntegrationEvent, OrderStockRejectedIntegrationEvent)

---

INTERACTION_ID: 8
FROM_SERVICE: OrderProcessor
TO_SERVICE: Ordering.API
PROTOCOL: MessageQueue
CHANNEL: order.grace-period.confirmed
DIRECTION: Event
PURPOSE: Notify Ordering.API that grace period has expired; advances order state from AwaitingValidation to StockConfirmed
CONTRACT_REF: src/OrderProcessor (GracePeriodConfirmedIntegrationEvent)

---

INTERACTION_ID: 9
FROM_SERVICE: PaymentProcessor
TO_SERVICE: Ordering.API
PROTOCOL: MessageQueue
CHANNEL: order.payment.succeeded, order.payment.failed
DIRECTION: Event
PURPOSE: Publish payment attempt outcome (success or failure) to advance order state
CONTRACT_REF: src/PaymentProcessor (OrderPaymentSucceededIntegrationEvent, OrderPaymentFailedIntegrationEvent)

---

INTERACTION_ID: 10
FROM_SERVICE: Catalog.API
TO_SERVICE: Ordering.API
PROTOCOL: MessageQueue
CHANNEL: order.paid (implicit subscription)
DIRECTION: Event
PURPOSE: Catalog subscribes to PaidIntegrationEvent and decrements inventory stock
CONTRACT_REF: src/Catalog.API/IntegrationEvents/

---

INTERACTION_ID: 11
FROM_SERVICE: Ordering.API
TO_SERVICE: Webhooks.API
PROTOCOL: MessageQueue
CHANNEL: order.shipped, order.paid
DIRECTION: Event
PURPOSE: Notify Webhooks.API of order state changes for external subscriber dispatch
CONTRACT_REF: src/Ordering.Domain/Events/ (OrderStatusChangedToShippedIntegrationEvent, OrderStatusChangedToPaidIntegrationEvent)

---

INTERACTION_ID: 12
FROM_SERVICE: Catalog.API
TO_SERVICE: Webhooks.API
PROTOCOL: MessageQueue
CHANNEL: product.price.changed
DIRECTION: Event
PURPOSE: Notify Webhooks.API of product price changes for subscriber webhook dispatch
CONTRACT_REF: src/Catalog.API/IntegrationEvents/ (ProductPriceChangedIntegrationEvent)

---

INTERACTION_ID: 13
FROM_SERVICE: Webhooks.API
TO_SERVICE: External Subscribers
PROTOCOL: HTTP
CHANNEL: POST (subscriber webhook URL)
DIRECTION: Fire-and-Forget
PURPOSE: Dispatch outbound webhooks with order/product events to third-party subscribers
CONTRACT_REF: src/Webhooks.API/Services/WebhooksSender.cs

---

INTERACTION_ID: 14
FROM_SERVICE: WebhookClient
TO_SERVICE: Webhooks.API
PROTOCOL: HTTP
CHANNEL: GET /subscriptions, POST /subscriptions, DELETE /subscriptions/{id}
DIRECTION: Request-Response
PURPOSE: Manage webhook subscriptions and retrieve subscription details
CONTRACT_REF: src/Webhooks.API/Endpoints/

---

INTERACTION_ID: 15
FROM_SERVICE: WebhookClient
TO_SERVICE: Identity.API
PROTOCOL: HTTP
CHANNEL: POST /connect/token, GET /connect/authorize
DIRECTION: Request-Response
PURPOSE: OAuth2/OpenID Connect authentication for WebhookClient users
CONTRACT_REF: Duende IdentityServer 7 standard endpoints

---

INTERACTION_ID: 16
FROM_SERVICE: PaymentProcessor
TO_SERVICE: Catalog.API
PROTOCOL: MessageQueue
CHANNEL: order.stock.confirmed
DIRECTION: Event
PURPOSE: PaymentProcessor subscribes to stock confirmation before simulating payment
CONTRACT_REF: src/PaymentProcessor (consumes OrderStockConfirmedIntegrationEvent)

---

INTERACTION_ID: 17
FROM_SERVICE: All Services
TO_SERVICE: Identity.API
PROTOCOL: HTTP
CHANNEL: Introspection endpoint (token validation)
DIRECTION: Request-Response
PURPOSE: Validate JWT Bearer tokens from incoming HTTP requests
CONTRACT_REF: eShop.ServiceDefaults (AddServiceAuthentication)

---

## Shared Infrastructure

INFRA_NAME: RabbitMQ

TYPE: MessageBroker

USED_BY_SERVICES: Ordering.API, Catalog.API, Basket.API, Webhooks.API, OrderProcessor, PaymentProcessor

PURPOSE: Distributed event bus for asynchronous cross-service communication; supports topic-based pub/sub with message persistence and replay.

CONFIG_REF: src/eShop.AppHost/Program.cs (RabbitMQ container definition), src/EventBusRabbitMQ/RabbitMQEventBus.cs (AMQP topology)

---

INFRA_NAME: PostgreSQL (pgvector)

TYPE: Database

USED_BY_SERVICES: Catalog.API, Identity.API, Ordering.API, Webhooks.API, IntegrationEventLogEF

PURPOSE: Primary data store with schema isolation (database-per-service pattern); pgvector extension enables semantic search embeddings in Catalog.API.

CONFIG_REF: src/eShop.AppHost/Program.cs (postgres container and database provisioning), Directory.Build.props

---

INFRA_NAME: Redis

TYPE: Cache

USED_BY_SERVICES: Basket.API

PURPOSE: In-memory cache for shopping basket state with TTL expiration; provides fast cart read/write performance.

CONFIG_REF: src/eShop.AppHost/Program.cs (redis container definition), src/Basket.API/Repositories/BasketRepository.cs

---

INFRA_NAME: Identity.API (Duende IdentityServer 7)

TYPE: IdentityProvider

USED_BY_SERVICES: WebApp, WebhookClient, Catalog.API, Ordering.API, Basket.API, Webhooks.API

PURPOSE: Centralized OAuth2/OIDC provider; issues JWT Bearer tokens and validates client credentials; manages user profiles and registered OAuth clients.

CONFIG_REF: src/Identity.API/Program.cs, src/eShop.ServiceDefaults

---

## Folder Structure

PATH: /

ROLE: Config

PURPOSE: Repository root with solution files, package manifests, CI/CD pipeline definitions.

---

PATH: src/

ROLE: Service

PURPOSE: Top-level services container; each subdirectory represents a deployable unit or shared library.

---

PATH: src/Basket.API/

ROLE: Service

PURPOSE: Shopping cart microservice with gRPC interface.

---

PATH: src/Catalog.API/

ROLE: Service

PURPOSE: Product catalog microservice with REST API and semantic search.

---

PATH: src/Identity.API/

ROLE: Service

PURPOSE: OAuth2/OIDC identity provider microservice.

---

PATH: src/Ordering.API/

ROLE: Service

PURPOSE: Order management microservice with CQRS and domain-driven design.

---

PATH: src/Webhooks.API/

ROLE: Service

PURPOSE: Webhook subscription and dispatch microservice.

---

PATH: src/OrderProcessor/

ROLE: Service

PURPOSE: Background worker for grace period detection and event publishing.

---

PATH: src/PaymentProcessor/

ROLE: Service

PURPOSE: Background worker for payment simulation and event publishing.

---

PATH: src/WebApp/

ROLE: Service

PURPOSE: Customer-facing Blazor Server frontend for shopping and order tracking.

---

PATH: src/WebhookClient/

ROLE: Service

PURPOSE: Webhook management interface (Blazor Server frontend).

---

PATH: src/EventBus/

ROLE: Library

PURPOSE: Abstract event bus interfaces and contracts.

---

PATH: src/EventBusRabbitMQ/

ROLE: Library

PURPOSE: RabbitMQ concrete implementation of event bus.

---

PATH: src/IntegrationEventLogEF/

ROLE: Library

PURPOSE: Outbox pattern implementation for reliable event publishing.

---

PATH: src/Ordering.Domain/

ROLE: Library

PURPOSE: DDD aggregates and domain events for ordering bounded context.

---

PATH: src/Ordering.Infrastructure/

ROLE: Library

PURPOSE: Data access layer and infrastructure for ordering service.

---

PATH: src/WebAppComponents/

ROLE: Library

PURPOSE: Reusable Razor components shared by frontends.

---

PATH: src/eShop.ServiceDefaults/

ROLE: Library

PURPOSE: Cross-cutting concerns and configuration extensions for all services.

---

PATH: src/Shared/

ROLE: Library

PURPOSE: Utility functions, OpenTelemetry helpers, and database migration helpers.

---

PATH: src/eShop.AppHost/

ROLE: Infrastructure

PURPOSE: .NET Aspire orchestration and service composition configuration.

---

PATH: tests/

ROLE: Test

PURPOSE: End-to-end and unit test projects.

---

PATH: tests/Basket.UnitTests/

ROLE: Test

PURPOSE: Unit tests for Basket.API business logic.

---

PATH: tests/Catalog.FunctionalTests/

ROLE: Test

PURPOSE: Functional integration tests for Catalog.API.

---

PATH: tests/Ordering.UnitTests/

ROLE: Test

PURPOSE: Unit tests for Ordering domain and handlers.

---

PATH: tests/Ordering.FunctionalTests/

ROLE: Test

PURPOSE: Functional end-to-end tests for Ordering workflows.

---

PATH: tests/ClientApp.UnitTests/

ROLE: Test

PURPOSE: Unit tests for WebApp Blazor components.

---

PATH: e2e/

ROLE: Test

PURPOSE: Playwright end-to-end test suite (browser automation).

---

PATH: build/

ROLE: Tool

PURPOSE: Build scripts and multi-architecture manifest generation.

---

PATH: artifacts/

ROLE: Config

PURPOSE: Build output directory for compiled binaries and intermediates.

---

## Architectural Patterns

PATTERN: Microservices

SCOPE: Entire system (5 independent API services + 2 worker services + 2 frontends)

EVIDENCE: src/Basket.API, src/Catalog.API, src/Identity.API, src/Ordering.API, src/Webhooks.API each with independent deployability

---

PATTERN: Event-Driven Architecture

SCOPE: Cross-service communication via RabbitMQ

EVIDENCE: src/EventBus, src/EventBusRabbitMQ, IntegrationEvent classes throughout src/**/IntegrationEvents/

---

PATTERN: Domain-Driven Design (DDD)

SCOPE: Ordering bounded context

EVIDENCE: src/Ordering.Domain (Order, Buyer aggregates), src/Ordering.Domain/Events (7 domain event types), src/Ordering.Infrastructure (repository pattern)

---

PATTERN: CQRS (Command Query Responsibility Segregation)

SCOPE: Ordering.API

EVIDENCE: src/Ordering.API using MediatR for command/query dispatch; separate write and read models

---

PATTERN: Outbox Pattern

SCOPE: Reliable event publishing across all services

EVIDENCE: src/IntegrationEventLogEF, used by Ordering.API to persist events before publishing to RabbitMQ

---

PATTERN: Saga Pattern (Implicitly Long-Running Transaction)

SCOPE: Order lifecycle (multi-step state machine across Ordering/Catalog/Payment services)

EVIDENCE: OrderProcessor + PaymentProcessor chain events through grace-period and payment stages; Ordering.API tracks 6 order states

---

PATTERN: API Gateway / BFF (Backend-for-Frontend)

SCOPE: mobile-bff (YARP reverse proxy)

EVIDENCE: src/eShop.AppHost (mobile-bff service definition); routes external traffic to Catalog/Ordering/Identity services

---

PATTERN: Per-Service Database

SCOPE: All microservices (database isolation)

EVIDENCE: src/eShop.AppHost defines catalogdb, identitydb, orderingdb, webhooksdb as separate PostgreSQL databases

---

PATTERN: Health Checks

SCOPE: All services

EVIDENCE: eShop.ServiceDefaults (AddServiceHealthChecks extension method)

---

PATTERN: OpenTelemetry Observability

SCOPE: Entire system (traces, metrics, logs)

EVIDENCE: eShop.ServiceDefaults (AddOpenTelemetry extension), EventBusRabbitMQ (W3C Trace Context propagation)

---

## Security Topology

AUTHN_AUTHZ: OAuth2 Authorization Code Flow + JWT Bearer tokens

- **Entry point**: WebApp, WebhookClient redirect users to Identity.API for login
- **Tokens**: JWT Bearer issued by Identity.API (Duende IdentityServer 7)
- **Enforcement**: eShop.ServiceDefaults (JWT validation middleware on API services)
- **Scopes**: WebApp and WebhookClient use OpenID Connect; APIs validate bearer tokens
- **Identity.API**: No authentication required to reach login endpoint (public OIDC discovery)

---

TRUST_BOUNDARIES: Two boundaries identified

1. **External/Public Boundary** (between internet and system)
   - Services: WebApp, WebhookClient, Identity.API, Webhooks.API (grant URL endpoint)
   - Enforcement: OAuth2/OIDC login + JWT bearer token validation
   - Entry vectors: HTTPS only (assumed in production)

2. **Internal/Private Boundary** (between frontend/public APIs and internal services)
   - Services: Ordering.API, Catalog.API, Basket.API (internal, no direct external access)
   - Enforcement: Token propagation via HttpClientAuthorizationDelegatingHandler in eShop.ServiceDefaults
   - Broker: RabbitMQ (trusted internal network, no authentication required)
   - Databases: PostgreSQL (access restricted to respective services)

---

KNOWN_RISKS:

- **External Webhook Delivery**: Webhooks.API forwards credentials (bearer token) to external subscribers; token compromise exposes external systems. Mitigation: rotate tokens, validate certificate, enforce HTTPS.

- **Database Access**: No explicit row-level security; database isolation relies on application-layer authorization middleware.

- **RabbitMQ**: No authentication on message broker; assumes private network (Aspire containerization). Risk in multi-tenant deployments.

- **JWT Secret Management**: Secret storage for JWT signing keys should use key management service (Azure Key Vault, etc.) in production; not evident in code.

- **CORS Configuration**: Not explicitly documented; verify CORS headers on Identity.API and public endpoints.

---

## Deployment Topology

DEPLOYMENT_MODEL: Microservices (cloud-native, multi-container)

---

CONTAINER_RUNTIME: Docker (Aspire abstraction)

---

ORCHESTRATION: .NET Aspire (local) | Kubernetes (production-assumed)

---

SERVICES_AND_PORTS:

(Derived from src/eShop.AppHost/Program.cs and eShop.ServiceDefaults configuration)

- **Identity.API**: Exposed port 5500 (HTTP) + HTTPS
- **Catalog.API**: Internal port (service discovery via Aspire)
- **Basket.API**: Internal port; gRPC endpoint
- **Ordering.API**: Internal port
- **Webhooks.API**: Internal port
- **WebApp**: Exposed port 5173 (HTTP) + HTTPS
- **WebhookClient**: Exposed port (HTTP) + HTTPS
- **OrderProcessor**: Background worker (no exposed port)
- **PaymentProcessor**: Background worker (no exposed port)
- **PostgreSQL**: Internal port 5432 (via Aspire)
- **RabbitMQ**: Internal port 5672 (AMQP), 15672 (management UI — development only)
- **Redis**: Internal port 6379

---

CONFIG_REFS:

- **Aspire Orchestration**: src/eShop.AppHost/Program.cs
- **Service Defaults**: src/eShop.ServiceDefaults/Extensions/ServiceDefaults.cs
- **Docker Images**: DOCKERFILE in each service (if present), or base images specified in Aspire AppHost
- **Kubernetes Manifests**: Not included in repo; assumed generated via `dotnet publish --use-asp-net-core-runtime-container`
- **Database Migrations**: src/Ordering.Infrastructure/Data/Migrations/, src/Catalog.API/Migrations/
- **Environment Configuration**: appsettings.json and appsettings.Development.json in each service root

---

## Assumptions

ASSUMPTION: RabbitMQ requires no explicit authentication.

BASIS: src/eShop.AppHost and src/EventBusRabbitMQ do not configure credentials for RabbitMQ connection; assumed to be a private trusted network inside container orchestration. In production, AMQP authentication and encryption should be enforced.

---

ASSUMPTION: All databases are PostgreSQL with pgvector extension.

BASIS: src/eShop.AppHost defines single postgres container with ankane/pgvector image; each service connects to isolated database within same instance.

---

ASSUMPTION: .NET Aspire orchestration replaces traditional docker-compose for local development and generates Kubernetes manifests for production deployment.

BASIS: No docker-compose.yml or Kubernetes YAML files present in repository; Aspire is the single source of truth for infrastructure as code.

---

ASSUMPTION: Identity.API (Duende IdentityServer 7) is a single centralized OAuth2/OIDC provider for all services and frontends.

BASIS: src/Identity.API is the only OAuth2 provider; all other services delegate token validation to Identity.API.

---

ASSUMPTION: WebhookClient is an internal application (same domain ownership) and does not require additional authentication beyond OAuth2 for webhook management.

BASIS: Webhooks.API grant URL validation suggests intended use by same organization; external webhooks are NOT authenticated by Webhooks.API (only by presence in subscription list).

---

ASSUMPTION: Order processing is eventually consistent (not transactional) with implicit saga coordination via events.

BASIS: OrderProcessor polls for grace-period expiration; PaymentProcessor waits for stock confirmation; no distributed transaction coordinator (Temporal, Saga orchestrator) is evident.

---

ASSUMPTION: Semantic search (embeddings) in Catalog.API is optional and requires OpenAI or Ollama configuration; functional without it.

BASIS: src/Catalog.API includes pgvector imports and embedding models, but integration is conditional on appsettings; core product search via SQL full-text or LIKE queries remains available.

---

ASSUMPTION: All service-to-service HTTP communication is authenticated via JWT Bearer tokens propagated from incoming requests.

BASIS: eShop.ServiceDefaults includes HttpClientAuthorizationDelegatingHandler that automatically appends Bearer token to outgoing requests.

---

ASSUMPTION: End-to-end test coverage (e2e/) assumes running system is fully operational; tests do not mock infrastructure dependencies.

BASIS: e2e/ folder contains Playwright tests (browser automation); integration tests in tests/Ordering.FunctionalTests/ likely simulate entire order workflow.
