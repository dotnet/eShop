# Azure Migration — Architecture Decision Record

**Project:** eShop (fork of `dotnet/eShop`)
**Target platform:** Azure Container Apps
**Document status:** Living document — updated as decisions are made
**Last updated:** 2026-10-08

---

## 1. Purpose

This document records the architectural decisions taken to move eShop from its
local, container-based development topology to a production-shaped deployment on
Azure. The upstream sample is explicitly designed for evaluation: it provisions
disposable PostgreSQL, Redis and RabbitMQ containers and keeps several
"not recommended for production" shortcuts. Each of those is tracked here as a
decision with its rationale and consequences.

Decisions are numbered `ADR-NNN` and carry one of these states:

| State | Meaning |
|---|---|
| **Accepted** | Decided. Implementation pending or in progress. |
| **Proposed** | Recommended, awaiting sign-off. |
| **Open** | Not yet decided. Listed in section 6. |

---

## 2. Starting point

The application is orchestrated by .NET Aspire. `src/eShop.AppHost/Program.cs`
is the single source of truth for topology, and it already declares the target:

```csharp
builder.AddAzureContainerAppEnvironment("aca");
```

### 2.1 Services

| Component | Kind | State store | Responsibility |
|---|---|---|---|
| `Identity.API` | Web + Duende IdentityServer | `identitydb` | OIDC/OAuth2 authority for all clients |
| `Catalog.API` | Minimal API | `catalogdb` + pgvector | Products, brands, stock, semantic search |
| `Basket.API` | gRPC | Redis | Shopping cart |
| `Ordering.API` | Minimal API, DDD + CQRS | `orderingdb` | Order and Buyer aggregates |
| `OrderProcessor` | Worker | polls `orderingdb` | Grace-period timer |
| `PaymentProcessor` | Worker | — | Payment simulator (config-driven outcome) |
| `Webhooks.API` | Minimal API | `webhooksdb` | Third-party event subscriptions |
| `WebApp` | Blazor Interactive Server | — | Storefront |
| `mobile-bff` | YARP resource (not a project) | — | Backend-for-frontend for mobile |
| `WebhookClient` | Web | — | Demo webhook subscriber |
| `ClientApp` / `HybridApp` | .NET MAUI | — | Client apps, not deployed to Azure |

### 2.2 Architectural patterns in use

Domain-Driven Design (aggregates, value objects, domain events, repositories,
unit of work), CQRS with MediatR, pipeline behaviours (logging, validation,
transaction), transactional outbox, idempotent commands, database-per-service,
choreographed saga, backend-for-frontend, and a shared cross-cutting
`ServiceDefaults` package providing service discovery, Polly resilience,
OpenTelemetry and health checks.

### 2.3 Order saga

```
WebApp -> Ordering.API : CreateOrderCommand (wrapped in IdentifiedCommand)
  Order created with status Submitted
  OrderStartedDomainEvent -> create/validate Buyer
  [outbox] OrderStartedIntegrationEvent -> Basket.API clears the cart

OrderProcessor polls orderingdb
  grace period elapsed -> GracePeriodConfirmedIntegrationEvent
    -> Ordering.API : SetAwaitingValidationOrderStatus
       -> OrderStatusChangedToAwaitingValidationIntegrationEvent
          -> Catalog.API : stock check
             -> OrderStockConfirmed | OrderStockRejected

Ordering.API : SetStockConfirmedOrderStatus
  -> OrderStatusChangedToStockConfirmedIntegrationEvent
     -> PaymentProcessor -> OrderPaymentSucceeded | OrderPaymentFailed

Ordering.API : SetPaidOrderStatus
  -> OrderStatusChangedToPaidIntegrationEvent
     -> Catalog.API decrements stock
     -> Webhooks.API notifies subscribers
```

Shipping is triggered manually through `ShipOrderCommand`.

---

## 3. Decision log

