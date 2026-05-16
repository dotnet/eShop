# Catalog.API

eShop Catalog Microservice — HTTP API providing catalog item browsing, management, and AI-powered semantic search with integration event publishing.

## Summary

- Exposes REST API endpoints for querying, creating, updating, and deleting catalog items with support for pagination, filtering by brand/type, and semantic search using embeddings.
- Receives integration events from Ordering service (order status changes) and responds with stock validation or stock confirmation events.
- Uses PostgreSQL with pgvector extension for AI embeddings; publishes events through RabbitMQ EventBus.
- Supports optional AI embeddings via OpenAI or Ollama for semantic relevance search.
- Implements outbox pattern for guaranteed event publishing with integration event log.

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|-------------|
| `src/Catalog.API` | Main Catalog API service | `Program.cs`, `Apis/CatalogApi.cs`, `Extensions/Extensions.cs` |
| `src/Catalog.API/Model` | Domain entities and DTOs | `CatalogItem.cs`, `CatalogBrand.cs`, `CatalogType.cs`, `PaginationRequest.cs` |
| `src/Catalog.API/Infrastructure` | Data access context and migrations | `CatalogContext.cs`, `CatalogContextSeed.cs`, `EntityConfigurations/` |
| `src/Catalog.API/Services` | Business logic services | `CatalogAI.cs`, `ICatalogAI.cs` |
| `src/Catalog.API/Apis` | HTTP route definitions | `CatalogApi.cs` |
| `src/Catalog.API/IntegrationEvents` | Event definitions and handlers | `Events/`, `EventHandling/`, `CatalogIntegrationEventService.cs` |

## Components

---

COMPONENT_NAME: CatalogApi
TYPE: API
PURPOSE: Exposes HTTP REST endpoints for querying catalog items, managing items, and performing AI-powered semantic search.
RESPONSIBILITIES:
  - Maps HTTP routes for paginated item retrieval with optional filtering by name, brand, type
  - Handles semantic relevance search using AI embeddings with fallback to name-based search
  - Exposes endpoints for batch item retrieval by IDs and individual item information
  - Provides catalog types and brands listing
  - Handles item creation, update, and deletion with validation
  - Serves item picture files with appropriate MIME types and caching headers
SOURCE: `src/Catalog.API/Apis/CatalogApi.cs`
CALLS:
  - CatalogContext — to retrieve and modify catalog items, brands, types
  - CatalogAI — to generate embeddings for semantic search and item creation/updates
  - CatalogIntegrationEventService — to publish ProductPriceChangedIntegrationEvent when item price is modified
CALLED_BY:
  - HTTP clients (external)

---

COMPONENT_NAME: CatalogContext
TYPE: Repository
PURPOSE: Abstracts data access to PostgreSQL database for catalog items, brands, types, and integration event logs.
RESPONSIBILITIES:
  - Defines DbSet properties for CatalogItems, CatalogBrands, CatalogTypes
  - Configures EF Core model mappings including pgvector extension for embeddings
  - Includes IntegrationEventLog table via UseIntegrationEventLogs()
  - Manages database transactions for atomicity between catalog and event log changes
SOURCE: `src/Catalog.API/Infrastructure/CatalogContext.cs`
CALLS:
  - NONE (data access abstraction)
CALLED_BY:
  - CatalogApi
  - CatalogIntegrationEventService
  - OrderStatusChangedToAwaitingValidationIntegrationEventHandler
  - OrderStatusChangedToPaidIntegrationEventHandler
  - CatalogContextSeed

---

COMPONENT_NAME: CatalogAI
TYPE: Service
PURPOSE: Generates vector embeddings for catalog items and search queries to enable semantic relevance search.
RESPONSIBILITIES:
  - Conditionally generates embeddings via OpenAI or Ollama embedding generators
  - Converts catalog items to text representation (name + description) for embedding
  - Processes single or batch embedding requests with 384-dimensional vectors
  - Provides IsEnabled flag to check if embedding generator is configured
  - Logs embedding generation timing and results at trace level
