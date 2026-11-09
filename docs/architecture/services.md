# Services

Each section lists responsibilities, interfaces, data ownership, and notable design choices. Resource names match `AppHost.cs`.

## identity-api (`src/Identity.API`)

- **Role:** OpenID Connect / OAuth2 authority for all clients and APIs.
- **Tech:** Duende IdentityServer 7 on ASP.NET Core Identity (`ApplicationUser`), Razor Pages UI for login/consent.
- **Data:** `identitydb` (users only). Clients, scopes, and API resources are configured in memory (`Config.cs`) using callback URLs injected by the AppHost (`WebAppClient`, `BasketApiClient`, `OrderingApiClient`, etc.).
- **Signing key:** `AddDeveloperSigningCredential()` (development key).
- **Health:** `/health`.

## catalog-api (`src/Catalog.API`)

- **Role:** Product catalog, brands/types, pictures, stock levels, and AI semantic search.
- **API:** Minimal APIs under `api/catalog`, versioned 1.0 and 2.0.

| Method | Route | Version |
| --- | --- | --- |
| GET | `/items` (paged, v2 adds name/type/brand filters) | 1, 2 |
| GET | `/items/by?ids=` , `/items/{id}`, `/items/{id}/pic`, `/items/facets` | 1, 2 |
| GET | `/items/by/{name}`, `/items/type/{typeId}/brand/{brandId?}`, `/items/type/all/brand/{brandId?}` | 1 |
| GET | `/items/withsemanticrelevance/{text}` (v1) / `?text=` (v2) | 1, 2 |
| GET | `/catalogtypes`, `/catalogbrands` | 1, 2 |
| POST | `/items` | 1, 2 |
| PUT | `/items` (v1) / `/items/{id}` (v2) | 1, 2 |
| DELETE | `/items/{id}` | 1, 2 |

- **Data:** `catalogdb` (EF Core `CatalogContext` + `vector` column via pgvector), plus outbox table.
- **AI:** `CatalogAI` generates embeddings with an `IEmbeddingGenerator` (Foundry/OpenAI or Ollama). When unconfigured, semantic search falls back to name search.
- **Events:** publishes `ProductPriceChangedIntegrationEvent` (on price update), `OrderStockConfirmed/RejectedIntegrationEvent`; consumes `OrderStatusChangedToAwaitingValidation` and `OrderStatusChangedToPaid` (decrements stock).
- **Auth:** none - endpoints, including write endpoints, are anonymous.

## basket-api (`src/Basket.API`)

- **Role:** Per-user shopping basket.
- **API:** gRPC service `Basket` (`GetBasket` [anonymous - returns empty for unauthenticated], `UpdateBasket`, `DeleteBasket`), user resolved from the JWT `sub` claim.
- **Data:** Redis (`RedisBasketRepository`), JSON-serialized basket per buyer key. No durable store.
- **Events:** consumes `OrderStartedIntegrationEvent` to clear the basket after checkout.

## ordering-api (`src/Ordering.API`, `Ordering.Domain`, `Ordering.Infrastructure`)

- **Role:** Order lifecycle - the core bounded context.
- **API:** `api/orders` v1.0, requires authorization.

| Method | Route | Handler |
| --- | --- | --- |
| POST | `/` (requires `x-requestid` header) | `CreateOrderCommand` (idempotent via `IdentifiedCommand`) |
| POST | `/draft` | `CreateOrderDraftCommand` |
| GET | `/`, `/{orderId}`, `/cardtypes` | `IOrderQueries` (read side) |
| PUT | `/cancel`, `/ship` | `CancelOrderCommand`, `ShipOrderCommand` |

- **Design:**
  - **Domain layer** - aggregates `Order` (with `OrderItem`, `Address`) and `Buyer` (with `PaymentMethod`), `Entity`/`ValueObject`/`Enumeration` base types, domain events (`OrderStartedDomainEvent`, `OrderStatusChangedTo*DomainEvent`, etc.). Status: `Submitted -> AwaitingValidation -> StockConfirmed -> Paid -> Shipped`, or `Cancelled`.
  - **Application layer** - MediatR commands + pipeline behaviors `LoggingBehavior -> ValidatorBehavior (FluentValidation) -> TransactionBehavior`. `TransactionBehavior` wraps the command in a DB transaction and, after commit, publishes outbox events.
  - **Infrastructure layer** - `OrderingContext` (EF Core, `ordering` schema), repositories, `RequestManager` for idempotency, domain events dispatched on `SaveEntitiesAsync`.
  - **Read side** - `OrderQueries` queries the same database (logical CQRS, single store).
