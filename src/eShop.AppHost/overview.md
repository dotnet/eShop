# eShop AppHost

Microsoft Aspire-based microservices orchestration and local development environment that configures and coordinates all eShop services, infrastructure dependencies, and API gateways.

## Summary

- Central orchestrator for the eShop microservices architecture using Microsoft Aspire v13.1.0
- Manages local Docker containers for infrastructure services: Redis (cache), RabbitMQ (message broker), PostgreSQL (database)
- Defines all service-to-service dependencies, health checks, and startup prerequisites
- Exposes a YARP-based reverse proxy (mobile-bff) for API aggregation and BFF routing
- Supports optional AI/ML integrations: OpenAI, Azure OpenAI, and local Ollama embeddings/chat models
- Configured for CI/CD testing with HTTP/HTTPS endpoint flexibility via environment variables

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `src/eShop.AppHost/` | Aspire distributed application project; orchestrates all microservices and infrastructure | `Program.cs`, `Extensions.cs` |

## Components

---

COMPONENT_NAME: eShop.AppHost
TYPE: Gateway
PURPOSE: Aspire orchestration hub that defines infrastructure, services, and routing for the entire eShop microservices platform.
RESPONSIBILITIES:
  - Instantiate and configure Docker containers for Redis, RabbitMQ, and PostgreSQL
  - Create and wire all microservice projects (Basket.API, Catalog.API, Ordering.API, etc.)
  - Define inter-service dependencies and startup ordering (WaitFor conditions)
  - Configure environment variables and connection strings for all services
  - Manage YARP reverse proxy routes for the mobile-bff gateway
  - Apply global middleware settings (forwarded headers) to all projects via Aspire eventing
SOURCE: `src/eShop.AppHost/Program.cs`, `src/eShop.AppHost/Extensions.cs`
CALLS:
  - Redis — to cache basket state for Basket.API
  - RabbitMQ — to enable asynchronous event-driven communication
  - PostgreSQL — to provide persistent storage for all services
  - Basket.API — to add reference and start the service
  - Catalog.API — to add reference and start the service
  - Identity.API — to add reference and start the service
  - Ordering.API — to add reference and start the service
  - OrderProcessor — to add reference and start the service
  - PaymentProcessor — to add reference and start the service
  - Webhooks.API — to add reference and start the service
  - WebApp — to add reference and start the service
  - WebhookClient — to add reference and start the service
  - YARP (mobile-bff) — to configure reverse proxy routes
  - OpenAI / Azure OpenAI / Ollama — optionally, to inject AI models into Catalog.API and WebApp
CALLED_BY:
  - NONE (entry point; launched by dotnet run or Aspire dashboard)

---

COMPONENT_NAME: Redis
TYPE: Cache
PURPOSE: In-memory distributed cache for session and basket state management.
RESPONSIBILITIES:
  - Store basket items and session data with TTL expiration
  - Provide high-speed read/write access for shopping cart operations
  - Serve as the backing store for the Basket.API repository layer
SOURCE: Instantiated in `src/eShop.AppHost/Program.cs` line 5; configured as persistent container via `ContainerLifetime.Persistent`
CALLS:
  - NONE
CALLED_BY:
  - Basket.API — to persist and retrieve basket entries

---

COMPONENT_NAME: RabbitMQ
TYPE: Producer
PURPOSE: Message broker for asynchronous event-driven communication across all microservices.
RESPONSIBILITIES:
  - Accept integration event publications from API services
  - Route events to subscribed consumer services
  - Guarantee message delivery and processing across service boundaries
  - Enable loose coupling between ordering, payment, inventory, and notification services
SOURCE: Instantiated in `src/eShop.AppHost/Program.cs` line 6; configured as persistent container with `ContainerLifetime.Persistent`
CALLS:
  - NONE
CALLS_INDIRECTLY:
  - OrderProcessor — receives order integration events
  - PaymentProcessor — receives payment events
  - All services with event subscribers
