# Basket.API

gRPC microservice providing shopping basket management with Redis persistence and RabbitMQ event-driven architecture.

## Summary

- Exposes gRPC service for authenticated users to retrieve, update, and delete shopping baskets
- Persists basket state to Redis with UTF8-optimized key prefixes
- Subscribes to OrderStarted integration events and clears baskets upon order initiation
- Implements JWT Bearer authentication with user identity extraction from claims
- Uses RabbitMQ as the message broker for cross-service communication

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|-------------|
| `src/Basket.API` | gRPC microservice for basket management | `Program.cs` |

## Components

COMPONENT_NAME: BasketService
TYPE: API
PURPOSE: Exposes gRPC methods for basket operations (retrieve, update, delete).
RESPONSIBILITIES:
  - Accepts gRPC requests and validates user authentication via JWT claims
  - Maps gRPC request/response messages to domain models
  - Delegates business operations to IBasketRepository
  - Extracts user identity from ServerCallContext and enforces authentication
  - Logs method invocations at Debug level for observability
SOURCE: `src/Basket.API/Grpc/BasketService.cs`
CALLS:
  - IBasketRepository — to retrieve, update, and delete basket data
  - ServerCallContextIdentityExtensions — to extract user identity claim
  - ILogger — to record operation details
CALLED_BY:
  - gRPC clients (external)

---

COMPONENT_NAME: IBasketRepository
TYPE: Repository
PURPOSE: Abstracts data access for Customer Basket entities.
RESPONSIBILITIES:
  - Define contract for basket retrieval by customer ID
  - Define contract for basket updates with full replacement semantics
  - Define contract for basket deletion
SOURCE: `src/Basket.API/Repositories/IBasketRepository.cs`
CALLS:
  - NONE
CALLED_BY:
  - BasketService
  - OrderStartedIntegrationEventHandler

---

COMPONENT_NAME: RedisBasketRepository
TYPE: Repository
PURPOSE: Implements basket persistence using Redis as the backing store.
RESPONSIBILITIES:
  - Serialize CustomerBasket to UTF8 JSON and store in Redis with `/basket/{id}` key prefix
  - Deserialize Redis strings back to CustomerBasket domain objects
  - Use lease-based reads (StringGetLeaseAsync) for efficient memory management
  - Log persistence outcomes (success, failure)
  - Return null on key miss or deserialization failure
SOURCE: `src/Basket.API/Repositories/RedisBasketRepository.cs`
CALLS:
  - IConnectionMultiplexer (StackExchange.Redis) — to execute Redis commands
  - ILogger — to record persistence events
CALLED_BY:
  - IBasketRepository (via dependency injection)

---

COMPONENT_NAME: BasketService.GetBasket
TYPE: API
PURPOSE: Retrieves the user's current basket state.
RESPONSIBILITIES:
  - Require authenticated user (UserIdentity claim "sub" present)
  - Return empty CustomerBasketResponse if no identity is present
  - Query repository and return mapped gRPC response
SOURCE: `src/Basket.API/Grpc/BasketService.cs` (lines 13–31)
CALLS:
  - IBasketRepository.GetBasketAsync()
CALLED_BY:
  - External gRPC clients

---

COMPONENT_NAME: BasketService.UpdateBasket
TYPE: API
PURPOSE: Updates the user's basket with new or modified items.
RESPONSIBILITIES:
  - Require authenticated user; throw RpcException(Unauthenticated) if missing
  - Map gRPC UpdateBasketRequest to CustomerBasket domain model
  - Delegate persistence to repository
  - Throw RpcException(NotFound) if repository returns null
SOURCE: `src/Basket.API/Grpc/BasketService.cs` (lines 33–55)
CALLS:
  - IBasketRepository.UpdateBasketAsync()
CALLED_BY:
  - External gRPC clients

---

COMPONENT_NAME: BasketService.DeleteBasket
TYPE: API
PURPOSE: Deletes the user's basket.
RESPONSIBILITIES:
  - Require authenticated user; throw RpcException(Unauthenticated) if missing
  - Delegate deletion to repository
  - Return empty DeleteBasketResponse on success