SOURCE: `src/Catalog.API/Services/CatalogAI.cs`
CALLS:
  - NONE (delegates to injected IEmbeddingGenerator)
CALLED_BY:
  - CatalogApi
  - CatalogContextSeed

---

COMPONENT_NAME: CatalogIntegrationEventService
TYPE: Service
PURPOSE: Manages publishing of integration events through EventBus with transactional guarantees via IntegrationEventLog.
RESPONSIBILITIES:
  - Saves integration events to IntegrationEventLog and applies DbContext changes atomically
  - Publishes integration events through RabbitMQ EventBus
  - Tracks event state transitions: In Progress → Published or Failed
  - Logs event publishing operations at information and error levels
  - Implements disposal pattern for cleanup
SOURCE: `src/Catalog.API/IntegrationEvents/CatalogIntegrationEventService.cs`
CALLS:
  - IntegrationEventLogService — to mark events as in progress, published, or failed
  - EventBus — to publish the integration event to message queue
  - CatalogContext — to save context changes within resilient transaction
CALLED_BY:
  - CatalogApi
  - OrderStatusChangedToAwaitingValidationIntegrationEventHandler

---

COMPONENT_NAME: OrderStatusChangedToAwaitingValidationIntegrationEventHandler
TYPE: Consumer
PURPOSE: Consumes order validation events and validates stock availability for ordered items.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToAwaitingValidationIntegrationEvent from RabbitMQ
  - Queries CatalogContext for each item in the order to verify available stock
  - Determines if items are in stock and creates ConfirmedOrderStockItem records
  - Publishes OrderStockConfirmedIntegrationEvent if all items have stock
  - Publishes OrderStockRejectedIntegrationEvent if any item lacks sufficient stock
  - Logs handling of integration events
SOURCE: `src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs`
CALLS:
  - CatalogContext — to find and check available stock for catalog items
  - CatalogIntegrationEventService — to save event and context changes atomically, then publish resulting event
CALLED_BY:
  - EventBus (RabbitMQ subscription)

---

COMPONENT_NAME: OrderStatusChangedToPaidIntegrationEventHandler
TYPE: Consumer
PURPOSE: Consumes order paid events and decrements inventory stock for ordered items.
RESPONSIBILITIES:
  - Receives OrderStatusChangedToPaidIntegrationEvent from RabbitMQ
  - Iterates through order items and removes stock from corresponding catalog items
  - Logs handling of integration events
  - Persists inventory changes to database
SOURCE: `src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToPaidIntegrationEventHandler.cs`
CALLS:
  - CatalogContext — to find catalog items and save updated stock quantities via RemoveStock() method
CALLED_BY:
  - EventBus (RabbitMQ subscription)

---

COMPONENT_NAME: CatalogContextSeed
TYPE: Utility
PURPOSE: Seeds initial catalog data from JSON file into PostgreSQL database on application startup.
RESPONSIBILITIES:
  - Loads catalog items, brands, and types from `Setup/catalog.json`
  - Creates CatalogBrand and CatalogType entities from seed data
  - Generates embeddings for all catalog items using CatalogAI service
  - Manages pgvector extension initialization and type reloading for Npgsql
  - Implements IDbSeeder<CatalogContext> interface for integration with service defaults
SOURCE: `src/Catalog.API/Infrastructure/CatalogContextSeed.cs`
CALLS:
  - CatalogContext — to add and save entities
  - CatalogAI — to generate embeddings for seeded items
CALLED_BY:
  - Service startup (via IDbSeeder registration in Extensions.cs)

---

COMPONENT_NAME: CatalogServices
TYPE: Service
PURPOSE: Aggregates dependencies injected into HTTP endpoints via constructor injection.
RESPONSIBILITIES:
  - Provides unified access to CatalogContext, CatalogAI, Options, Logger, and CatalogIntegrationEventService
  - Used as [AsParameters] injectable service in endpoint handlers
  - Contains all transitive dependencies needed by endpoint implementations
