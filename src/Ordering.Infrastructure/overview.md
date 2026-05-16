# Ordering.Infrastructure — Data Access and Domain Event Orchestration Layer

eShop Ordering Infrastructure provides data persistence, transaction management, and domain event dispatching for the Ordering bounded context using Entity Framework Core with PostgreSQL and MediatR.

## Summary

- Data access layer for Order and Buyer aggregates using Entity Framework Core with PostgreSQL database
- Implements Unit of Work pattern with atomic transaction management across domain event dispatch and database persistence
- Domain event extraction and publishing via MediatR for orchestrating order workflow (validation, payment, stock confirmation)
- Integration event outbox pattern (via IntegrationEventLogEF) for reliable distributed event publishing
- Request idempotency tracking to prevent duplicate command execution
- Repository abstraction for Order, Buyer, and payment-related data entities
- HiLo key generation strategy for distributed ID allocation

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `.` (root) | Infrastructure project configuration and main context | `Ordering.Infrastructure.csproj`, `OrderingContext.cs`, `MediatorExtension.cs` |
| `Repositories` | Data access layer abstractions and implementations | `OrderRepository.cs`, `BuyerRepository.cs` |
| `EntityConfigurations` | EF Core entity mappings and database schema configuration | `OrderEntityTypeConfiguration.cs`, `BuyerEntityTypeConfiguration.cs`, `PaymentMethodEntityTypeConfiguration.cs` |
| `Idempotency` | Request deduplication mechanism for command reliability | `RequestManager.cs`, `ClientRequest.cs`, `IRequestManager.cs` |
| `Migrations` | Database schema version control and incremental updates | `20230925222426_Initial.cs`, `20231026091055_Outbox.cs`, `20240106121712_UseEnumForOrderStatus.cs` |

## Components

---

COMPONENT_NAME: OrderingContext
TYPE: Store
PURPOSE: Manages database connections, Entity Framework Core DbContext, Unit of Work transactions, and domain event dispatching orchestration.
RESPONSIBILITIES:
  - Registers DbSet collections for Order, OrderItem, PaymentMethod, Buyer, and CardType entities
  - Implements IUnitOfWork interface for atomic transaction management
  - Coordinates domain event dispatch before SaveChanges to ensure single-transaction consistency
  - Manages explicit transactions with ReadCommitted isolation level
  - Provides current transaction state tracking and rollback handling
SOURCE: `OrderingContext.cs`
CALLS:
  - MediatorExtension.DispatchDomainEventsAsync() — dispatches domain events collected from change tracker
  - DbContext.SaveChangesAsync() — persists aggregates and value objects to PostgreSQL
  - Database.BeginTransactionAsync() — initiates explicit transaction
CALLED_BY:
  - OrderRepository
  - BuyerRepository
  - RequestManager
  - TransactionBehavior (MediatR pipeline)

---

COMPONENT_NAME: OrderRepository
TYPE: Repository
PURPOSE: Abstracts data access for Order aggregate with lazy-loading of OrderItems collection.
RESPONSIBILITIES:
  - Persists new Order aggregates to database via Add()
  - Retrieves Order entities with related OrderItems collection via GetAsync(int orderId)
  - Marks Order entities as modified for updates via Update()
  - Exposes UnitOfWork property to support IUnitOfWork transactional operations
SOURCE: `Repositories/OrderRepository.cs`
CALLS:
  - OrderingContext.Orders.Add() — adds new order aggregate
  - OrderingContext.Orders.FindAsync() — retrieves order by ID
  - OrderingContext.Entry().Collection().LoadAsync() — eager-loads OrderItems
  - OrderingContext.Entry().State — marks entity state for update
CALLED_BY:
  - CreateOrderCommandHandler (Ordering.API)
  - SetAwaitingValidationOrderStatusCommandHandler (Ordering.API)
  - SetPaidOrderStatusCommandHandler (Ordering.API)
  - SetShippedOrderStatusCommandHandler (Ordering.API)

---