SOURCE: `src/Basket.API/Grpc/BasketService.cs` (lines 57–65)
CALLS:
  - IBasketRepository.DeleteBasketAsync()
CALLED_BY:
  - External gRPC clients

---

COMPONENT_NAME: OrderStartedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Handles OrderStarted integration events and clears the customer's basket upon order placement.
RESPONSIBILITIES:
  - Subscribe to OrderStartedIntegrationEvent messages from RabbitMQ
  - Extract UserId from event payload
  - Delete the associated basket from Redis
  - Log event handling entry point with correlation ID
SOURCE: `src/Basket.API/IntegrationEvents/EventHandling/OrderStartedIntegrationEventHandler.cs`
CALLS:
  - IBasketRepository.DeleteBasketAsync() — to remove basket after order placement
  - ILogger — to record event handling
CALLED_BY:
  - EventBus (RabbitMQ subscription dispatcher)

---

COMPONENT_NAME: OrderStartedIntegrationEvent
TYPE: Event
PURPOSE: Represents a cross-service event signaling that a customer has placed an order.
RESPONSIBILITIES:
  - Carry UserId of the customer who placed the order
  - Inherit correlation ID, creation timestamp from IntegrationEvent base
SOURCE: `src/Basket.API/IntegrationEvents/Events/OrderStartedIntegrationEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Ordering.API (publishes to RabbitMQ)

---

COMPONENT_NAME: CustomerBasket
TYPE: Store
PURPOSE: Represents the state of a shopping basket for a specific customer.
RESPONSIBILITIES:
  - Aggregate BuyerId and collection of BasketItems
  - Provide default constructor and customer-parameterized constructor
  - Initialize Items collection as empty list
SOURCE: `src/Basket.API/Model/CustomerBasket.cs`
CALLS:
  - NONE
CALLED_BY:
  - BasketService (mapping layer)
  - RedisBasketRepository (serialization/deserialization)

---

COMPONENT_NAME: BasketItem
TYPE: Store
PURPOSE: Represents a single item within a customer's basket.
RESPONSIBILITIES:
  - Aggregate product metadata (ID, name, price, picture URL)
  - Track quantity and historical price (OldUnitPrice for price tracking)
  - Validate that Quantity is at least 1
SOURCE: `src/Basket.API/Model/BasketItem.cs`
CALLS:
  - NONE
CALLED_BY:
  - BasketService (mapping layer)
  - CustomerBasket (composition)

---

COMPONENT_NAME: BasketSerializationContext
TYPE: Utility
PURPOSE: Provides compile-time JSON source-generated serialization/deserialization for CustomerBasket.
RESPONSIBILITIES:
  - Enable case-insensitive property matching during deserialization
  - Optimize UTF8 JSON encoding/decoding via reflection-free code generation
SOURCE: `src/Basket.API/Repositories/RedisBasketRepository.cs`
CALLS:
  - NONE
CALLED_BY:
  - JsonSerializer (compile-time wired)

---

COMPONENT_NAME: IntegrationEventContext
TYPE: Utility
PURPOSE: Provides compile-time JSON source-generated serialization for OrderStartedIntegrationEvent.
RESPONSIBILITIES:
  - Enable type-aware JSON serialization of integration events
SOURCE: `src/Basket.API/Extensions/Extensions.cs`
CALLS:
  - NONE
CALLED_BY:
  - EventBus (configured via ConfigureJsonOptions)

---

COMPONENT_NAME: ServerCallContextIdentityExtensions
TYPE: Utility
PURPOSE: Helper methods to extract JWT claims from gRPC ServerCallContext.
RESPONSIBILITIES:
  - Extract "sub" claim as user identity
  - Extract Name claim as user name
SOURCE: `src/Basket.API/Extensions/ServerCallContextIdentityExtensions.cs`
CALLS:
  - NONE
CALLED_BY:
  - BasketService

## Component Call Sequences

### Use-Case: Retrieve Basket

STEP 1: gRPC Client → BasketService.GetBasket
  OPERATION: GetBasket(GetBasketRequest)
  PURPOSE: Client initiates basket retrieval for authenticated user
  SOURCE: `src/Basket.API/Grpc/BasketService.cs`

STEP 2: BasketService → ServerCallContextIdentityExtensions.GetUserIdentity
  OPERATION: GetUserIdentity()
  PURPOSE: Extract user identity ("sub" claim) from JWT token in request context
  SOURCE: `src/Basket.API/Grpc/BasketService.cs` (line 15)

STEP 3: BasketService → IBasketRepository.GetBasketAsync
  OPERATION: GetBasketAsync(userId)
  PURPOSE: Query Redis for basket data keyed by user ID
  SOURCE: `src/Basket.API/Grpc/BasketService.cs` (line 24)

STEP 4: IBasketRepository → Redis Store
  OPERATION: StringGetLeaseAsync("/basket/{userId}")
  PURPOSE: Retrieve serialized basket from Redis with lease-based read
  SOURCE: `src/Basket.API/Repositories/RedisBasketRepository.cs` (line 25)

STEP 5: IBasketRepository → JsonSerializer
  OPERATION: Deserialize(RedisValue, BasketSerializationContext)
  PURPOSE: Convert stored JSON bytes back to CustomerBasket domain object
  SOURCE: `src/Basket.API/Repositories/RedisBasketRepository.cs` (line 30)

STEP 6: BasketService → gRPC Response
  OPERATION: MapToCustomerBasketResponse(CustomerBasket)
  PURPOSE: Transform domain model to gRPC message format for transmission
  SOURCE: `src/Basket.API/Grpc/BasketService.cs` (lines 70–79)

### Use-Case: Place Order (Basket Cleanup)

STEP 1: Ordering.API → RabbitMQ EventBus
  OPERATION: Publish(OrderStartedIntegrationEvent)
  PURPOSE: Notify downstream services that an order has been initiated
  SOURCE: External (Ordering.API)

STEP 2: RabbitMQ EventBus → OrderStartedIntegrationEventHandler
  OPERATION: Handle(OrderStartedIntegrationEvent)
  PURPOSE: Event subscription dispatcher invokes handler for this event type
  SOURCE: `src/Basket.API/Extensions/Extensions.cs` (line 20)

STEP 3: OrderStartedIntegrationEventHandler → IBasketRepository.DeleteBasketAsync
  OPERATION: DeleteBasketAsync(event.UserId)
  PURPOSE: Remove customer's basket from Redis following order placement
  SOURCE: `src/Basket.API/IntegrationEvents/EventHandling/OrderStartedIntegrationEventHandler.cs` (line 16)

STEP 4: IBasketRepository → Redis Store
  OPERATION: KeyDeleteAsync("/basket/{userId}")
  PURPOSE: Delete basket entry by key from Redis
  SOURCE: `src/Basket.API/Repositories/RedisBasketRepository.cs` (line 19)

## Communication Channels

CHANNEL_TYPE: gRPC
ENDPOINT: `localhost:5221` (default; configurable via launch settings)
SOURCE: `src/Basket.API/Properties/launchSettings.json`
NOTES: Kestrel protocol set to HTTP/2; service contract defined in `src/Basket.API/Proto/basket.proto`

---

CHANNEL_TYPE: MessageQueue
EXCHANGE: RabbitMQ (AMQP 0.9.1)
TOPIC: `OrderStartedIntegrationEvent`
SOURCE: `src/Basket.API/appsettings.json` (ConnectionStrings.EventBus)
NOTES: Broker URL `amqp://localhost`; subscription client name `Basket`; event routing configured in `Extensions.cs` (line 20)