| ID | Decision | State |
|---|---|---|
| ADR-001 | Host on Azure Container Apps | Accepted |
| ADR-002 | PostgreSQL to Azure Database for PostgreSQL Flexible Server | Accepted |
| ADR-003 | Redis to Azure Cache for Redis | Accepted |
| ADR-004 | RabbitMQ to Azure Service Bus | Accepted |
| ADR-005 | Azure SignalR Service for Blazor Server scale-out | Accepted |
| ADR-006 | EF Core migrations move to a dedicated init job | Accepted |
| ADR-007 | Replace the grace-period poller with scheduled messages | Proposed |
| ADR-008 | Key Vault plus Managed Identity for all secrets | Accepted |
| ADR-009 | Export telemetry to Azure Monitor / Application Insights | Accepted |
| ADR-010 | Stable custom domain for the identity authority | Proposed |
| ADR-011 | Replace the developer signing credential | Proposed |
| ADR-012 | Persist the Data Protection key ring | Proposed |
| ADR-013 | Azure Container Registry for images | Accepted |

---

## 4. Decisions in detail

### ADR-001 — Host on Azure Container Apps

**State:** Accepted

**Context.** The AppHost already calls `AddAzureContainerAppEnvironment("aca")`.
Aspire can generate and apply the Container Apps topology directly from the
application model via `aspire deploy`.

**Decision.** Keep Azure Container Apps as the compute platform. Do not
introduce AKS.

**Consequences.** Ingress TLS terminates at the platform, so applications
receive plain HTTP internally — forwarded headers must be honoured (see
ADR-010). Scale-to-zero is available but is a poor fit for the workers and for
Blazor Server. Internal service-to-service DNS is provided by the environment,
which matches Aspire's service discovery.

---

### ADR-002 — PostgreSQL to Azure Database for PostgreSQL Flexible Server

**State:** Accepted

**Context.** The AppHost provisions an `ankane/pgvector` container with
`ContainerLifetime.Persistent`, hosting four logical databases: `catalogdb`,
`identitydb`, `orderingdb`, `webhooksdb`. In Container Apps this container is
disposable; all data is lost on restart.

**Decision.** Provision an Azure Database for PostgreSQL Flexible Server and
keep the four databases as separate logical databases on it, preserving the
database-per-service boundary at the logical level.

**Implementation notes.**

- Use `Aspire.Hosting.Azure.PostgreSQL` and `AddAzurePostgresFlexibleServer`.
- `Catalog.API` stores a `Vector?` column and requires the `vector` extension.
  On Flexible Server, extensions must be allow-listed through the
  `azure.extensions` server parameter before `CREATE EXTENSION` will succeed.
  This is a common first-deploy failure.
- Embedding dimensionality is fixed at 384 in `CatalogAI.cs`.
- Prefer Microsoft Entra authentication over a password (see ADR-008).

**Consequences.** One server with four databases is cheaper and simpler than
four servers, at the cost of a shared failure domain and shared throughput. This
is acceptable for the current goals and can be split later without application
changes, since each service already holds its own connection string.

---

### ADR-003 — Redis to Azure Cache for Redis

**State:** Accepted

**Context.** `Basket.API` keeps the entire shopping cart in Redis via
`RedisBasketRepository`. The AppHost runs it as a plain container, so carts do
not survive a restart.

**Decision.** Provision Azure Cache for Redis.

**Implementation notes.**

- Use `Aspire.Hosting.Azure.Redis` and `AddAzureRedis`.
- Choose a tier that offers an availability SLA; the Basic tier is single-node
  and has none.
- Enable Microsoft Entra authentication and remove the access key (see ADR-008).

**Consequences.** The cart becomes durable across deployments. Note that Redis
remains the system of record for baskets — there is no relational fallback — so
the eviction policy matters and must not be `allkeys-lru`.

---

### ADR-004 — RabbitMQ to Azure Service Bus

**State:** Accepted

**Context.** `EventBusRabbitMQ` publishes to a single direct exchange
(`eshop_event_bus`) with the event type name as routing key, and each service
declares its own queue bound to the event types it subscribes to. The
`IEventBus` abstraction in `src/EventBus` already isolates callers from the
transport.