COMPONENT_NAME: BuyerRepository
TYPE: Repository
PURPOSE: Abstracts data access for Buyer aggregate with PaymentMethods relationship loading.
RESPONSIBILITIES:
  - Persists new Buyer aggregates via Add() checking transient state
  - Updates existing Buyer entities via Update()
  - Retrieves Buyer by IdentityGuid (external user identity) with eager-loaded PaymentMethods collection via FindAsync()
  - Retrieves Buyer by internal database ID via FindByIdAsync()
  - Exposes UnitOfWork property for transactional operations
SOURCE: `Repositories/BuyerRepository.cs`
CALLS:
  - OrderingContext.Buyers.Add() — adds new buyer
  - OrderingContext.Buyers.Update() — updates buyer
  - OrderingContext.Buyers.Where().SingleOrDefaultAsync() — queries by IdentityGuid or ID with Include() for PaymentMethods
CALLED_BY:
  - ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler (Ordering.API)
  - BuyerAndPaymentMethodVerifiedDomainEventHandler (Ordering.API)

---

COMPONENT_NAME: MediatorExtension
TYPE: Utility
PURPOSE: Extracts domain events from change-tracked entities and publishes them via MediatR.
RESPONSIBILITIES:
  - Iterates Entity Framework Core change tracker to collect DomainEvents from all modified Entity objects
  - Clears domain events from entities to prevent duplicate publication
  - Publishes each domain event to registered MediatR handlers asynchronously
  - Executes before SaveChanges to ensure atomicity within single transaction
SOURCE: `MediatorExtension.cs`
CALLS:
  - DbContext.ChangeTracker.Entries<Entity>() — accesses modified entities
  - Entity.DomainEvents — reads events from aggregate root
  - IMediator.Publish() — publishes each event to handlers
CALLED_BY:
  - OrderingContext.SaveEntitiesAsync()

---

COMPONENT_NAME: RequestManager
TYPE: Utility
PURPOSE: Enforces idempotency by tracking command execution requests and detecting duplicates.
RESPONSIBILITIES:
  - Checks whether a command with given Guid ID has been previously executed via ExistAsync()
  - Creates new ClientRequest record with command type name and UTC timestamp via CreateRequestForCommandAsync<T>()
  - Throws OrderingDomainException if duplicate command ID detected
  - Persists idempotency records to ordering database
SOURCE: `Idempotency/RequestManager.cs`
CALLS:
  - OrderingContext.FindAsync<ClientRequest>() — queries existing request by ID
  - OrderingContext.Add() — adds new ClientRequest record
  - OrderingContext.SaveChangesAsync() — persists idempotency check
CALLED_BY:
  - CreateOrderCommandHandler (via IdempotencyBehavior in Ordering.API)

---

COMPONENT_NAME: ClientRequest
TYPE: Store
PURPOSE: Stores idempotency metadata to prevent duplicate command execution across retries.
RESPONSIBILITIES:
  - Records command execution history with immutable ID, command type name, and timestamp
  - Acts as a deduplication key for distributed command retries
SOURCE: `Idempotency/ClientRequest.cs`
CALLS:
  - NONE
CALLED_BY:
  - RequestManager
  - OrderingContext (DbSet<ClientRequest>)

---

COMPONENT_NAME: PostgreSQL Database (ordering schema)
TYPE: Store
PURPOSE: Persists Order, Buyer, PaymentMethod, OrderItem, CardType entities and integration event log using PostgreSQL RDBMS.
RESPONSIBILITIES:
  - Maintains 5-table schema for aggregates and value objects (orders, buyers, paymentmethods, order_items, cardtypes)
  - Stores ClientRequest idempotency records
  - Maintains integration event outbox table (via IntegrationEventLogEntry)
  - Enforces referential integrity via foreign keys
  - Supports HiLo sequences for distributed ID generation
SOURCE: Database engine; migrations in `Migrations/`
CALLS:
  - NONE
CALLED_BY:
  - OrderingContext
  - OrderRepository
  - BuyerRepository
  - IntegrationEventLogService<OrderingContext>

---

