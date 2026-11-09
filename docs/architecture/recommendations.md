# Recommendations

Suggestions for what to change or add, grounded in the current code. Each item cites the evidence. Priority: **P0** = correctness/security risk, **P1** = production readiness, **P2** = evolution/enhancement.

## Summary

| # | Recommendation | Priority | Effort |
| --- | --- | --- | --- |
| 1 | Secure Catalog write endpoints and enable audience validation | P0 | S |
| 2 | Add an outbox relay (retry unpublished events) | P0 | M |
| 3 | Dead-letter + retry instead of ack-on-failure in the consumer | P0 | S-M |
| 4 | Idempotent consumers (inbox / processed-message table) | P0 | M |
| 5 | Reserve stock at validation; reject unknown products | P0 | M |
| 6 | Production identity configuration | P1 | M |
| 7 | Production health probes, rate limiting, caching | P1 | S |
| 8 | Managed Azure backing services + environment configs | P1 | M |
| 9 | Shared, versioned event contracts | P1 | M |
| 10 | Signed webhooks with retry | P1 | M |
| 11 | Close test gaps; contract and saga tests | P1 | M |
| 12 | Business metrics, SLOs, alerting | P1 | S |
| 13 | Replace polling grace-period worker | P2 | M |
| 14 | Real payment integration boundary | P2 | M |
| 15 | Admin / back-office UI | P2 | L |
| 16 | AI improvements | P2 | M |
| 17 | Delivery pipeline: deploy, security scanning, IaC | P1 | M |

## P0 - Correctness and security

### 1. Secure Catalog writes and validate token audience

- **Evidence:** `Catalog.API` never calls `AddDefaultAuthentication`/`RequireAuthorization`; `POST/PUT/DELETE /api/catalog/items` are anonymous and a price update fans out to webhooks. `ServiceDefaults/AuthenticationExtensions.cs` sets `ValidateAudience = false`, so a token issued for `webhooks` is accepted by `orders`.
- **Do:** add JWT auth to Catalog; protect write routes with an `Admin`/`catalog.write` policy and keep reads anonymous. Turn on audience validation (each API already configures `Identity:Audience`). Add scope-based policies per API.

### 2. Outbox relay

- **Evidence:** `IntegrationEventLogService.RetrieveEventLogsPendingToPublishAsync(transactionId)` is only called right after commit for that transaction. Rows left `NotPublished` (crash) or `PublishedFailed` (broker down) are never retried, so the order saga stalls silently.
- **Do:** add a `BackgroundService` per publishing service (or a shared one in `IntegrationEventLogEF`) that periodically publishes stale `NotPublished`/`PublishedFailed` rows with backoff and a max-attempt count (`TimesSent` already exists). Alternatively adopt a library that provides outbox + inbox + retries (MassTransit, Wolverine, NServiceBus) - see item 9.

### 3. Dead-letter and retry on consume

- **Evidence:** `RabbitMQEventBus.OnMessageReceived` catches all exceptions and still calls `BasicAckAsync` (the code comment says a DLX is needed in production). A transient DB error in `OrderStockConfirmed` handling loses the event permanently.
- **Do:** declare queues with `x-dead-letter-exchange`; `BasicNack(requeue:false)` after N in-process retries (Polly), plus a retry queue with TTL for delayed redelivery. Surface DLQ depth as a metric/alert.

### 4. Idempotent consumers

- **Evidence:** RabbitMQ delivers at-least-once and the outbox relay (item 2) will cause duplicates. `OrderStatusChangedToPaidIntegrationEventHandler` calls `RemoveStock` unconditionally - a redelivery decrements stock twice. Ordering has an idempotency `requests` table only for HTTP commands.
- **Do:** add an inbox table (event id + handler) checked in the same transaction as the handler's writes, or make handlers state-guarded (e.g. Ordering commands already check status transitions; Catalog does not).

### 5. Stock reservation and validation

- **Evidence:** `OrderStatusChangedToAwaitingValidationIntegrationEventHandler` only *checks* `AvailableStock >= Units`; stock is decremented much later on `Paid`. Concurrent orders can all pass validation and oversell. Products not found (`Find` returns null) are skipped, so an order with a deleted product is confirmed.
- **Do:** reserve stock at validation (decrement or a `ReservedStock` column) with optimistic concurrency (`xmin` row version), release on `Cancelled`/payment failure, and treat missing products as rejected.

## P1 - Production readiness

### 6. Identity hardening

- **Evidence:** `AddDeveloperSigningCredential()`, in-memory clients/scopes in `Config.cs`, seeded demo users, permissive `ValidIssuers` including `https://10.0.2.2:5243` in debug builds.
- **Do:** load signing keys from Azure Key Vault (Duende automatic key management with a persisted store), move clients to the EF configuration store, gate seeding to Development, and plan for Duende licensing or evaluate Entra External ID / Keycloak.

### 7. Health probes, rate limiting, caching