SOURCE: `src/Catalog.API/Model/CatalogServices.cs`
CALLS:
  - NONE (dependency aggregator)
CALLED_BY:
  - CatalogApi (endpoint methods)

---

COMPONENT_NAME: IntegrationEventLogService
TYPE: Service
PURPOSE: Persists integration events to outbox table for guaranteed delivery via transactional outbox pattern.
RESPONSIBILITIES:
  - Saves integration events to IntegrationEventLog table
  - Marks events as "In Progress", "Published", or "Failed"
  - Operates within client-provided database transactions for atomicity
  - Provides disposal mechanism
SOURCE: (external, from eShop.IntegrationEventLogEF package)
CALLS:
  - NONE (data persistence abstraction)
CALLED_BY:
  - CatalogIntegrationEventService

---

COMPONENT_NAME: EventBus
TYPE: Gateway
PURPOSE: RabbitMQ message broker gateway for publishing and subscribing to integration events.
RESPONSIBILITIES:
  - Publishes integration events to RabbitMQ exchange
  - Routes published events to subscribed handlers via message queue
  - Manages message serialization and deserialization
SOURCE: (external, from eShop.EventBusRabbitMQ package)
CALLS:
  - NONE (message bus abstraction)
CALLED_BY:
  - CatalogIntegrationEventService (publisher)
  - Framework (invokes handlers as subscriber)

## Component Call Sequences

### Use-Case: Browse Catalog Items with Semantic Search

STEP 1: CatalogApi → CatalogContext
  OPERATION: CatalogItems.Where(...).OrderBy(...).Skip(...).Take(...)
  PURPOSE: Retrieve paginated list of all catalog items, optionally filtered by name, brand, or type
  SOURCE: `src/Catalog.API/Apis/CatalogApi.cs:GetAllItems()`

STEP 2: CatalogApi → CatalogAI
  OPERATION: GetEmbeddingAsync(text)
  PURPOSE: Generate vector embedding for the search text to enable semantic similarity comparison
  SOURCE: `src/Catalog.API/Apis/CatalogApi.cs:GetItemsBySemanticRelevance()`

STEP 3: CatalogApi → CatalogContext
  OPERATION: CatalogItems.Where(c => c.Embedding != null).OrderBy(c => c.Embedding.CosineDistance(vector)).Skip(...).Take(...)
  PURPOSE: Retrieve catalog items ordered by cosine distance (similarity) to the search embedding, enabling semantic relevance ranking
  SOURCE: `src/Catalog.API/Apis/CatalogApi.cs:GetItemsBySemanticRelevance()`

---

### Use-Case: Validate and Fulfill Order Stock

STEP 1: EventBus → OrderStatusChangedToAwaitingValidationIntegrationEventHandler
  OPERATION: Handle(OrderStatusChangedToAwaitingValidationIntegrationEvent)
  PURPOSE: Consume integration event from Ordering service indicating order awaits validation
  SOURCE: (RabbitMQ subscription configured in Extensions.cs)

STEP 2: OrderStatusChangedToAwaitingValidationIntegrationEventHandler → CatalogContext
  OPERATION: CatalogItems.Find(orderStockItem.ProductId) [repeated for each item]
  PURPOSE: Retrieve each catalog item from the order to check available stock
  SOURCE: `src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs`

STEP 3: OrderStatusChangedToAwaitingValidationIntegrationEventHandler → CatalogIntegrationEventService
  OPERATION: SaveEventAndCatalogContextChangesAsync(OrderStockConfirmedIntegrationEvent or OrderStockRejectedIntegrationEvent)
  PURPOSE: Atomically save the stock validation result event to the outbox table and commit within a database transaction
  SOURCE: `src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs`