**Decision.** Replace RabbitMQ with Azure Service Bus. Add a new
`EventBusServiceBus` project implementing `IEventBus`, mirroring the structure
of `EventBusRabbitMQ`. Delete `EventBusRabbitMQ` once parity is confirmed.

**Topology mapping.**

| RabbitMQ | Azure Service Bus |
|---|---|
| Exchange `eshop_event_bus` (direct) | Topic `eshop-event-bus` |
| One queue per service | One subscription per service |
| Routing key = event type name | Correlation filter on the `Subject` property |
| Polly retry, 10 attempts | Native delivery count and dead-letter queue |
| (not available) | Scheduled messages — enables ADR-007 |

**Implementation notes.**

- Use `Aspire.Hosting.Azure.ServiceBus` and `Aspire.Azure.Messaging.ServiceBus`.
- Grant `Azure Service Bus Data Sender` and `Azure Service Bus Data Receiver`
  per service, scoped as narrowly as practical.
- Dead-letter queues must be monitored; silently filling DLQs is the usual
  failure mode after this kind of migration.

**Consequences.** This is the largest single piece of work in the migration, but
it is well bounded because the abstraction already exists. It unlocks ADR-007,
removes a stateful container, and replaces hand-rolled retry with
platform-level delivery guarantees and dead-lettering.

---

### ADR-005 — Azure SignalR Service for Blazor Server scale-out

**State:** Accepted

**Context.** `WebApp` uses `AddInteractiveServerComponents()` and
`AddInteractiveServerRenderMode()`. Every user holds a stateful SignalR circuit
pinned to one server instance. Running more than one replica without help causes
clients to be routed to a replica that does not own their circuit.

**Decision.** Add Azure SignalR Service in **Default** mode and wire it with
`AddSignalR().AddAzureSignalR()`.

**Implementation notes.**

- Serverless mode is not supported for Blazor Server. Default mode is required.
- Azure SignalR maintains the client-to-app-server mapping and routes messages
  back to the server that owns the circuit, which is what makes horizontal
  scaling work.
- Circuit *state* still lives in the app server's memory. A replica restart
  still drops its circuits; clients reconnect and lose in-memory component
  state. This is inherent to Blazor Server, not something SignalR Service fixes.

**Consequences.** Connection management is offloaded, so `WebApp` can scale
horizontally. Adds a per-connection cost. Combine with ADR-012, since cookie
authentication breaks across replicas independently of SignalR.

---

### ADR-006 — EF Core migrations move to a dedicated init job

**State:** Accepted

**Context.** `Catalog.API`, `Identity.API`, `Ordering.API` and `Webhooks.API`
each call `AddMigration<TContext, TSeeder>()`, which registers an
`IHostedService` that runs `Database.MigrateAsync()` and then a seeder at
startup (`src/Shared/MigrateDbContextExtensions.cs:65`). `Identity.API` even
carries an inline comment stating the approach is not recommended for
production. With N replicas starting concurrently this is a race, and it couples
application readiness to schema changes.

**Decision.** Extract migrations and seeding into a dedicated console project
run as a **Container Apps Job** (manual trigger, executed as a deployment step
before services roll out). Remove the `AddMigration` hosted services from the
services themselves.

**Implementation notes.**

- The new project references all four `DbContext` types.
- `CatalogContextSeed` generates embeddings and therefore needs the AI backend
  available when the job runs; otherwise it seeds without embeddings.
- The job must complete successfully before the service revision is activated.
- `src/Shared/MigrateDbContextExtensions.cs` becomes job-only code.

**Consequences.** Schema changes become an explicit, observable deployment step
with its own exit code, instead of an implicit side effect of a container
starting. Services start faster and can scale without coordination. Rollbacks
need thought: a job that applies a destructive migration cannot be undone by
redeploying the previous revision.

---

### ADR-007 — Replace the grace-period poller with scheduled messages

**State:** Proposed

**Context.** `OrderProcessor` does not call `Ordering.API`. It opens a raw
Npgsql connection to `orderingdb` and issues literal SQL against another
service's table (`src/OrderProcessor/Services/GracePeriodOrdersRepository.cs:27`):