- **Data:** `orderingdb` (runs EF migrations; `order-processor` waits for it).
- **Events:** publishes `OrderStarted`, `OrderStatusChangedTo{Submitted,AwaitingValidation,StockConfirmed,Paid,Shipped,Cancelled}`; consumes `GracePeriodConfirmed`, `OrderStock{Confirmed,Rejected}`, `OrderPayment{Succeeded,Failed}`.

## order-processor (`src/OrderProcessor`)

- **Role:** Background worker (`GracePeriodManagerService`) that polls `orderingdb` every `CheckUpdateTime` seconds for `Submitted` orders older than `GracePeriodTime` minutes and publishes `GracePeriodConfirmedIntegrationEvent`.
- **Note:** Reads Ordering's database directly (shared-database coupling by design).

## payment-processor (`src/PaymentProcessor`)

- **Role:** Simulated payment gateway. Consumes `OrderStatusChangedToStockConfirmed` and publishes `OrderPaymentSucceeded` or `OrderPaymentFailed` based on the `PaymentSucceeded` config flag.

## webhooks-api (`src/Webhooks.API`)

- **Role:** Lets authenticated users register webhook subscriptions and delivers events to them.
- **API:** `api/webhooks` v1.0 (GET list, GET by id, POST, DELETE), requires authorization. On registration it validates the target via a "grant URL" handshake.
- **Data:** `webhooksdb`.
- **Events:** consumes `ProductPriceChanged`, `OrderStatusChangedToShipped`, `OrderStatusChangedToPaid`; POSTs payloads with a token header to subscriber URLs.

## webhooksclient (`src/WebhookClient`)

- Blazor sample app that signs in via Identity, registers subscriptions, and exposes `POST /webhook-received` to display received hooks.

## webapp (`src/WebApp`, `src/WebAppComponents`)

- **Role:** Customer storefront (Blazor interactive server).
- **Integrations:** `CatalogService`/`OrderingService` via HTTP (service discovery `https+http://...`), `BasketService` via gRPC, OIDC login against Identity, AI chatbot (`Components/Chatbot`, `IChatClient`) when an AI provider is configured.
- **Events:** subscribes to all `OrderStatusChangedTo*` events to push live order-status updates to the user's open "My orders" page (`OrderStatusNotificationService`).

## mobile-bff (YARP)

- Declared in AppHost (`ConfigureMobileBffRoutes`). Routes `/catalog-api/...` (with exact `api-version` matching), `/api/orders/{*any}`, and `/identity/{*any}` to the respective services.

## ClientApp / HybridApp

- .NET MAUI app (MVVM, `ClientApp`) and MAUI Blazor Hybrid app (`HybridApp`) consuming the BFF/APIs. Built by the `pr-validation-maui` workflow, not orchestrated by AppHost.

## Shared libraries

| Project | Purpose |
| --- | --- |
| `eShop.ServiceDefaults` | `AddServiceDefaults()` - OpenTelemetry, health checks (`/health`, `/alive`), service discovery, standard HTTP resilience; `AddDefaultAuthentication()` (JWT bearer); OpenAPI + Scalar; claims helpers |
| `EventBus` | `IEventBus`, `IIntegrationEventHandler<T>`, `IntegrationEvent`, `AddSubscription<TEvent,THandler>()` |
| `EventBusRabbitMQ` | `RabbitMQEventBus` hosted service - publishes to the `eshop_event_bus` direct exchange (routing key = event type name), consumes one durable queue per `SubscriptionClientName`, Polly retry on publish, OTel propagation |
| `IntegrationEventLogEF` | Outbox: `IntegrationEventLogEntry` + `IntegrationEventLogService` (`NotPublished -> InProgress -> Published / PublishedFailed`) |