CALLED_BY:
  - Catalog.API — publishes product availability events
  - Ordering.API — publishes order placement, confirmation, and state change events
  - OrderProcessor — publishes order state events
  - PaymentProcessor — publishes payment processed/failed events
  - Webhooks.API — publishes webhook triggered events

---

COMPONENT_NAME: PostgreSQL
TYPE: Store
PURPOSE: Persistent relational database with pgvector extension for semantic search; provides isolated schemas for each service.
RESPONSIBILITIES:
  - Store catalog products, categories, and embeddings (pgvector)
  - Maintain user identity and authentication data
  - Persist customer orders and order line items
  - Store webhook registration and delivery logs
  - Support Entity Framework Core migrations and schema management
SOURCE: Instantiated in `src/eShop.AppHost/Program.cs` lines 7-8; image: `ankane/pgvector`, configured as persistent container
CALLS:
  - NONE
CALLED_BY:
  - Catalog.API — (via catalogdb database)
  - Identity.API — (via identitydb database)
  - Ordering.API — (via orderingdb database)
  - OrderProcessor — (via orderingdb database)
  - Webhooks.API — (via webhooksdb database)

---

COMPONENT_NAME: Basket.API
TYPE: API
PURPOSE: HTTP API for shopping cart management; exposes endpoints for adding, updating, and retrieving basket items.
RESPONSIBILITIES:
  - Accept POST/GET/DELETE basket operations from WebApp
  - Validate basket integrity and item stock availability
  - Persist basket state to Redis
  - Consume integration events for order confirmation (to clear basket)
  - Authenticate requests via Identity.API
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 23-26
CALLS:
  - Identity.API — to authenticate incoming requests
  - Redis — to persist and retrieve basket items
  - RabbitMQ — to subscribe to order confirmation events
CALLED_BY:
  - WebApp — to retrieve and modify basket state
  - eShop.AppHost — to add reference and manage service lifecycle

---

COMPONENT_NAME: Catalog.API
TYPE: API
PURPOSE: HTTP API for product catalog; exposes endpoints for browsing items, searching by category, and semantic search with embeddings.
RESPONSIBILITIES:
  - Serve GET endpoints for items, categories (types), and brands
  - Filter products by type and brand
  - Implement semantic search using pgvector embeddings and AI models (optional)
  - Return product images and metadata
  - Consume integration events for inventory updates (from OrderProcessor)
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 28-30
CALLS:
  - PostgreSQL (catalogdb) — to query and retrieve product catalog
  - RabbitMQ — to subscribe to inventory update events
  - OpenAI / Azure OpenAI / Ollama (conditional) — to compute and query embeddings for semantic search
CALLED_BY:
  - WebApp — to fetch products and categories
  - mobile-bff (YARP) — to route catalog requests
  - eShop.AppHost — to add reference and manage service lifecycle

---

COMPONENT_NAME: Identity.API
TYPE: API
PURPOSE: OAuth2/OpenID Connect identity provider for user authentication and authorization across all eShop applications.
RESPONSIBILITIES:
  - Authenticate users via login endpoint
  - Issue JWT bearer tokens for API calls
  - Validate and revoke tokens
  - Manage user registration and profile data
  - Support callback URL registration for OAuth flows
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 20-22
CALLS:
  - PostgreSQL (identitydb) — to store and query user credentials and OAuth state
CALLED_BY:
  - Basket.API — to validate request authentication
  - Ordering.API — to validate request authentication
  - Webhooks.API — to validate request authentication
  - WebApp — to authenticate users and obtain tokens
  - WebhookClient — to authenticate with Webhooks.API
  - eShop.AppHost — to add reference, manage service lifecycle, and retrieve endpoint for downstream services

---

COMPONENT_NAME: Ordering.API
TYPE: API
PURPOSE: HTTP API for order creation and querying; handles order placement, payment processing coordination, and order status retrieval.
RESPONSIBILITIES:
  - Accept POST requests to create new orders with line items
  - Validate order integrity and payment method
  - Orchestrate payment processing via PaymentProcessor
  - Return GET endpoints for order status and history by customer
  - Publish OrderPlacedEvent and OrderStatusChangedEvent for downstream processing
  - Implement health check endpoint for startup coordination
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 32-37
CALLS:
  - PostgreSQL (orderingdb) — to persist and retrieve orders
  - RabbitMQ — to publish order events and subscribe to payment completion events
  - Identity.API — to authenticate incoming requests