COMPONENT_NAME: IntegrationEventLogService<OrderingContext>
TYPE: Service
PURPOSE: Implements outbox pattern for reliable delivery of integration events to RabbitMQ message bus.
RESPONSIBILITIES:
  - Creates IntegrationEventLogEntry records before domain events are published
  - Marks entries as Published after successful RabbitMQ delivery
  - Handles eventual consistency for distributed transactions
  - Prevents loss of events due to process failure between SaveChanges and message publishing
SOURCE: `IntegrationEventLogEF` (external package)
CALLS:
  - OrderingContext.AddIntegrationEventLog() — persists outbox entry
  - OrderingContext.MarkEventAsPublishedAsync() — updates status to Published
CALLED_BY:
  - OrderingIntegrationEventService (Ordering.API)

## Component Call Sequences

### Use-Case 1: Create Order

STEP 1: CreateOrderCommandHandler → OrderRepository
  OPERATION: Add(new Order aggregate)
  PURPOSE: Persist new Order entity with items to database before domain event dispatch
  SOURCE: `Ordering.API/Application/Commands/CreateOrderCommandHandler.cs`

STEP 2: OrderRepository → OrderingContext
  OPERATION: SaveEntitiesAsync()
  PURPOSE: Trigger domain event dispatch and transactional persistence
  SOURCE: `OrderingContext.cs`

STEP 3: OrderingContext → MediatorExtension
  OPERATION: DispatchDomainEventsAsync(this)
  PURPOSE: Extract and publish OrderStartedDomainEvent to registered handlers
  SOURCE: `OrderingContext.cs`

STEP 4: Order aggregate → OrderStartedDomainEvent
  OPERATION: Publish domain event from aggregate
  PURPOSE: Notify infrastructure that order creation process started
  SOURCE: `Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`

STEP 5: ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler → BuyerRepository
  OPERATION: FindAsync(string identity) or Add(new Buyer)
  PURPOSE: Create or retrieve Buyer aggregate based on customer identity
  SOURCE: `Ordering.API/Application/DomainEventHandlers/ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler.cs`

STEP 6: Order aggregate → BuyerAndPaymentMethodVerifiedDomainEvent
  OPERATION: Raise domain event after buyer validation
  PURPOSE: Signal that buyer and payment method are verified and ready for stock validation
  SOURCE: `Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`

STEP 7: OrderingIntegrationEventService → IntegrationEventLogService<OrderingContext>
  OPERATION: CreateEventLogEntry(OrderStatusChangedToSubmittedIntegrationEvent)
  PURPOSE: Persist integration event to outbox for reliable delivery to Catalog service
  SOURCE: `Ordering.API/Services/OrderingIntegrationEventService.cs`

---

### Use-Case 2: Process External Integration Event (e.g., OrderStockConfirmedIntegrationEvent)

STEP 1: RabbitMQ Event Bus → OrderStockConfirmedIntegrationEventHandler
  OPERATION: Receive and deserialize OrderStockConfirmedIntegrationEvent
  PURPOSE: React to stock confirmation from Catalog service
  SOURCE: `Ordering.API/Application/IntegrationEventHandlers/OrderStockConfirmedIntegrationEventHandler.cs`

STEP 2: OrderStockConfirmedIntegrationEventHandler → SetAwaitingValidationOrderStatusCommand
  OPERATION: Send command to update order status
  PURPOSE: Move order to AwaitingValidation state after stock check passes
  SOURCE: `Ordering.API/Application/IntegrationEventHandlers/OrderStockConfirmedIntegrationEventHandler.cs`

STEP 3: SetAwaitingValidationOrderStatusCommandHandler → OrderRepository
  OPERATION: GetAsync(int orderId)
  PURPOSE: Retrieve Order aggregate for status update
  SOURCE: `Ordering.API/Application/Commands/SetAwaitingValidationOrderStatusCommandHandler.cs`

STEP 4: Order aggregate → OrderStatusChangedToAwaitingValidationDomainEvent
  OPERATION: SetAwaitingValidationStatus() raises domain event
  PURPOSE: Publish status change for observers (e.g., integration event publisher)
  SOURCE: `Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`