## Dependency Registration and Wiring

DI_CONTAINER: Built-in .NET 10 dependency injection (Microsoft.Extensions.DependencyInjection)

REGISTRATION_FILE: `src/Basket.API/Extensions/Extensions.cs` (method `AddApplicationServices`)

### Registrations

**Scope: Singleton**
- Abstraction: `IBasketRepository`
- Implementation: `RedisBasketRepository`
- Snippet:
```csharp
builder.Services.AddSingleton<IBasketRepository, RedisBasketRepository>();
```
- Purpose: Single instance shared across all requests; stateless access to Redis

**Scope: Singleton (implicit via AddDefaultAuthentication)**
- Abstraction: JWT Bearer authentication handler
- Implementation: Aspire/ServiceDefaults-provided
- Purpose: Validate Bearer tokens and populate User.Claims

**Scope: Service Collection Configuration**
- Method: `builder.AddRedisClient("redis")`
- Registers: `IConnectionMultiplexer` (from Aspire.StackExchange.Redis)
- Snippet:
```csharp
builder.AddRedisClient("redis");
```
- Purpose: Provides Redis multiplexer for RedisBasketRepository

**Scope: Service Collection Configuration**
- Method: `builder.AddRabbitMqEventBus("eventbus")`
- Registers: EventBus subscriber and message broker client
- Snippet:
```csharp
builder.AddRabbitMqEventBus("eventbus")
       .AddSubscription<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();
```
- Purpose: Configure RabbitMQ subscription for OrderStartedIntegrationEvent