CALLED_BY:
  - WebApp — to place and query orders
  - mobile-bff (YARP) — to route order requests
  - OrderProcessor — consumes OrderPlacedEvent
  - eShop.AppHost — to add reference, manage service lifecycle, and set WaitFor(orderingDb) dependency

---

COMPONENT_NAME: OrderProcessor
TYPE: Consumer
PURPOSE: Background service that processes order state transitions and triggers fulfillment workflows.
RESPONSIBILITIES:
  - Consume OrderPlacedEvent from RabbitMQ
  - Transition order state from Pending to Processing/Shipped
  - Trigger inventory decrement and reservation actions
  - Publish OrderShipped and OrderCanceled events for notifications and webhooks
  - Access Ordering.API health check to ensure readiness before consuming
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 38-41
CALLS:
  - PostgreSQL (orderingdb) — to update order state
  - RabbitMQ — to consume order events and publish shipping events
  - Ordering.API — health check via WaitFor(Ordering.API)
CALLED_BY:
  - RabbitMQ — receives OrderPlacedEvent from Ordering.API
  - eShop.AppHost — to add reference and manage service lifecycle

---

COMPONENT_NAME: PaymentProcessor
TYPE: Consumer
PURPOSE: Background service that processes payment transactions; simulates or integrates with payment gateway.
RESPONSIBILITIES:
  - Consume PaymentRequestedEvent from RabbitMQ
  - Execute payment processing logic (mock or external gateway)
  - Publish PaymentSuccessfulEvent or PaymentFailedEvent
  - Enable Ordering.API to respond to payment results
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 43-44
CALLS:
  - RabbitMQ — to consume payment events and publish payment result events
CALLED_BY:
  - RabbitMQ — receives PaymentRequestedEvent from Ordering.API
  - eShop.AppHost — to add reference and manage service lifecycle

---

COMPONENT_NAME: Webhooks.API
TYPE: API
PURPOSE: HTTP API for registering and delivering webhook notifications to external subscribers on order and domain events.
RESPONSIBILITIES:
  - Accept POST requests to register webhook subscriptions (URL, event types)
  - Store registered webhooks in persistent storage
  - Consume order and domain events from RabbitMQ
  - Deliver events to subscriber URLs via HTTP callbacks
  - Track webhook delivery success/failure and retry logic
  - Authenticate webhook subscribers via Identity.API
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 48-52
CALLS:
  - PostgreSQL (webhooksdb) — to store and retrieve webhook registrations
  - RabbitMQ — to consume order and event notifications
  - Identity.API — to validate requests from subscribers
CALLED_BY:
  - WebhookClient — to register and query webhooks
  - RabbitMQ — receives order events for webhook delivery
  - eShop.AppHost — to add reference and manage service lifecycle

---

COMPONENT_NAME: WebApp
TYPE: API
PURPOSE: Blazor/ASP.NET Core web application that serves the online store user interface and orchestrates customer shopping interactions.
RESPONSIBILITIES:
  - Serve HTML/CSS/JavaScript UI for browsing products
  - Accept user interactions for browse, search, add-to-basket, and checkout
  - Delegate API calls to downstream services (Basket.API, Catalog.API, Ordering.API)
  - Authenticate users via Identity.API
  - Display order history and current basket state
  - Optionally display AI-powered chat and semantic search results (when OpenAI/Ollama enabled)
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 66-73
CALLS:
  - Basket.API — to retrieve and modify shopping cart
  - Catalog.API — to fetch product list and details
  - Ordering.API — to create and query orders
  - Identity.API — to authenticate users
  - RabbitMQ — to subscribe to order status updates and notifications
  - OpenAI / Azure OpenAI / Ollama (conditional) — to generate chat responses and semantic search
CALLED_BY:
  - Browser / HTTP clients — external end users
  - eShop.AppHost — to add reference, manage service lifecycle, and set external endpoint