STEP 4: CatalogIntegrationEventService → CatalogContext
  OPERATION: SaveChangesAsync() within ResilientTransaction
  PURPOSE: Persist catalog state changes and integration event log entry in a single transaction
  SOURCE: `src/Catalog.API/IntegrationEvents/CatalogIntegrationEventService.cs:SaveEventAndCatalogContextChangesAsync()`

STEP 5: CatalogIntegrationEventService → IntegrationEventLogService
  OPERATION: SaveEventAsync(evt, transaction)
  PURPOSE: Record the integration event in the outbox table with "Pending" state
  SOURCE: `src/Catalog.API/IntegrationEvents/CatalogIntegrationEventService.cs:SaveEventAndCatalogContextChangesAsync()`

STEP 6: CatalogIntegrationEventService → EventBus
  OPERATION: PublishAsync(event)
  PURPOSE: Publish the stock confirmation or rejection event to RabbitMQ for consumption by Ordering service
  SOURCE: `src/Catalog.API/IntegrationEvents/CatalogIntegrationEventService.cs:PublishThroughEventBusAsync()`

STEP 7: CatalogIntegrationEventService → IntegrationEventLogService
  OPERATION: MarkEventAsPublishedAsync(event.Id)
  PURPOSE: Update the outbox entry to "Published" state after successful message broker acknowledgment
  SOURCE: `src/Catalog.API/IntegrationEvents/CatalogIntegrationEventService.cs:PublishThroughEventBusAsync()`

---

## Communication Channels

### HTTP REST API

CHANNEL_TYPE: HTTP
ENDPOINT: /api/catalog
SOURCE: `src/Catalog.API/Apis/CatalogApi.cs`
NOTES:
  - API version 1.0 and 2.0 supported via Asp.Versioning middleware
  - Semantic search endpoint available in both versions with different signatures (V1: route parameter; V2: query parameter)
  - All endpoints support pagination via PageSize and PageIndex query parameters
  - Item pictures served as binary files with conditional GET (304 Not Modified) support
  - Problem Details responses on validation errors (400 Bad Request)

Endpoints:

- GET `/api/catalog/items?pageSize=10&pageIndex=0&name=...&type=...&brand=...` — List paginated catalog items with optional filters
- GET `/api/catalog/items/by?ids=1&ids=2&ids=3` — Batch retrieve multiple items by ID
- GET `/api/catalog/items/{id}` — Get single item by ID
- GET `/api/catalog/items/by/{name}` — List items matching name prefix (V1 only)
- GET `/api/catalog/items/withsemanticrelevance/{text}` — Search items by semantic relevance (V1) or `/items/withsemanticrelevance?text=...` (V2)
- GET `/api/catalog/items/type/{typeId}/brand/{brandId}` — List items filtered by type and optional brand
- GET `/api/catalog/items/type/all/brand/{brandId}` — List items filtered by brand across all types
- GET `/api/catalog/items/{id}/pic` — Retrieve item picture file
- GET `/api/catalog/catalogtypes` — List all catalog types
- GET `/api/catalog/catalogbrands` — List all catalog brands
- PUT `/api/catalog/items` — Create or replace item (V1) or `/items/{id}` (V2)
- POST `/api/catalog/items` — Create new catalog item
- DELETE `/api/catalog/items/{id}` — Delete catalog item by ID

---

### RabbitMQ EventBus

CHANNEL_TYPE: MessageQueue
ENDPOINT: amqp://localhost (configurable)
SOURCE: `src/Catalog.API/Extensions/Extensions.cs`, `src/Catalog.API/appsettings.json`
NOTES:
  - Event exchange name: "eventbus" (configured via AddRabbitMqEventBus("eventbus"))
  - Subscription client name: "Catalog" (SubscriptionClientName in appsettings)

Subscriptions:

- **Subscribe**: `OrderStatusChangedToAwaitingValidationIntegrationEvent` → `OrderStatusChangedToAwaitingValidationIntegrationEventHandler`
  - Triggered when order status changes to awaiting validation in Ordering service
  - Handler validates stock and publishes OrderStockConfirmedIntegrationEvent or OrderStockRejectedIntegrationEvent