**Scope: Global JSON Serialization**
- Method: `.ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventContext.Default))`
- Purpose: Wire source-generated JSON context for integration event serialization

## Configuration and Secrets

SOURCE_TYPE: Configuration file (JSON)
KEYS:
  - `Logging:LogLevel:Default` — set to "Information"
  - `Logging:LogLevel:Microsoft.AspNetCore` — set to "Warning"
  - `Kestrel:EndpointDefaults:Protocols` — set to "Http2"
  - `ConnectionStrings:Redis` — Redis host and port (e.g., "localhost")
  - `ConnectionStrings:EventBus` — RabbitMQ AMQP URL (e.g., "amqp://localhost")
  - `Identity:Audience` — JWT audience claim validation ("basket")
  - `EventBus:SubscriptionClientName` — RabbitMQ subscriber group name ("Basket")
SENSITIVE: YES (ConnectionStrings.EventBus contains broker credentials if auth is enabled)
LOCATION: `src/Basket.API/appsettings.json`

---

SOURCE_TYPE: Environment variables (via Aspire service defaults)
KEYS:
  - `ASPNETCORE_ENVIRONMENT` — environment designation (Development, Staging, Production)
GROUP_PREFIX: `ConnectionString__*` (converted from config hierarchy)
SENSITIVE: YES
LOCATION: Process environment and Aspire service discovery

---

SOURCE_TYPE: appsettings environment override file
LOCATION: `src/Basket.API/appsettings.Development.json`
NOTES: Empty by default; development-specific overrides can be added here

## Persistence and Data Access

DATABASE: Redis (in-memory key-value store)
DATA_ACCESS: StackExchange.Redis (client library)
MIGRATIONS_PATH: NONE (Redis is schemaless; no migrations)
REPOSITORY_PATTERN: YES

### Repository Abstraction

Interface: `IBasketRepository`
Source: `src/Basket.API/Repositories/IBasketRepository.cs`

Methods:
- `GetBasketAsync(string customerId): Task<CustomerBasket>`
- `UpdateBasketAsync(CustomerBasket basket): Task<CustomerBasket>`
- `DeleteBasketAsync(string id): Task<bool>`

### Repository Implementation

Implementation: `RedisBasketRepository`
Source: `src/Basket.API/Repositories/RedisBasketRepository.cs`
Key Prefix Pattern: `/basket/{customerId}` (UTF8-encoded)
Serialization: UTF8 JSON via source-generated `BasketSerializationContext`

## Patterns and Architecture Notes