---

COMPONENT_NAME: WebhookClient
TYPE: Consumer
PURPOSE: Integration test application that simulates external webhook subscribers and validates webhook delivery.
RESPONSIBILITIES:
  - Register test webhook subscriptions with Webhooks.API
  - Expose HTTP endpoints to receive webhook callbacks
  - Verify that order events are delivered correctly
  - Validate webhook payload structure and content
SOURCE: Project reference in `src/eShop.AppHost/eShop.AppHost.csproj`; orchestrated in `src/eShop.AppHost/Program.cs` lines 55-59
CALLS:
  - Webhooks.API — to register and query webhook subscriptions
  - Identity.API — to authenticate with Webhooks.API
CALLED_BY:
  - Webhooks.API — via HTTP callback delivery
  - E2E test suite (e.g., Playwright tests)
  - eShop.AppHost — to add reference, manage service lifecycle

---

COMPONENT_NAME: mobile-bff (YARP)
TYPE: Gateway
PURPOSE: YARP-based reverse proxy that provides a unified API endpoint for mobile and external clients; routes and aggregates requests to backend services.
RESPONSIBILITIES:
  - Accept incoming requests on unified `/catalog-api/`, `/api/orders/`, `/identity/` paths
  - Route catalog requests to Catalog.API with version filtering (api-version query param)
  - Route order requests to Ordering.API
  - Route identity requests to Identity.API
  - Execute path prefix transformation (e.g., strip `/catalog-api` before forwarding)
  - Support semantic search routes: `/catalog-api/api/catalog/items/withsemanticrelevance/{text}`
SOURCE: Configured in `src/eShop.AppHost/Program.cs` line 45; routes defined in `src/eShop.AppHost/Extensions.cs` method `ConfigureMobileBffRoutes()` lines 183-248
CALLS:
  - Catalog.API — forwards catalog queries and semantic search requests
  - Ordering.API — forwards order creation and status queries
  - Identity.API — forwards authentication and token requests
CALLED_BY:
  - Mobile clients / external HTTP clients
  - eShop.AppHost — to add reference and manage service lifecycle

---

COMPONENT_NAME: ForwardedHeadersMiddleware
TYPE: Utility
PURPOSE: Aspire eventing subscriber that injects ASPNETCORE_FORWARDEDHEADERS_ENABLED environment variable into all project services at launch.
RESPONSIBILITIES:
  - Subscribe to Aspire BeforeStartEvent for all project resources
  - Set ASPNETCORE_FORWARDEDHEADERS_ENABLED=true via EnvironmentCallbackAnnotation
  - Enable each service to trust X-Forwarded-* headers from reverse proxies and gateways
SOURCE: Defined as `AddForwardHeadersSubscriber` class in `src/eShop.AppHost/Extensions.cs` lines 21-38; attached via `AddForwardedHeaders()` extension method in `Program.cs` line 3
CALLS:
  - BeforeStartEvent subscriber (Aspire eventing)
CALLED_BY:
  - eShop.AppHost — via builder.AddForwardedHeaders() at startup

## Component Call Sequences

### Use-Case: Browse Products and Add to Cart

STEP 1: WebApp → Catalog.API
  OPERATION: GET /api/v1/catalog/items
  PURPOSE: Retrieve list of available products with descriptions, prices, and image metadata for homepage display
  SOURCE: `src/eShop.AppHost/Program.cs` line 70 (WebApp reference to catalogApi)

STEP 2: WebApp → Identity.API
  OPERATION: POST /connect/token (OAuth2 token endpoint)
  PURPOSE: Authenticate user and obtain JWT bearer token for subsequent API calls
  SOURCE: `src/eShop.AppHost/Program.cs` line 72 (WebApp reference to identityEndpoint)

STEP 3: Catalog.API → PostgreSQL (catalogdb)
  OPERATION: SELECT * FROM Products WHERE ...
  PURPOSE: Query product catalog with filtering by type, brand, or semantic relevance (if embeddings enabled)
  SOURCE: `src/eShop.AppHost/Program.cs` line 29 (catalogApi reference to catalogDb)