STEP 5: OrderingContext → SaveEntitiesAsync()
  OPERATION: Dispatch domain events and persist status change
  PURPOSE: Atomically update order in database and publish integration event
  SOURCE: `OrderingContext.cs`

STEP 6: OrderingIntegrationEventService → IntegrationEventLogService<OrderingContext>
  OPERATION: CreateEventLogEntry(OrderStatusChangedToAwaitingValidationIntegrationEvent)
  PURPOSE: Queue integration event for delivery to downstream services
  SOURCE: `Ordering.API/Services/OrderingIntegrationEventService.cs`

STEP 7: IntegrationEventLogService<OrderingContext> → RabbitMQ Event Bus
  OPERATION: Publish to topic/exchange for Ordering service
  PURPOSE: Notify Ordering orchestrat service and other listeners of status change
  SOURCE: `IntegrationEventLogEF` (external)

## Communication Channels

---

CHANNEL_TYPE: PostgreSQL (relational database)
ENDPOINT: `amqp://orderingdb` (connections string)
SOURCE: `appsettings.json`, injected via DbContext options
NOTES: Schema is `ordering`. Implemented via Entity Framework Core Npgsql provider. Connection pooling enabled.

---

CHANNEL_TYPE: RabbitMQ (message queue)
ENDPOINT: Host from configuration `amqp://localhost` (development), topic/exchange: `ordering`
SOURCE: `Ordering.API/Extensions/Extensions.cs` (service registration)
NOTES: 5 subscriptions registered for inbound integration events: GracePeriodConfirmedIntegrationEvent, OrderStockConfirmedIntegrationEvent, OrderStockRejectedIntegrationEvent, OrderPaymentFailedIntegrationEvent, OrderPaymentSucceededIntegrationEvent. Outgoing events published via IntegrationEventLogEntry outbox pattern.

## Dependency Registration and Wiring

---

DI_CONTAINER: Microsoft.Extensions.DependencyInjection (built-in ASP.NET Core DI)

REGISTRATION_FILE: `Ordering.API/Extensions/Extensions.cs`, method `AddOrderingServices()`

---

REGISTRATION: OrderingContext (DbContext)
LIFETIME: Scoped
INTERFACE: DbContext (implicit)
IMPLEMENTATION: OrderingContext
SNIPPET:
```csharp
services.AddDbContext<OrderingContext>(options =>
    options.UseNpgsql(connectionString));
```

---

REGISTRATION: IOrderRepository
LIFETIME: Scoped
INTERFACE: IOrderRepository (domain interface)
IMPLEMENTATION: OrderRepository
SNIPPET:
```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
```

---

REGISTRATION: IBuyerRepository
LIFETIME: Scoped
INTERFACE: IBuyerRepository (domain interface)
IMPLEMENTATION: BuyerRepository
SNIPPET:
```csharp
services.AddScoped<IBuyerRepository, BuyerRepository>();
```

---

REGISTRATION: IRequestManager
LIFETIME: Scoped
INTERFACE: IRequestManager (idempotency)
IMPLEMENTATION: RequestManager
SNIPPET:
```csharp
services.AddScoped<IRequestManager, RequestManager>();
```

---

REGISTRATION: IIntegrationEventLogService<OrderingContext>
LIFETIME: Scoped
INTERFACE: IIntegrationEventLogService<OrderingContext> (outbox pattern)
IMPLEMENTATION: IntegrationEventLogService<OrderingContext> (from IntegrationEventLogEF)
SNIPPET:
```csharp
services.AddIntegrationEventLogging<OrderingContext>();
```

---

REGISTRATION: IOrderingIntegrationEventService
LIFETIME: Scoped
INTERFACE: IOrderingIntegrationEventService
IMPLEMENTATION: OrderingIntegrationEventService
SNIPPET:
```csharp
services.AddScoped<IOrderingIntegrationEventService, 
    OrderingIntegrationEventService>();
```

---

REGISTRATION: MediatR Handlers and Pipeline Behaviors
LIFETIME: Varies (Singleton for handlers, Transient for behaviors)
SOURCE: `Ordering.API/Extensions/Extensions.cs`
NOTES: Registers all MediatR handlers from Ordering.API assembly. Applies TransactionBehavior and LoggingBehavior to command pipeline. FluentValidation validators configured for command validation.