- **Evidence:** `MapDefaultEndpoints` maps `/health` and `/alive` only when `IsDevelopment()`; ACA cannot probe them in production. No `AddRateLimiter`, `AddOutputCache`, or `HybridCache` anywhere.
- **Do:** expose liveness/readiness in all environments (restrict detail, or use a separate port); add dependency checks (Postgres, RabbitMQ, Redis). Add output caching / `HybridCache` (Redis already present) for catalog reads, brands, types, and pictures. Add rate limiting on public endpoints (catalog search, AI chat, webhook registration).

### 8. Managed backing services and environments

- **Evidence:** AppHost uses `ankane/pgvector:latest` (unpinned tag), RabbitMQ and Redis containers, even for the `aca` deployment.
- **Do:** use `AddAzurePostgresFlexibleServer(...).RunAsContainer()` (pgvector extension enabled), `AddAzureManagedRedis`/`AddAzureRedis().RunAsContainer()`, and consider Azure Service Bus for production messaging behind the existing `IEventBus`. Pin image tags. Add Key Vault and managed identity for connection secrets. Use `builder.ExecutionContext.IsPublishMode` / parameters for per-environment settings.

### 9. Shared, versioned event contracts

- **Evidence:** `OrderStatusChangedToPaidIntegrationEvent` is defined four times (Ordering, Catalog, WebApp, Webhooks). Routing uses the CLR type name, so a rename or field change silently breaks consumers.
- **Do:** create an `eShop.IntegrationEvents.Contracts` package (or schema registry with JSON Schema/AsyncAPI), add explicit event versioning, and add consumer-driven contract tests. Publish an AsyncAPI document alongside the existing OpenAPI docs.

### 10. Webhook delivery reliability and security

- **Evidence:** Webhooks.API POSTs synchronously inside the event handler with a static token header; failures are logged and dropped.
- **Do:** sign payloads (HMAC-SHA256 with per-subscription secret, timestamp to prevent replay), persist deliveries with retry/backoff and a delivery log, validate target URLs to prevent SSRF (block private IP ranges), and let users view delivery history.

### 11. Testing gaps

- **Evidence:** no tests for PaymentProcessor, OrderProcessor, Identity.API, `EventBusRabbitMQ`, or `IntegrationEventLogEF`; Webhooks and Basket functional suites have 2 tests each.
- **Do:** add an Aspire-based integration test (`DistributedApplicationTestingBuilder`) that drives a full checkout and asserts the order reaches `Paid`; add compensating-path tests (stock rejected, payment failed); add event contract tests; add load tests (k6/Azure Load Testing) for catalog and checkout.

### 12. Business observability

- **Evidence:** only framework metrics are emitted.
- **Do:** add a `Meter` per service for orders created/paid/cancelled, stock rejections, payment failures, outbox backlog, DLQ depth, webhook failures, and AI token usage/latency. Define SLOs (checkout success rate, p95 order-to-paid latency) and wire alerts in Azure Monitor / Application Insights.

### 17. Delivery pipeline

- **Evidence:** CI builds and tests but does not deploy; no dependency/container/secret scanning.
- **Do:** add a deploy workflow (`aspire deploy` or `azd`) with environments and approvals, Dependabot/Renovate, CodeQL, container image scanning, and SBOM generation.

## P2 - Evolution and new capabilities

### 13. Replace polling grace-period worker

- **Evidence:** `GracePeriodManagerService` polls `orderingdb` on a timer and reads another service's database directly.
- **Do:** publish the grace-period confirmation as a scheduled/delayed message (Service Bus scheduled messages, RabbitMQ delayed exchange) from Ordering itself, or move the worker inside the Ordering bounded context. This removes cross-service DB coupling.

### 14. Payment boundary

- **Evidence:** `PaymentProcessor` returns success/failure from a config flag; card data flows into Ordering (`CardNumber`, `CardSecurityNumber` on `CreateOrderCommand`).
- **Do:** integrate a PSP (Stripe/Adyen) with tokenized payment methods so raw card data never touches AI Immersion eShop (PCI scope reduction), plus refunds as a compensating action for cancel-after-paid.

### 15. Back-office capabilities

- No UI exists for catalog management, order shipping (`PUT /ship` is API-only), or stock replenishment. Add an admin Blazor app (role-protected) that uses the secured endpoints from item 1.

### 16. AI improvements

- **Evidence:** embeddings are truncated to 384 dimensions (`CatalogAI.EmbeddingDimensions`) without re-normalization; no vector index; chatbot has no guardrails, evaluation, or token budgeting.
- **Do:** request reduced dimensions from the model directly (`text-embedding-3-small` supports `dimensions`), add an HNSW index on `Embedding`, re-embed on product description changes via an event, add content safety and rate limits for the chatbot, and add AI evaluation tests (`Microsoft.Extensions.AI.Evaluation`). Consider hybrid search (full-text + vector).

### Other ideas

- **API gateway for web too:** route WebApp traffic through YARP (or Azure API Management) for consistent auth, rate limiting, and versioning.
- **Read models:** move `OrderQueries` to a denormalized read model if order history grows.
- **Basket durability:** Redis is the only store; enable persistence/replication or treat abandoned-basket data as a business signal (events to a marketing service).
- **Feature flags:** use Azure App Configuration / `Microsoft.FeatureManagement` instead of code toggles like `useOllama = false`.
- **Architecture decision records:** add `docs/adr/` to capture the decisions above.