STEP 4: WebApp → Basket.API
  OPERATION: POST /api/v1/basket/items
  PURPOSE: Add selected item to shopping cart with quantity
  SOURCE: `src/eShop.AppHost/Program.cs` line 69 (WebApp reference to basketApi); Basket.API implementation in `src/Basket.API/`

STEP 5: Basket.API → Redis
  OPERATION: SET basket:{userId} {items_json}
  PURPOSE: Persist user's basket state with item list and quantities
  SOURCE: `src/eShop.AppHost/Program.cs` line 24 (basketApi reference to redis)

---

### Use-Case: Place Order and Process Payment

STEP 1: WebApp → Ordering.API
  OPERATION: POST /api/v1/orders
  PURPOSE: Submit order creation request with basket items, shipping address, and payment method
  SOURCE: `src/eShop.AppHost/Program.cs` line 71 (WebApp reference to orderingApi)

STEP 2: Ordering.API → PostgreSQL (orderingdb)
  OPERATION: INSERT INTO Orders, INSERT INTO OrderItems
  PURPOSE: Persist new order and associated line items to the database
  SOURCE: `src/eShop.AppHost/Program.cs` line 34 (orderingApi reference to orderDb)

STEP 3: Ordering.API → RabbitMQ
  OPERATION: Publish OrderPlacedEvent {OrderId, CustomerId, Items, Total}
  PURPOSE: Notify downstream services (payment, fulfillment, notifications) that an order has been created
  SOURCE: `src/eShop.AppHost/Program.cs` line 33 (orderingApi reference to rabbitMq)

STEP 4: PaymentProcessor (Consumer) ← RabbitMQ
  OPERATION: Consume OrderPlacedEvent / PaymentRequestedEvent
  PURPOSE: Receive payment request and execute payment processing (mock or external gateway)
  SOURCE: `src/eShop.AppHost/Program.cs` line 44 (paymentProcessor reference to rabbitMq)

STEP 5: PaymentProcessor → RabbitMQ
  OPERATION: Publish PaymentSuccessfulEvent or PaymentFailedEvent {OrderId, Status}
  PURPOSE: Notify Ordering.API and OrderProcessor of payment outcome
  SOURCE: `src/eShop.AppHost/Program.cs` line 44 (paymentProcessor reference to rabbitMq)

STEP 6: OrderProcessor (Consumer) ← RabbitMQ
  OPERATION: Consume OrderPlacedEvent / OrderConfirmedEvent
  PURPOSE: Receive order confirmation and trigger fulfillment workflow
  SOURCE: `src/eShop.AppHost/Program.cs` line 40 (orderProcessor reference to rabbitMq)

STEP 7: OrderProcessor → PostgreSQL (orderingdb)
  OPERATION: UPDATE Orders SET Status='Shipped' WHERE OrderId=...
  PURPOSE: Update order state to Shipped in response to payment success
  SOURCE: `src/eShop.AppHost/Program.cs` line 39 (orderProcessor reference to orderDb)

STEP 8: OrderProcessor → RabbitMQ
  OPERATION: Publish OrderShippedEvent {OrderId, TrackingNumber}
  PURPOSE: Notify Webhooks.API and downstream subscribers of order shipment
  SOURCE: `src/eShop.AppHost/Program.cs` line 40 (orderProcessor reference to rabbitMq)

STEP 9: Webhooks.API (Consumer) ← RabbitMQ
  OPERATION: Consume OrderShippedEvent
  PURPOSE: Receive shipment notification and forward to registered webhook subscribers
  SOURCE: `src/eShop.AppHost/Program.cs` line 50 (webHooksApi reference to rabbitMq)

STEP 10: Webhooks.API → WebhookClient / External Subscriber
  OPERATION: HTTP POST {subscriber_url} with OrderShippedEvent payload
  PURPOSE: Deliver order shipment notification to external subscribers
  SOURCE: `src/eShop.AppHost/Program.cs` line 56 (webhooksClient reference to webHooksApi)

## Communication Channels