```sql
SELECT "Id" FROM ordering.orders
WHERE CURRENT_TIMESTAMP - "OrderDate" >= @GracePeriodTime
  AND "OrderStatus" = 'Submitted'
```

This couples the worker to Ordering's physical schema: schema name, table name,
three column names, and the literal `'Submitted'` which is the name of
Ordering's enum member. The existing `catch (NpgsqlException)` returns an empty
list and only logs, so a schema change would leave every order stuck in
`Submitted` with no exception and no alert.

The service exists only because RabbitMQ, as configured, has no delayed-delivery
support, so a business timer had to be implemented as a polling loop.

**Decision.** Once ADR-004 lands, have `Ordering.API` publish
`GracePeriodConfirmedIntegrationEvent` as a **scheduled** Service Bus message at
order creation time, with `ScheduledEnqueueTime = OrderDate + gracePeriod`.
Delete the `OrderProcessor` project.

**Consequences.** Removes a container app, a cross-service database connection,
a polling loop, and a silent failure mode, while restoring the
database-per-service boundary. Order cancellation during the grace period must
then cancel the scheduled message (Service Bus supports cancellation by sequence
number), which requires storing that sequence number against the order — the one
piece of new state this introduces.

**If not accepted:** `OrderProcessor` must stay pinned to a single replica. It
has no row locking, so multiple replicas publish duplicate events. Those
duplicates are currently harmless because `Order.SetAwaitingValidationStatus()`
is guarded by `if (OrderStatus == OrderStatus.Submitted)`, but the guard is
incidental, not a designed idempotency barrier.

---

### ADR-008 — Key Vault plus Managed Identity for all secrets

**State:** Accepted

**Context.** Every credential today travels as a plain environment variable
injected by the AppHost.

**Decision.** Use a user-assigned Managed Identity per service. Prefer
token-based access over secrets wherever the resource supports it, and use Key
Vault only for material that genuinely cannot be replaced by a token.

**Target mapping.**

| Resource | Mechanism | Secret in Key Vault? |
|---|---|---|
| PostgreSQL Flexible Server | Entra authentication | No |
| Azure Cache for Redis | Entra authentication | No |
| Azure Service Bus | RBAC data roles | No |
| Azure SignalR Service | Entra authentication | No |
| Application Insights | Connection string | Yes (not sensitive, but centralised) |
| IdentityServer signing certificate | Key Vault certificate | Yes — see ADR-011 |
| Data Protection key ring | Key Vault key wrapping Blob storage | Yes — see ADR-012 |
| Duende licence key, if required | Key Vault secret | Yes — see section 6 |

**Consequences.** Most of the system ends up with no secrets at all, which is
the desired end state. Each service needs its own identity and role assignments,
which adds infrastructure-as-code surface. Local development must keep working:
`DefaultAzureCredential` falls back to developer credentials, and the Aspire
local topology should remain usable with containers.

---

### ADR-009 — Export telemetry to Azure Monitor / Application Insights

**State:** Accepted

**Context.** `ServiceDefaults` always configures OpenTelemetry tracing, metrics
and logging, but registers the exporters conditionally
(`src/eShop.ServiceDefaults/Extensions.cs:87`):

```csharp
var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
```

Locally, Aspire sets that variable to the dashboard endpoint. In Azure nothing
sets it, so the application instruments everything and discards it. The failure
is silent: no error, no warning, simply no telemetry.

**Decision.** Add an Application Insights resource to the AppHost
(`AddAzureApplicationInsights`) and call `UseAzureMonitor()` from
`Azure.Monitor.OpenTelemetry.AspNetCore` in `ServiceDefaults`, alongside the
existing OTLP path so local development is unaffected.

**Consequences.** Gives distributed tracing across the full order saga, the
application map, and Live Metrics. The saga crosses six services and a message
broker, so end-to-end correlation is the main diagnostic tool available — this
is not optional tooling.