- **Subscribe**: `OrderStatusChangedToPaidIntegrationEvent` → `OrderStatusChangedToPaidIntegrationEventHandler`
  - Triggered when order payment is confirmed in Ordering service
  - Handler decrements inventory stock for ordered items

Publications:

- **Publish**: `OrderStockConfirmedIntegrationEvent` — Order has sufficient stock; sent to Ordering service
- **Publish**: `OrderStockRejectedIntegrationEvent` — Some items lack stock; sent to Ordering service
- **Publish**: `ProductPriceChangedIntegrationEvent` — Catalog item price updated; sent to all subscribers

---

### PostgreSQL Database

CHANNEL_TYPE: Relational Database
ENDPOINT: catalogdb (connection string via Aspire service discovery)
SOURCE: `src/Catalog.API/Extensions/Extensions.cs` (Npgsql configuration)
NOTES:
  - Uses pgvector extension for 384-dimensional embedding vectors
  - EF Core ORM with migrations stored in `Infrastructure/Migrations/`
  - Connection resiliency enabled
  - Isolation level: default (Read Committed)

Database schema:

- **Catalog** table (maps to CatalogItem entity)
  - Columns: Id, Name (max 50), Description, Price, PictureFileName, CatalogTypeId, CatalogBrandId, AvailableStock, RestockThreshold, MaxStockThreshold, Embedding (vector(384)), OnReorder
  - Index on Name column
  - Foreign keys to CatalogType and CatalogBrand

- **CatalogBrand** table
  - Columns: Id, Brand (required, 255)

- **CatalogType** table
  - Columns: Id, Type (required, 255)

- **IntegrationEventLog** table (created and managed by IntegrationEventLogEF package)
  - Outbox pattern implementation for guaranteed event delivery
  - Columns: EventId, EventTypeName, EventTypeFullName, Content, CreationTime, State, TimesSent, TransactionId

---

## Dependency Registration and Wiring

DI_CONTAINER: Built-in ASP.NET Core IServiceCollection
REGISTRATION_FILE: `src/Catalog.API/Extensions/Extensions.cs` — `AddApplicationServices()` method

All service lifetimes use the default ASP.NET Core scoping:

| Abstraction | Implementation | Lifetime | Scope | Notes |
|-------------|----------------|----------|-------|-------|
| `IDbContext: CatalogContext` | `CatalogContext` | Scoped | Per HTTP request | Registered via AddNpgsqlDbContext<CatalogContext>("catalogdb") with pgvector support |
| `IIntegrationEventLogService` | `IntegrationEventLogService<CatalogContext>` | Transient | Single use | Registers event logging service for outbox pattern |
| `ICatalogIntegrationEventService` | `CatalogIntegrationEventService` | Transient | Single use | Event publishing service with transaction support |
| `ICatalogAI` | `CatalogAI` | Scoped | Per HTTP request | AI embedding service; conditionally registered based on Ollama or OpenAI config |
| `IEmbeddingGenerator<string, Embedding<float>>` | OpenAI or Ollama provider | Singleton | Application lifetime | Optional; only registered if OllamaEnabled or textEmbeddingModel connection string exists |
| `IEventBus` | RabbitMQ provider | Singleton | Application lifetime | Registered via AddRabbitMqEventBus("eventbus") and subscriptions |
| `IDbSeeder<CatalogContext>` | `CatalogContextSeed` | (registered via AddMigration call) | Startup only | Database seeding on application startup |

Example registration snippets:

```csharp
builder.AddNpgsqlDbContext<CatalogContext>("catalogdb", configureDbContextOptions: dbContextOptionsBuilder =>
{
    dbContextOptionsBuilder.UseNpgsql(builder =>
    {
        builder.UseVector();
    });
});

builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();
builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

builder.AddRabbitMqEventBus("eventbus")
       .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
       .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();

builder.Services.AddScoped<ICatalogAI, CatalogAI>();
```

---

## Configuration and Secrets