| CHANNEL_TYPE | ENDPOINT / EXCHANGE / TOPIC | SOURCE | NOTES |
|--------------|---------------------------|--------|-------|
| HTTP | Basket.API: http://localhost:port | `src/eShop.AppHost/Program.cs` line 24 | REST API for cart operations; authenticated by Identity.API |
| HTTP | Catalog.API: http://localhost:port | `src/eShop.AppHost/Program.cs` line 28 | REST API for product browsing; supports semantic search with embeddings |
| HTTP | Identity.API: http://localhost:port | `src/eShop.AppHost/Program.cs` line 20 | OAuth2/OIDC identity provider; issues JWT bearer tokens |
| HTTP | Ordering.API: http://localhost:port | `src/eShop.AppHost/Program.cs` line 32 | REST API for order creation and status; includes /health endpoint |
| HTTP | WebApp: http://localhost:19000 | `src/eShop.AppHost/Program.cs` line 66 | Blazor web application; external HTTP endpoint |
| HTTP | mobile-bff (YARP): http://localhost:port | `src/eShop.AppHost/Program.cs` line 45 | Reverse proxy gateway; unified API surface for mobile clients |
| HTTP | WebhookClient: http://localhost:port | `src/eShop.AppHost/Program.cs` line 55 | Test webhook subscriber; receives webhook deliveries |
| MessageQueue | RabbitMQ: amqp://localhost:5672 | `src/eShop.AppHost/Program.cs` line 6 | Exchanges: OrderPlacedEvent, OrderShippedEvent, PaymentRequestedEvent, PaymentSuccessfulEvent, WebhookTriggeredEvent |
| Cache | Redis: localhost:6379 | `src/eShop.AppHost/Program.cs` line 5 | In-memory key-value store; used by Basket.API for session/cart state |
| SQL | PostgreSQL: localhost:5432 | `src/eShop.AppHost/Program.cs` line 7 | Databases: catalogdb, identitydb, orderingdb, webhooksdb; engine: pgvector extension enabled |

## Dependency Registration and Wiring

DI_CONTAINER: Microsoft Aspire DistributedApplication builder pattern + ASP.NET Core built-in dependency injection

### Service Registration via Aspire Extensions

All service registration occurs in `src/eShop.AppHost/Program.cs` using Aspire fluent API extensions:

**Infrastructure Resources:**

```csharp
var redis = builder.AddRedis("redis");
var rabbitMq = builder.AddRabbitMQ("eventbus")
    .WithLifetime(ContainerLifetime.Persistent);
var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithLifetime(ContainerLifetime.Persistent);
```

**Database Isolation:**

```csharp
var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var orderDb = postgres.AddDatabase("orderingdb");
var webhooksDb = postgres.AddDatabase("webhooksdb");
```

**Service Project Registration with Dependencies:**

```csharp
var basketApi = builder.AddProject<Projects.Basket_API>("basket-api")
    .WithReference(redis)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithEnvironment("Identity__Url", identityEndpoint);
```

**Lifetime/Scope Details:**
- Infrastructure resources (Redis, RabbitMQ, PostgreSQL): `ContainerLifetime.Persistent` (container survives Aspire session restarts)
- Service projects: Controlled via `AddProject<T>()` — instantiated and managed by Aspire dashboard

### Extension Methods for Configuration

**File:** `src/eShop.AppHost/Extensions.cs`

| Extension | Scope | Purpose | Code |
|-----------|-------|---------|------|
| `AddForwardedHeaders()` | Global | Inject ASPNETCORE_FORWARDEDHEADERS_ENABLED=true into all projects at startup | Registers `AddForwardHeadersSubscriber` with `IDistributedApplicationEventing` |
| `AddOpenAI()` | Conditional | Configure OpenAI / Azure OpenAI clients for Catalog.API (embeddings) and WebApp (chat) | Accepts `OpenAITarget` enum; creates parameter resources and connection strings |
| `AddOllama()` | Conditional | Configure local Ollama LLM for embeddings and chat; optional GPU support and WebUI | Adds Ollama resource with `all-minilm` embedding model and `llama3.1` chat model |
| `ConfigureMobileBffRoutes()` | YARP Proxy | Define reverse proxy routes for `/catalog-api/`, `/api/orders/`, `/identity/` paths | Fluent route registration with query parameter filtering and path prefix stripping |