## Configuration and Secrets

---

SOURCE_TYPE: appsettings.json / environment variables
KEYS: `ConnectionStrings:orderingdb`, `EventBus:HostName`, `EventBus:SubscriptionClientName`
SENSITIVE: NO (for HostName/SubscriptionClientName); YES (database credentials in production)
LOCATION: `appsettings.Development.json`, `appsettings.json` (Ordering.API)

---

SOURCE_TYPE: Environment variables / Azure Key Vault
KEYS: Database password (in production environment), RabbitMQ credentials (if required)
SENSITIVE: YES
LOCATION: Runtime secrets management, not checked into source control

---

SOURCE_TYPE: Configuration Classes
KEYS: CatalogOptions (if referenced), IntegrationEventRabbitMQSettings
SENSITIVE: NO for settings; YES for connection strings/credentials
LOCATION: Bound via IOptions<T> pattern in DI container

## Persistence and Data Access

---

DATABASE: PostgreSQL (version 11+)
DATA_ACCESS: Entity Framework Core (v7+) with Npgsql provider
MIGRATIONS_PATH: `Migrations/`
REPOSITORY_PATTERN: YES

---

REPOSITORY: IOrderRepository
IMPLEMENTATION: OrderRepository
METHODS:
  - Add(Order order): persists new Order aggregate
  - GetAsync(int orderId): retrieves with OrderItems loaded
  - Update(Order order): marks as modified
SOURCE: `Repositories/OrderRepository.cs`, `Ordering.Domain/AggregatesModel/OrderAggregate/IOrderRepository.cs`

---

REPOSITORY: IBuyerRepository
IMPLEMENTATION: BuyerRepository
METHODS:
  - Add(Buyer buyer): persists new Buyer (checks transient state)
  - Update(Buyer buyer): marks as modified
  - FindAsync(string identity): retrieves by IdentityGuid with PaymentMethods
  - FindByIdAsync(int id): retrieves by primary key with PaymentMethods
SOURCE: `Repositories/BuyerRepository.cs`, `Ordering.Domain/AggregatesModel/BuyerAggregate/IBuyerRepository.cs`

---

DATABASE_SCHEMA:
- Table: `orders` (schema: `ordering`) — Order aggregates with Address owned entity, status enum, payment/buyer FKs
- Table: `order_items` — OrderItem children with product ID, price, discount references
- Table: `buyers` — Buyer aggregates with IdentityGuid (user identity from Identity service)
- Table: `paymentmethods` — PaymentMethod children with card details and FK to CardType
- Table: `cardtypes` — Reference data for card types (Visa, MasterCard, etc.)
- Table: `ClientRequest` — Idempotency records with command type and timestamp
- Table: `IntegrationEventLog` — Outbox pattern records with event payload and publish status

---

ID_GENERATION: HiLo strategy
SEQUENCES: `orderseq`, `itemseq`, `paymentseq`, `buyerseq`, `cardtypeseq`, etc.
PURPOSE: Distributed ID generation without central counter
CONFIGURATION: `UseHiLo()` in entity configurations

## Patterns and Architecture Notes

---

PATTERN: Unit of Work
EVIDENCE: OrderingContext implements IUnitOfWork; SaveEntitiesAsync() provides atomic save
SNIPPET:
```csharp
public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
{
    await _mediator.DispatchDomainEventsAsync(this);
    _ = await base.SaveChangesAsync(cancellationToken);
    return true;
}
```
SOURCE: `OrderingContext.cs`

---

PATTERN: Repository
EVIDENCE: OrderRepository and BuyerRepository abstract data access behind interfaces
SNIPPET:
```csharp
public Order Add(Order order)
{
    return _context.Orders.Add(order).Entity;
}

public async Task<Order> GetAsync(int orderId)
{
    var order = await _context.Orders.FindAsync(orderId);
    if (order != null)
    {
        await _context.Entry(order).Collection(i => i.OrderItems).LoadAsync();
    }
    return order;
}
```
SOURCE: `Repositories/OrderRepository.cs`