### Configuration Sources

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|-------------|------|-----------|----------|
| config file | Logging, OpenApi, ConnectionStrings, EventBus, CatalogOptions | NO | `src/Catalog.API/appsettings.json` |
| config file | Logging (overrides) | NO | `src/Catalog.API/appsettings.Development.json` |
| environment variable / Aspire | catalogdb (connection string) | YES | Aspire service discovery |
| environment variable / Aspire | eventbus (RabbitMQ connection string) | YES | Aspire service discovery (amqp://localhost) |
| environment variable / Aspire | textEmbeddingModel (OpenAI connection string) | YES | Aspire service discovery (optional) |
| environment variable / Aspire | embedding (Ollama connection string) | YES | Aspire service discovery (optional) |
| environment variable | OllamaEnabled (string "true"/"false") | NO | Application configuration |
| secrets (user-secrets) | (not used in code, available via user-secrets for local development) | YES | User secrets store (UserSecretsId: d1b521ec-3411-4d39-98c6-8509466ed471) |

### Configuration Details

**Logging:**
- Default level: Information
- Microsoft.AspNetCore level: Warning

**OpenApi:**
- Endpoint name: "Catalog.API V1"
- Document title: "eShop - Catalog HTTP API"
- Document version: "v1"
- Description: "The Catalog Microservice HTTP API. This is a Data-Driven/CRUD microservice sample"

**ConnectionStrings:**
- `catalogdb`: PostgreSQL connection (Aspire service reference)
- `EventBus`: RabbitMQ connection URI (default: amqp://localhost)

**EventBus:**
- SubscriptionClientName: "Catalog"

**CatalogOptions:**
- UseCustomizationData: false (controls whether customized item data is loaded during seeding)
- PicBaseUrl: null (optional base URL for picture retrieval)

**AI Configuration:**
- OllamaEnabled: string boolean; if "true", uses Ollama for embeddings
- textEmbeddingModel: Optional OpenAI embedding model connection string

---

## Persistence and Data Access

DATABASE: PostgreSQL 14+ with pgvector extension
DATA_ACCESS: Entity Framework Core 10.0 with Npgsql provider and pgvector mapping
MIGRATIONS_PATH: `src/Catalog.API/Infrastructure/Migrations/`

REPOSITORY_PATTERN: YES

Repositories (implicit via DbContext):

- **CatalogItemRepository** (implicit)
  - IMPLEMENTATION: `CatalogContext.CatalogItems` DbSet
  - OPERATIONS: Query by Id, Name, Brand, Type, Pagination; Create; Update embeddings; Delete; RemoveStock; AddStock
  - ACCESSED_BY: CatalogApi, event handlers

- **CatalogBrandRepository** (implicit)
  - IMPLEMENTATION: `CatalogContext.CatalogBrands` DbSet
  - OPERATIONS: Query all brands
  - ACCESSED_BY: CatalogApi

- **CatalogTypeRepository** (implicit)
  - IMPLEMENTATION: `CatalogContext.CatalogTypes` DbSet
  - OPERATIONS: Query all types
  - ACCESSED_BY: CatalogApi

- **IntegrationEventLogRepository** (implicit)
  - IMPLEMENTATION: Provided by IntegrationEventLogEF package
  - OPERATIONS: SaveEventAsync, MarkEventAsInProgressAsync, MarkEventAsPublishedAsync, MarkEventAsFailedAsync
  - ACCESSED_BY: CatalogIntegrationEventService

Database entity configuration:

```csharp
// CatalogItem mapping via CatalogItemEntityTypeConfiguration
builder.ToTable("Catalog");
builder.Property(ci => ci.Name).HasMaxLength(50);
builder.Property(ci => ci.Embedding).HasColumnType("vector(384)");
builder.HasOne(ci => ci.CatalogBrand).WithMany();
builder.HasOne(ci => ci.CatalogType).WithMany();
builder.HasIndex(ci => ci.Name);
```

Migrations:

1. `20231009153249_Initial.cs` — Initial schema with CatalogItem, CatalogBrand, CatalogType tables
2. `20231018163051_RemoveHiLoAndIndexCatalogName.cs` — Removed identity Hi-Lo pattern, added Name index
3. `20231026091140_Outbox.cs` — Added IntegrationEventLog table for outbox pattern

---

## Patterns and Architecture Notes

### Pattern: Repository Pattern (Implicit)

PATTERN: Repository
EVIDENCE: `CatalogContext` class in `src/Catalog.API/Infrastructure/CatalogContext.cs`
SNIPPET:
```csharp
public class CatalogContext : DbContext
{
    public required DbSet<CatalogItem> CatalogItems { get; set; }
    public required DbSet<CatalogBrand> CatalogBrands { get; set; }
    public required DbSet<CatalogType> CatalogTypes { get; set; }
}
```

---

### Pattern: Transactional Outbox

PATTERN: Outbox Pattern
EVIDENCE: Dual publish mechanism in `src/Catalog.API/IntegrationEvents/CatalogIntegrationEventService.cs` and `OrderStatusChangedToAwaitingValidationIntegrationEventHandler.cs`
SNIPPET:
```csharp
await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
{
    await catalogContext.SaveChangesAsync();
    await integrationEventLogService.SaveEventAsync(evt, catalogContext.Database.CurrentTransaction);
});
await eventBus.PublishAsync(evt);
await integrationEventLogService.MarkEventAsPublishedAsync(evt.Id);
```

---

### Pattern: Event-Driven Architecture

PATTERN: Event-Driven with Integration Events
EVIDENCE: 
- Event subscription in `src/Catalog.API/Extensions/Extensions.cs`: `.AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()`
- Event publishing in `src/Catalog.API/Apis/CatalogApi.cs:UpdateItem()`: `await services.EventService.PublishThroughEventBusAsync(priceChangedEvent);`

---

### Pattern: Semantic Search with Vector Embeddings

PATTERN: AI/ML Integration — Vector Similarity Search
EVIDENCE: `src/Catalog.API/Apis/CatalogApi.cs:GetItemsBySemanticRelevance()` and `src/Catalog.API/Services/CatalogAI.cs`
SNIPPET:
```csharp
var vector = await services.CatalogAI.GetEmbeddingAsync(text);
itemsOnPage = await services.Context.CatalogItems
    .Where(c => c.Embedding != null)
    .OrderBy(c => c.Embedding!.CosineDistance(vector))
    .Skip(pageSize * pageIndex)
    .Take(pageSize)
    .ToListAsync();
```

---

### Pattern: Minimal APIs

PATTERN: Minimal APIs (ASP.NET Core)
EVIDENCE: `src/Catalog.API/Apis/CatalogApi.cs` uses static methods and MapGet/MapPost/MapPut/MapDelete for route mapping
SNIPPET:
```csharp
v2.MapGet("/items", GetAllItems)
    .WithName("ListItems-V2")
    .WithSummary("List catalog items");
api.MapPost("/items", CreateItem)
    .WithName("CreateItem");
```

---

### Pattern: API Versioning

PATTERN: API Versioning with Asp.Versioning middleware
EVIDENCE: `src/Catalog.API/Program.cs` and `src/Catalog.API/Apis/CatalogApi.cs`
SNIPPET:
```csharp
var withApiVersioning = builder.Services.AddApiVersioning();
var vApi = app.NewVersionedApi("Catalog");
var v1 = vApi.MapGroup("api/catalog").HasApiVersion(1, 0);
var v2 = vApi.MapGroup("api/catalog").HasApiVersion(2, 0);
```

---

### Pattern: Domain Events in Aggregate

PATTERN: Domain-driven design with aggregate root methods
EVIDENCE: `src/Catalog.API/Model/CatalogItem.cs` contains business logic in RemoveStock() and AddStock() methods
SNIPPET:
```csharp
public int RemoveStock(int quantityDesired)
{
    if (AvailableStock == 0)
        throw new CatalogDomainException($"Empty stock, product item {Name} is sold out");
    
    int removed = Math.Min(quantityDesired, this.AvailableStock);
    this.AvailableStock -= removed;
    return removed;
}
```

---

## Security and Operational Considerations

AUTHN_AUTHZ: None (API is public for browsing; protected by API Gateway / Nginx reverse proxy in production)
MECHANISM: No built-in authentication; relies on infrastructure-level access control
CONFIGURATION: Not applicable; set at deployment boundary via reverse proxy or service mesh

---

### KNOWN_RISKS

- **No input validation on CatalogItem creation/update**: POST /api/catalog/items accepts arbitrary CatalogItem properties; should validate CatalogTypeId and CatalogBrandId foreign keys exist.
- **No authorization checks**: All endpoints are public; no role-based or policy-based access control implemented.
- **No rate limiting**: No rate limiting or throttling implemented; exposed to potential DoS attacks.
- **Vector embedding dependency optional but unvalidated**: If OllamaEnabled or textEmbeddingModel is misconfigured, embeddings will silently fail and queries will fall back to name-based search without alerting the caller.
- **Database connection string in appsettings.json**: For development only; production should use Aspire service discovery or environment variables.
- **No field-level encryption**: Sensitive data (prices, descriptions) stored in plaintext in PostgreSQL.
- **Picture file access via path traversal**: GetItemPictureById constructs file paths from database records; potential for symlink traversal if database is compromised.

---

### OBSERVABILITY

**Logging:**
- Framework: Microsoft.Extensions.Logging (built-in)
- Integration points:
  - `CatalogApi`: No explicit logging (uses default HTTP middleware logging)
  - `CatalogIntegrationEventService`: LogInformation for event publishing, LogError on exceptions
  - `OrderStatusChangedToAwaitingValidationIntegrationEventHandler`: LogInformation for event handling
  - `OrderStatusChangedToPaidIntegrationEventHandler`: LogInformation for event handling
  - `CatalogAI`: LogTrace for embedding generation timing and results (debug/trace level only)

**Metrics:**
- None implemented in application code; relies on framework defaults (request count, response time, status codes)

**Health Checks:**
- Default health check endpoints mapped via `app.MapDefaultEndpoints()` in Program.cs (provides /health, /alive endpoints)

**Tracing:**
- Activity tracing configured in `GlobalUsings.cs` imports (ActivityExtensions via Shared/)
- HTTP requests and responses traced at framework level

**Configuration sources for observability:**
- Logging.LogLevel configuration in appsettings.json
- Trace level set in appsettings.Development.json (default "Information" in production)

---

### DEPLOYMENT

**Containerization:**
- Dockerfile: Not present in src/Catalog.API; built by CI/CD pipeline
- Default .NET runtime: net10.0

**Orchestration:**
- Infrastructure-as-Code: None in repository
- Docker Compose: Likely in root-level docker-compose.yml (not analyzed in this scope)
- Kubernetes: No Helm charts or K8s manifests in src/Catalog.API directory

**Build and Package References:**
- NuGet packages (see Catalog.API.csproj):
  - Asp.Versioning.Http (API versioning)
  - Aspire.Npgsql.EntityFrameworkCore.PostgreSQL (EF Core + PostgreSQL with Aspire integration)
  - Aspire.Azure.AI.OpenAI (OpenAI embeddings)
  - CommunityToolkit.Aspire.OllamaSharp (Ollama embeddings)
  - Pgvector and Pgvector.EntityFrameworkCore (vector search)
  - EventBusRabbitMQ (RabbitMQ integration events)
  - IntegrationEventLogEF (outbox pattern)
  - eShop.ServiceDefaults (shared defaults across services)

**Data Migration:**
- EF Core migrations support: `dotnet ef migrations add --context CatalogContext [migration-name]`
- Automatic migration execution on startup (via AddMigration in Extensions.cs)

---