PATTERN: Repository Pattern
EVIDENCE: `IBasketRepository` interface and `RedisBasketRepository` implementation decouple data access from business logic
SNIPPET:
```csharp
public interface IBasketRepository
{
    Task<CustomerBasket> GetBasketAsync(string customerId);
    Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string id);
}
```

---

PATTERN: Dependency Injection (DI)
EVIDENCE: Constructor injection of `IBasketRepository` and `ILogger` in `BasketService`; service registration in `Extensions.cs`
SNIPPET:
```csharp
public class BasketService(
    IBasketRepository repository,
    ILogger<BasketService> logger) : Basket.BasketBase
```

---

PATTERN: Event-Driven Architecture
EVIDENCE: `OrderStartedIntegrationEventHandler` subscribes to cross-service events via RabbitMQ
SNIPPET:
```csharp
public class OrderStartedIntegrationEventHandler(
    IBasketRepository repository,
    ILogger<OrderStartedIntegrationEventHandler> logger) : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        await repository.DeleteBasketAsync(@event.UserId);
    }
}
```

---

PATTERN: Source-Generated JSON Serialization
EVIDENCE: `BasketSerializationContext` and `IntegrationEventContext` provide compile-time JSON serialization
SNIPPET:
```csharp
[JsonSerializable(typeof(CustomerBasket))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class BasketSerializationContext : JsonSerializerContext { }
```

## Security and Operational Considerations

AUTHN_AUTHZ: JWT Bearer token authentication
- Mechanism: JWT Bearer token validation via `AddDefaultAuthentication()` from Aspire service defaults
- Configuration file: `src/Basket.API/Extensions/Extensions.cs` (line 12)
- Token claims used: "sub" (user identity), "name" (user display name)
- Scope: Enforced on `UpdateBasket` and `DeleteBasket` (RPC status Unauthenticated thrown if missing); `GetBasket` allows anonymous access but returns empty if unauthenticated
- Issuer validation: Delegated to Aspire Identity configuration (environment-dependent)

---

KNOWN_RISKS:
- Hard-coded Redis key prefix `/basket/` is optimized for UTF8 but lacks namespace isolation; consider adding service instance identifier if multiple Basket.API instances share Redis
- `GetBasket` allows unauthenticated access and silently returns empty response instead of failing fast; may mask misconfigured clients
- Redis connection string in `appsettings.json` is plaintext; ensure Redis is only accessible on protected networks or use TLS/credentials in production
- No request validation on gRPC message field constraints (e.g., quantity bounds); validation occurs only on `BasketItem.Validate()` for domain objects, not protobuf boundaries
- Development appsettings override file is empty; no hot-reload of configuration changes

---

OBSERVABILITY:
- **Logging Framework**: ILogger (Microsoft.Extensions.Logging)
- **Log Levels**: Information (default), Warning (ASP.NET Core framework suppression)
- **Logged Events**:
  - `BasketService.GetBasket`: Debug-level method entry with context method and user ID
  - `BasketService.UpdateBasket`: Debug-level method entry with context method and user ID
  - `RedisBasketRepository.UpdateBasketAsync`: Information-level on success/failure
  - `OrderStartedIntegrationEventHandler.Handle`: Information-level on event handling with correlation ID
- **Health Checks**: Mapped via `app.MapDefaultEndpoints()` (Aspire service defaults); check endpoints at `/health` and `/health/live`
- **Metrics**: Implicit via gRPC framework (request count, latency, exceptions)
- No explicit metrics exporter configured (use Aspire observability tooling)

---

DEPLOYMENT:
- Dockerfile: Defined in build stage; standard multi-stage .NET 10 container
- Docker Compose: Basket.API wired into `docker-compose.yml` in repository root; exposes port 5221
- Kubernetes Manifests: NONE (deployment model uses Aspire App Host)
- Aspire App Host Configuration: Registered in `eShop.AppHost/Program.cs` (Basket.API service definition)
- Port: 5221 (gRPC endpoint)
- Protocol: HTTP/2 (Kestrel configuration via appsettings.json)