---

PATTERN: Domain Event Dispatching
EVIDENCE: MediatorExtension extracts events from entities and publishes via MediatR before SaveChanges
SNIPPET:
```csharp
public static async Task DispatchDomainEventsAsync(this IMediator mediator, OrderingContext ctx)
{
    var domainEntities = ctx.ChangeTracker.Entries<Entity>()
        .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());
    var domainEvents = domainEntities.SelectMany(x => x.Entity.DomainEvents).ToList();
    domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());
    foreach (var domainEvent in domainEvents)
        await mediator.Publish(domainEvent);
}
```
SOURCE: `MediatorExtension.cs`

---

PATTERN: Outbox (Transactional Outbox)
EVIDENCE: IntegrationEventLogService persists events to outbox table before publishing to message bus
PURPOSE: Ensures no event loss if publisher fails; achieves eventual consistency
SOURCE: `IntegrationEventLogEF` package; integration point in `OrderingIntegrationEventService`

---

PATTERN: Idempotency
EVIDENCE: RequestManager prevents duplicate command execution
SNIPPET:
```csharp
public async Task<bool> ExistAsync(Guid id)
{
    var request = await _context.FindAsync<ClientRequest>(id);
    return request != null;
}
```
SOURCE: `Idempotency/RequestManager.cs`

---

PATTERN: Domain-Driven Design (DDD)
EVIDENCE: Order and Buyer are aggregate roots; OrderItem and PaymentMethod are entities within aggregates; Address is value object
PURPOSE: Encapsulates business logic, enforces invariants, publishes domain events
SOURCE: `Ordering.Domain/AggregatesModel/` (aggregate definitions); `EntityConfigurations/` (ORM mapping)

---

PATTERN: Entity Framework Core Configurations (Fluent API)
EVIDENCE: EntityTypeConfiguration classes configure HiLo IDs, owned entities, shadow properties, foreign keys
PURPOSE: Decouples persistence mapping from domain model
SOURCE: `EntityConfigurations/` folder

## Security and Operational Considerations

---

AUTHN_AUTHZ: Not directly configured in infrastructure layer; relies on API layer authorization
MECHANISM: Bearer token (JWT) validated in Ordering.API controllers; resource-based authorization for buyer data
NOTES: OrderRepository and BuyerRepository assume authenticated context; current user identity extracted in API handlers

---

KNOWN_RISKS:
- Database credentials in appsettings.json: Use Azure Key Vault or secrets manager in production
- No field-level encryption: Card details (CardNumber, CVC) stored as plaintext in PostgreSQL; should use column encryption
- CORS: Not configured in infrastructure; delegated to API layer
- No request rate limiting: Infrastructure layer does not enforce throttling; apply at API Gateway or API layer
- No audit logging: No audit history of data modifications; could log via EF Core interceptors

---

OBSERVABILITY: Logging
FRAMEWORK: Microsoft.Extensions.Logging (ILogger<T>)
CONFIGURATION: Integrated with ASP.NET Core logging pipeline
HEALTH_CHECKS: Entity Framework Core probe via HeartbeatQueryable in EF Core
METRICS: EF Core query execution time via Application Insights (if configured)
SOURCE: `Ordering.API/Extensions/Extensions.cs` (logging configuration); EF Core interceptors if added

---

DEPLOYMENT: Application runs as ASP.NET Core hosted service
DOCKER: Dockerfile and docker-compose.yml in root and build/ directories
KUBERNETES: Helm charts or Kubernetes manifests in root (deployment to aks via CI/CD)
NOTES: Requires PostgreSQL instance accessible at connection string; RabbitMQ for integration events

---

DATABASE_MIGRATIONS: No automatic migrations; must run `dotnet ef database update` before deploying Ordering.API
MIGRATION_SAFETY: Use explicit migrations tracked in version control; rollback supported via EF Core model snapshots
NOTES: MultiTenant concerns: schema defaults to "ordering"; if multi-tenant, consider per-tenant migrations or schema isolation