**Alternative considered.** Enabling the Container Apps managed OpenTelemetry
agent, which sets `OTEL_EXPORTER_OTLP_ENDPOINT` for every container. Requires no
code change, but provides less than the native Azure Monitor integration.

---

### ADR-010 — Stable custom domain for the identity authority

**State:** Proposed

**Context.** Three distinct problems overlap here.

*The dependency cycle.* `WebApp` needs Identity's URL to redirect users to
login. Identity needs `WebApp`'s URL to register it as a permitted redirect
target (`src/Identity.API/Configuration/Config.cs:93`):

```csharp
RedirectUris = { $"{configuration["WebAppClient"]}/signin-oidc" }
```

`src/eShop.AppHost/Program.cs:109-113` resolves this by injecting endpoints in
both directions. OIDC requires exact redirect-URI matching, and in Container
Apps the FQDN (`<app>.<suffix>.<region>.azurecontainerapps.io`) does not exist
until the environment and app have been created.

*Scheme mismatch.* Container Apps terminates TLS at the ingress, so Identity
sees plain HTTP and will generate `http://` URIs while the browser arrived over
`https://`. `AddForwardedHeaders()` in the AppHost exists to prevent this; it
must keep working.

*Issuer mismatch — the one that actually breaks the system.* In
`src/eShop.ServiceDefaults/AuthenticationExtensions.cs:43-47`:

```csharp
options.Authority = identityUrl;
options.TokenValidationParameters.ValidIssuers = [identityUrl];
```

`Basket.API` and `Ordering.API` receive `Identity__Url` pointing at the
*internal* address, while the browser obtains its token from the *external*
address, which is what Duende writes into the `iss` claim. The token is then
rejected by every API with a 401 even though login succeeded.

**Decision.** Assign a custom domain to `Identity.API` (for example
`login.<domain>`), identical internally and externally, and pin
`options.IssuerUri` to it.

**Consequences.** The authority URL becomes a configuration constant rather than
a deploy-time discovery, which breaks the cycle at its root. Requires a DNS zone
and a managed certificate. Internal callers resolve the public name, so traffic
leaves and re-enters the ingress unless private DNS is configured — acceptable
at this scale, worth revisiting later.

---

### ADR-011 — Replace the developer signing credential

**State:** Proposed

**Context.** `src/Identity.API/Program.cs:37` ends the IdentityServer
registration with `.AddDeveloperSigningCredential()`, and line 30 sets
`options.KeyManagement.Enabled = false`. The code carries its own warning:
*"Not recommended for production - you need to store your key material somewhere
secure"*. The signing key is generated at startup into a temporary file, so each
replica generates a different key and every restart invalidates all issued
tokens.

**Decision.** Store an X.509 signing certificate in Key Vault and load it with
`AddSigningCredential`, or enable IdentityServer automatic key management backed
by shared persistent storage.

**Consequences.** Without this, `Identity.API` cannot run more than one replica,
and every deployment logs all users out. The symptom — intermittent 401s that
disappear on retry — is hard to attribute, which is why this is a blocker rather
than a cleanup.

---

### ADR-012 — Persist the Data Protection key ring

**State:** Proposed

**Context.** No `AddDataProtection()` call exists anywhere in `src`. ASP.NET
Core therefore falls back to writing the key ring to the container filesystem,
which is ephemeral. `WebApp` uses cookie authentication
(`src/WebApp/Extensions/Extensions.cs:68`).

**Decision.** Configure Data Protection with `PersistKeysToAzureBlobStorage`
and `ProtectKeysWithAzureKeyVault`, applied through `ServiceDefaults` so every
service shares one configuration.

**Consequences.** Without this, a cookie encrypted by one replica cannot be
decrypted by another, producing random logouts under scale-out and a full logout
on every restart. It also breaks Blazor antiforgery tokens. Like ADR-011, it
fails intermittently rather than loudly.

---

### ADR-013 — Azure Container Registry for images

**State:** Accepted

**Context.** Aspire builds container images from the project references in the
AppHost and needs a registry the Container Apps environment can pull from.