## Configuration and Secrets

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|-------------|------|-----------|----------|
| config file | `Logging:LogLevel:Default`, `Logging:LogLevel:Microsoft.AspNetCore` | NO | `src/eShop.AppHost/appsettings.json` |
| environment variable | `ESHOP_USE_HTTP_ENDPOINTS` | NO | Read in `Program.cs` line 124; if set to "1", forces HTTP endpoints instead of HTTPS for CI testing |
| environment variable | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | NO | Injected dynamically by `AddForwardHeadersSubscriber` at startup; enables all services to trust X-Forwarded-* headers |
| environment variable | `ASPNETCORE_URLS` | NO | Set by Aspire for each service; controls HTTP/HTTPS binding |
| environment variable | `Identity__Url` | NO | Set for Basket.API, Ordering.API, WebApp, WebhookClient; points to Identity.API endpoint for token validation |
| Aspire Parameters | `OpenAIEndpointParameter`, `OpenAIKeyParameter`, `ChatModelParameter`, `EmbeddingModelParameter` | YES (Key only) | Lazily evaluated by `Extensions.cs` method `AddOpenAI()`; accepts user input via Aspire dashboard at launch |
| connection string | `OpenAiConnectionString` (computed) | YES | Constructed in-memory in `Extensions.cs` from endpoint and key parameters; passed to Catalog.API and WebApp |
| User Secrets | `b99dbce4-17d4-41d2-858a-2b0529d60bb8` | YES | Read by ASP.NET Core UserSecretsId defined in `eShop.AppHost.csproj`; used for development-time sensitive configuration |

## Persistence and Data Access

**DATABASE:** PostgreSQL 16+ with pgvector extension (image: `ankane/pgvector:latest`)

**DATA_ACCESS:** Entity Framework Core (in each downstream service project)

**MIGRATIONS_PATH:** Each service maintains its own migrations:
- `src/Catalog.API/Infrastructure/Migrations/`
- `src/Identity.API/Infrastructure/Migrations/`
- `src/Ordering.API/Infrastructure/Migrations/`
- `src/Webhooks.API/Infrastructure/Migrations/`

**DATABASE_SCHEMAS:**

| Database | Service | Tables | Purpose |
|----------|---------|--------|---------|
| `catalogdb` | Catalog.API | `Products`, `ProductCategories`, `ProductBrands`, `ProductEmbeddings` (pgvector) | Product catalog with semantic embeddings |
| `identitydb` | Identity.API | `IdentityUser`, `IdentityRole`, `IdentityUserClaim`, `IdentityUserLogin` | User authentication and OAuth state |
| `orderingdb` | Ordering.API, OrderProcessor | `Orders`, `OrderItems`, `OrderStatus`, `OrderStatuses` | Order master and line items |
| `webhooksdb` | Webhooks.API | `WebhookSubscriptions`, `WebhookDeliveries` | Webhook registrations and delivery logs |

**REPOSITORY_PATTERN:** YES

Implemented in each upstream service (not in AppHost). Examples:
- `IBasketRepository` in Basket.API — abstracts Redis operations
- `ICatalogRepository` in Catalog.API — abstracts Product queries
- `IOrderRepository` in Ordering.API — abstracts Order persistence
- `IWebhookRepository` in Webhooks.API — abstracts subscription management

## Patterns and Architecture Notes

| PATTERN | EVIDENCE | SNIPPET |
|---------|----------|---------|
| **Aspire Orchestration** | `src/eShop.AppHost/Program.cs` — all services defined via `builder.AddProject<T>()` | `var identityApi = builder.AddProject<Projects.Identity_API>("identity-api", launchProfileName)` |
| **Dependency Injection (Fluent API)** | Aspire builder extension chain in `Program.cs` — each service references others | `.WithReference(redis).WithReference(rabbitMq).WaitFor(rabbitMq)` |
| **Event-Driven Architecture** | RabbitMQ message exchanges; OrderPlacedEvent → OrderProcessor → OrderShippedEvent | `builder.AddRabbitMQ("eventbus").WithLifetime(ContainerLifetime.Persistent)` |
| **Service Locator (Endpoint Extraction)** | `identityEndpoint = identityApi.GetEndpoint(launchProfileName)` passed to other services | Other services use endpoint to validate tokens; enables late binding of Identity.API URL |
| **Gateway/BFF Pattern** | YARP reverse proxy routes `/catalog-api/`, `/api/orders/`, `/identity/` to backend services | `ConfigureMobileBffRoutes()` in `Extensions.cs` lines 183–248 defines routes with version filtering |
| **Health Check Coordination** | OrderProcessor waits for Ordering.API health endpoint before consuming events | `.WaitFor(orderingApi)` in `Program.cs` line 40; Ordering.API exposes `/health` at line 36 |
| **Eventing/Middleware Injection** | Global middleware applied via `AddForwardHeaders()` subscriber to `IDistributedApplicationEventing` | `AddForwardHeadersSubscriber` in `Extensions.cs` lines 29–38 sets env var on all projects |
| **Feature Flags / Conditional Integration** | Optional OpenAI/Ollama injection; CI testing mode via `ESHOP_USE_HTTP_ENDPOINTS` env var | `if (useOpenAI) { builder.AddOpenAI(...) }` at line 85; `ShouldUseHttpForEndpoints()` at line 124 |
| **Cyclic Reference Resolution** | Identity.API requires callback URLs from all apps, but apps need Identity.API endpoint | Resolved by fetching Identity endpoint first, then passing to other services; Identity.API configured last |

## Security and Operational Considerations

**AUTHN_AUTHZ:**
- Mechanism: OAuth2 Bearer tokens (JWT) issued by Identity.API
- Configuration: `Identity__Url` environment variable set in `Program.cs` for Basket.API (line 25), Ordering.API (line 36), WebApp (line 72), WebhookClient (line 58)
- Forwarded Headers: `X-Forwarded-For`, `X-Forwarded-Proto` trusted globally via `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (injected by `AddForwardHeadersSubscriber`)

**KNOWN_RISKS:**
- NO hard-coded secrets detected in source; OpenAI keys accepted at runtime via Aspire parameter dashboard
- Development-only UserSecretsId in `eShop.AppHost.csproj` — for local dev only; production deployment should use Azure Key Vault or similar
- RabbitMQ configured without explicit credentials in Aspire (relies on default guest:guest); ensure production deployment isolates broker
- YARP routes not explicitly define rate limiting or request validation — downstream services are responsible
- Cyclic reference (Identity.API → all apps) may complicate startup order; mitigated by Aspire's WaitFor dependencies

**OBSERVABILITY:**
- Logging: Configured in `appsettings.json` — default LogLevel: Information, Microsoft.AspNetCore: Warning, Aspire.Hosting.Dcp: Warning
- OTLP (OpenTelemetry) Endpoint: Configured in `launchSettings.json` — `https://localhost:18076` for traces, `https://localhost:19076` for resource service
- Health Checks: Ordering.API exposes `/health` endpoint (`Program.cs` line 36); OrderProcessor waits for it before consuming events
- Aspire Dashboard: Default dashboard at `https://localhost:19076` for observing all services and resources in real-time

**DEPLOYMENT:**
- Dockerfiles: NONE in AppHost (Aspire manages Docker container lifecycle)
- Docker Compose: NONE explicit; Aspire generates equivalent orchestration via Aspire.Hosting
- Kubernetes: NONE in AppHost; deployment would require generating manifests from Aspire or manually creating `deployment.yaml` / `service.yaml` per microservice
- DevCenter: `.devcenter/` integration available for GitHub Codespaces or local Dev Containers
- CI Test Mode: Set `ESHOP_USE_HTTP_ENDPOINTS=1` environment variable in CI to force HTTP instead of HTTPS (supports Playwright E2E tests)