**Decision.** Provision an Azure Container Registry and grant the Container Apps
environment's managed identity the `AcrPull` role.

**Consequences.** No registry credentials are stored. The existing
`build/acr-build/queue-all.ps1` and
`build/multiarch-manifests/create-manifests.ps1` scripts are upstream artefacts
and are not part of this pipeline.

---

## 5. Target component mapping

| Current | Target on Azure | ADR |
|---|---|---|
| `ankane/pgvector` container | Azure Database for PostgreSQL Flexible Server | ADR-002 |
| Redis container | Azure Cache for Redis | ADR-003 |
| RabbitMQ container | Azure Service Bus (topic + subscriptions) | ADR-004 |
| In-process SignalR | Azure SignalR Service (Default mode) | ADR-005 |
| Migrations at service startup | Container Apps Job | ADR-006 |
| `OrderProcessor` polling loop | Service Bus scheduled messages | ADR-007 |
| Environment-variable secrets | Managed Identity + Key Vault | ADR-008 |
| Aspire dashboard (OTLP) | Application Insights | ADR-009 |
| Generated ACA FQDN for identity | Custom domain, pinned issuer | ADR-010 |
| Developer signing credential | Key Vault certificate | ADR-011 |
| Ephemeral Data Protection keys | Blob storage + Key Vault | ADR-012 |
| Local image build | Azure Container Registry | ADR-013 |
| `ClientApp`, `HybridApp` (MAUI) | Not deployed — distributed via app stores | — |

---

## 6. Open questions

1. **Duende IdentityServer licensing.** Commercial above a revenue threshold.
   Acceptable for learning; a real cost in production. Alternative: Microsoft
   Entra External ID, which would also dissolve ADR-010, ADR-011 and much of the
   callback-URL complexity, at the price of rewriting the authentication setup.
2. **AI backend.** `UseFoundry` is false by default, and the Ollama path is dead
   code (`bool useOllama = false`, `src/eShop.AppHost/Program.cs:88`). Decide
   whether semantic search and the chatbot ship at all. If Foundry stays, it is
   a preview hosting integration.
3. **`PaymentProcessor` is a simulator.** Its outcome is driven by the
   `PaymentOptions.PaymentSucceeded` flag. No decision on a real provider.
4. **`HybridApp`** is in `eShop.slnx` but in no solution filter and no CI
   workflow. Keep and wire up, or remove.
5. **NuGet vulnerability warnings** are globally suppressed by
   `NoWarn=NU1901;NU1902;NU1903;NU1904` in `Directory.Build.props`. These should
   be re-enabled and the findings triaged before a production deployment.
6. **Environment strategy.** Single environment, or separate dev/staging/prod
   Container Apps environments.
7. **Cost ceiling.** Not yet established. Drives tier selection across ADR-002,
   ADR-003, ADR-004 and ADR-005.

---

## 7. Suggested implementation order

Sequenced so that each step is independently verifiable.

| Step | Work | Why here |
|---|---|---|
| 1 | Deploy as-is with `aspire deploy` | Validates credentials, ACR, networking and the identity callback chain before anything is rewritten |
| 2 | ADR-011, ADR-012 | Cheap, self-contained, and they remove the two silent multi-replica failures |
| 3 | ADR-009 | Observability must exist before changing anything substantial |
| 4 | ADR-002, ADR-003 | Managed data stores; mostly configuration |
| 5 | ADR-013, ADR-008 | Identity and secret plumbing once the resources exist |
| 6 | ADR-006 | Migrations out of startup; needs the managed database in place |
| 7 | ADR-004 | The largest change — a new `IEventBus` implementation |
| 8 | ADR-007 | Depends on ADR-004 |
| 9 | ADR-005, ADR-010 | Scale-out and the custom domain, once the system is stable |

Step 1 is deliberately first: the upstream README is explicit that the default
deployment uses disposable data stores, which is acceptable for a throwaway
validation run and surfaces the Container Apps-specific problems — forwarded
headers, issuer mismatch, ingress behaviour — while the topology is still
unmodified.
