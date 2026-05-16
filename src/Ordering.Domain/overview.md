# Ordering.Domain

eShop domain layer for order and buyer management using Domain-Driven Design patterns with event-driven architecture.

## Summary

- Implements DDD Aggregate pattern with two primary aggregates: Order and Buyer
- Encapsulates all order business logic including state transitions and event publishing
- Provides repository contracts for data access abstraction at the domain layer
- Publishes domain events to enable event-driven communication with other bounded contexts
- Uses MediatR for domain event notifications (INotification pattern)
- Maintains strict aggregate boundaries with private collections preventing external manipulation

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|-------------|
| `src/Ordering.Domain` | Domain layer containing aggregates, entities, value objects, and domain events | GlobalUsings.cs |
| `src/Ordering.Domain/AggregatesModel/OrderAggregate` | Encapsulates order business logic, state transitions, and line items | Order.cs |
| `src/Ordering.Domain/AggregatesModel/BuyerAggregate` | Manages buyer identity and payment method verification | Buyer.cs |
| `src/Ordering.Domain/Events` | Domain event definitions published on aggregate state changes | All 7 event files |
| `src/Ordering.Domain/SeedWork` | Reusable base classes and interfaces for DDD patterns | Entity.cs, ValueObject.cs, IAggregateRoot.cs |
| `src/Ordering.Domain/Exceptions` | Domain-specific exception types | OrderingDomainException.cs |

## Components

COMPONENT_NAME: Order
TYPE: Service
PURPOSE: Aggregate Root managing order lifecycle, line items, and state transitions with domain event publishing.
RESPONSIBILITIES:
  - Accept new orders with complete line items and address information
  - Maintain order state machine with six valid states (Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped or Cancelled)
  - Encapsulate OrderItem collection, preventing direct external access to maintain aggregate consistency
  - Publish domain events on each state transition (OrderStarted, OrderStatusChangedToAwaitingValidation, OrderStatusChangedToStockConfirmed, OrderStatusChangedToPaid, OrderShipped, OrderCancelled)
  - Validate order items for positive units and discount constraints before addition
  - Persist buyer and payment association metadata (BuyerId, PaymentId)
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`
CALLS:
  - OrderItem — to create and manage line items within the aggregate
  - Address — to store and persist immutable shipping address
  - Entity — to inherit domain event management (AddDomainEvent, ClearDomainEvents)
CALLED_BY:
  - Ordering.Infrastructure (OrderRepository implementation)
  - Ordering.API (Application service commands: CreateOrderCommand, OrderStatusChangedToAwaitingValidation, etc.)

---

COMPONENT_NAME: OrderItem
TYPE: Utility
PURPOSE: Child entity representing a line item within an Order aggregate.
RESPONSIBILITIES:
  - Store product reference and pricing details (ProductId, ProductName, UnitPrice, Discount, PictureUrl)
  - Validate non-negative units and discount-does-not-exceed-total constraints at construction
  - Provide mutable operations for discount adjustment and unit quantity increments
  - Enforce invariants preventing invalid quantities or prices during domain operations
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/OrderItem.cs`
CALLS:
  - Entity — to inherit identity and equality behaviors
CALLED_BY:
  - Order — when AddOrderItem() is invoked; when domain events reference line items (AwaitingValidation, Paid status changes)

---

COMPONENT_NAME: Address
TYPE: Utility
PURPOSE: Value Object representing an immutable shipping address.
RESPONSIBILITIES:
  - Store address components as immutable fields (Street, City, State, Country, ZipCode)
  - Implement value-object equality by component comparison (all five fields)
  - Provide persistence-agnostic value semantics for EF Core owned entity configuration
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Address.cs`
CALLS:
  - ValueObject — to inherit immutable equality semantics
CALLED_BY:
  - Order — to store shipping address; contained in Order aggregate

---

COMPONENT_NAME: OrderStatus
TYPE: Utility
PURPOSE: Enumeration defining valid order state transitions and JSON serialization rules.
RESPONSIBILITIES:
  - Define six valid order states: Submitted, AwaitingValidation, StockConfirmed, Paid, Shipped, Cancelled
  - Provide JSON serialization as string values (e.g., "Submitted" not "1") via JsonStringEnumConverter
  - Enforce state machine semantics at the database level (persisted as string)
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/OrderStatus.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — to store and transition state
  - OrderRepository — to query orders by status
  - Ordering.Application handlers — to create query predicates

---

COMPONENT_NAME: Buyer
TYPE: Service
PURPOSE: Aggregate Root managing buyer identity, payment method storage, and payment verification.
RESPONSIBILITIES:
  - Associate buyer domain identity (IdentityGuid from identity provider) with internal ID
  - Store and deduplicate payment methods by card type, number, and expiration
  - Verify or register new payment methods and publish verification events
  - Maintain one-to-many relationship to PaymentMethod collection with encapsulation
SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/Buyer.cs`
CALLS:
  - PaymentMethod — to create and deduplicate payment records; IsEqualTo() for comparison
  - Entity — to inherit domain event management (AddDomainEvent)
CALLED_BY:
  - Ordering.Infrastructure (BuyerRepository implementation)
  - Ordering.API (CreateOrderCommand via OrderService)

---

COMPONENT_NAME: PaymentMethod
TYPE: Utility
PURPOSE: Child entity representing a payment method within a Buyer aggregate.
RESPONSIBILITIES:
  - Store card metadata (CardTypeId, Alias, CardNumber, SecurityNumber, CardHolderName, Expiration)
  - Provide deduplication logic via IsEqualTo() by card type, number, and expiration
  - Maintain immutable reference within Buyer's payment collection
SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/PaymentMethod.cs`
CALLS:
  - NONE
CALLED_BY:
  - Buyer — when VerifyOrAddPaymentMethod() checks for existing payment; when raising BuyerAndPaymentMethodVerifiedDomainEvent

---

COMPONENT_NAME: OrderStartedDomainEvent
TYPE: Utility
PURPOSE: Domain event published when a new order is created.
RESPONSIBILITIES:
  - Capture complete order context including order object, buyer/payment card details, and user identity
  - Serve as INotification for MediatR event handler routing
  - Signal to downstream bounded contexts (Ordering.API handlers) that order creation is complete
SOURCE: `src/Ordering.Domain/Events/OrderStartedDomainEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — via AddDomainEvent() when Order constructor completes

---

COMPONENT_NAME: OrderStatusChangedToAwaitingValidationDomainEvent
TYPE: Utility
PURPOSE: Domain event published when order transitions to AwaitingValidation status.
RESPONSIBILITIES:
  - Capture order ID and complete line items at validation time
  - Signal that order is ready for inventory validation
  - Enable downstream inventory service consumers to react
SOURCE: `src/Ordering.Domain/Events/OrderStatusChangedToAwaitingValidationDomainEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — via AddDomainEvent() in SetAwaitingValidationStatus()

---

COMPONENT_NAME: OrderStatusChangedToStockConfirmedDomainEvent
TYPE: Utility
PURPOSE: Domain event published when order transitions to StockConfirmed status.
RESPONSIBILITIES:
  - Capture order ID at stock confirmation
  - Signal that all order items have available inventory
SOURCE: `src/Ordering.Domain/Events/OrderStatusChangedToStockConfirmedDomainEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — via AddDomainEvent() in SetStockConfirmedStatus()

---

COMPONENT_NAME: OrderStatusChangedToPaidDomainEvent
TYPE: Utility
PURPOSE: Domain event published when order transitions to Paid status.
RESPONSIBILITIES:
  - Capture order ID and line items at payment completion
  - Signal that order payment has been successfully received
  - Enable downstream fulfillment/shipping service consumers to react
SOURCE: `src/Ordering.Domain/Events/OrderStatusChangedToPaidDomainEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — via AddDomainEvent() in SetPaidStatus()

---

COMPONENT_NAME: OrderShippedDomainEvent
TYPE: Utility
PURPOSE: Domain event published when order transitions to Shipped status.
RESPONSIBILITIES:
  - Capture complete order aggregate at shipment
  - Signal that order has been dispatched to customer
SOURCE: `src/Ordering.Domain/Events/OrderShippedDomainEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — via AddDomainEvent() in SetShippedStatus()

---

COMPONENT_NAME: OrderCancelledDomainEvent
TYPE: Utility
PURPOSE: Domain event published when order is cancelled.
RESPONSIBILITIES:
  - Capture complete order aggregate at cancellation
  - Signal that order will not proceed to fulfillment
SOURCE: `src/Ordering.Domain/Events/OrderCancelledDomainEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — via AddDomainEvent() in SetCancelledStatus()

---

COMPONENT_NAME: BuyerAndPaymentMethodVerifiedDomainEvent
TYPE: Utility
PURPOSE: Domain event published when a buyer's payment method is verified or added.
RESPONSIBILITIES:
  - Capture Buyer aggregate, PaymentMethod entity, and associated Order ID
  - Signal that payment information has been validated and associated with an order
  - Enable downstream payment processing and authorization handlers
SOURCE: `src/Ordering.Domain/Events/BuyerAndPaymentMethodVerifiedDomainEvent.cs`
CALLS:
  - NONE
CALLED_BY:
  - Buyer — via AddDomainEvent() in VerifyOrAddPaymentMethod() (both new and existing paths)

---

COMPONENT_NAME: Entity
TYPE: Utility
PURPOSE: Base class providing identity, equality, and domain event management for all domain objects.
RESPONSIBILITIES:
  - Define transient vs persisted entity identity semantics (IsTransient() checks if Id == default)
  - Provide lazy-initialized domain event collection management (AddDomainEvent, RemoveDomainEvent, ClearDomainEvents)
  - Implement MediatR INotification collection for event publishing through domain event handlers
  - Support deferred event dispatch (events collected during domain operation, cleared after persistence)
SOURCE: `src/Ordering.Domain/SeedWork/Entity.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — inherits domain event collection and deferred dispatch mechanism
  - Buyer — inherits domain event collection and deferred dispatch mechanism
  - OrderItem — inherits identity and equality
  - PaymentMethod — inherits identity and equality

---

COMPONENT_NAME: ValueObject
TYPE: Utility
PURPOSE: Base class providing immutable value semantics and equality by component comparison.
RESPONSIBILITIES:
  - Define abstract GetEqualityComponents() for value-based equality comparison
  - Implement Equals and GetHashCode based on component equality (not reference equality)
  - Provide immutable semantics for domain value types
SOURCE: `src/Ordering.Domain/SeedWork/ValueObject.cs`
CALLS:
  - NONE
CALLED_BY:
  - Address — inherits value semantics

---

COMPONENT_NAME: IOrderRepository
TYPE: Utility
PURPOSE: Interface abstraction for Order persistence operations.
RESPONSIBILITIES:
  - Define contract for Order aggregate creation and persistence (Add, Update)
  - Define contract for Order aggregate retrieval by ID (GetAsync)
  - Inherit repository base contract via IRepository<Order>
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/IOrderRepository.cs`
CALLS:
  - NONE
CALLED_BY:
  - Ordering.Infrastructure.OrderRepository (implementation)
  - Ordering.Application (OrderService, command handlers)

---

COMPONENT_NAME: IBuyerRepository
TYPE: Utility
PURPOSE: Interface abstraction for Buyer persistence operations.
RESPONSIBILITIES:
  - Define contract for Buyer aggregate creation and persistence (Add, Update)
  - Define contract for Buyer retrieval by identity GUID (FindAsync) and internal ID (FindByIdAsync)
  - Inherit repository base contract via IRepository<Buyer>
SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/IBuyerRepository.cs`
CALLS:
  - NONE
CALLED_BY:
  - Ordering.Infrastructure.BuyerRepository (implementation)
  - Ordering.Application (OrderService, command handlers)

---

COMPONENT_NAME: IRepository
TYPE: Utility
PURPOSE: Generic base interface for all repository implementations.
RESPONSIBILITIES:
  - Expose IUnitOfWork reference for transactional consistency
  - Define generic constraint that repository types must be aggregate roots (T : IAggregateRoot)
SOURCE: `src/Ordering.Domain/SeedWork/IRepository.cs`
CALLS:
  - NONE
CALLED_BY:
  - IOrderRepository — inherited via generic specialization
  - IBuyerRepository — inherited via generic specialization

---

COMPONENT_NAME: IAggregateRoot
TYPE: Utility
PURPOSE: Marker interface identifying classes as DDD aggregate roots.
RESPONSIBILITIES:
  - Signal that a class represents an aggregate boundary (Order, Buyer)
  - Enable type-safe generic constraints in repository definitions
SOURCE: `src/Ordering.Domain/SeedWork/IAggregateRoot.cs`
CALLS:
  - NONE
CALLED_BY:
  - Order — implemented as marker interface
  - Buyer — implemented as marker interface
  - IRepository — used as generic type constraint

---

COMPONENT_NAME: IUnitOfWork
TYPE: Utility
PURPOSE: Interface defining transactional consistency semantics for repositories.
RESPONSIBILITIES:
  - Provide abstraction for commit semantics and transaction boundaries
  - Enable coordinated persistence of multiple aggregates within same transaction
SOURCE: `src/Ordering.Domain/SeedWork/IUnitOfWork.cs`
CALLS:
  - NONE
CALLED_BY:
  - IRepository — exposed on repository interface

---

COMPONENT_NAME: OrderingDomainException
TYPE: Utility
PURPOSE: Custom exception type for domain-level invariant violations.
RESPONSIBILITIES:
  - Distinguish domain logic exceptions from infrastructure/application exceptions
  - Provide localized error messaging for domain operation failures
SOURCE: `src/Ordering.Domain/Exceptions/OrderingDomainException.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrderItem — thrown when units <= 0 or discount > total
  - Order — may be thrown by domain methods on invariant violation

## Component Call Sequences

### Use-Case: Place Order

STEP 1: Ordering.API (OrdersController) → Order
  OPERATION: new Order(userId, userName, address, cardTypeId, cardNumber, cardSecurityNumber, cardHolderName, cardExpiration, buyerId, paymentMethodId)
  PURPOSE: Create new order aggregate with initial Submitted state and capture buyer payment information
  SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs` (constructor)

STEP 2: Order → Entity
  OPERATION: AddDomainEvent(OrderStartedDomainEvent)
  PURPOSE: Register domain event to be dispatched after persistence; signals start of order lifecycle
  SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs` (Order constructor calls AddOrderStartedDomainEvent)

STEP 3: Ordering.API (OrdersController) → Order
  OPERATION: AddOrderItem(productId, productName, unitPrice, discount, pictureUrl, units)
  PURPOSE: Add line items to order; validate units > 0 and discount constraints; deduplicate or append items
  SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs` (AddOrderItem)

STEP 4: Order → OrderItem
  OPERATION: new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units)
  PURPOSE: Create and instantiate child entity for each order line; validate invariants
  SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs` (AddOrderItem creates OrderItem)

STEP 5: Ordering.Infrastructure (OrderRepository) → IOrderRepository
  OPERATION: Add(order)
  PURPOSE: Persist order aggregate and all child entities (OrderItems, Address) to database
  SOURCE: `src/Ordering.Infrastructure/Repositories/OrderRepository.cs` (implementer of IOrderRepository)

STEP 6: Ordering.Infrastructure (DbContext) → Entity
  OPERATION: ClearDomainEvents()
  PURPOSE: Retrieve collected domain events from aggregate before SaveChanges, then clear for re-initialization
  SOURCE: `src/Ordering.Infrastructure/OrderingContext.cs` (event dispatcher logic post-SaveChanges)

STEP 7: Ordering.API (EventPublisher/MediatR) → OrderStartedDomainEvent (handlers)
  OPERATION: Publish(OrderStartedDomainEvent)
  PURPOSE: Dispatch domain event to all registered handlers (OrderStrategyService, payment verification, etc.)
  SOURCE: `src/Ordering.Domain/Events/OrderStartedDomainEvent.cs` (INotification implementation for MediatR)

### Use-Case: Verify Buyer Payment Method

STEP 1: Ordering.API (OrdersController) → Buyer
  OPERATION: VerifyOrAddPaymentMethod(cardTypeId, alias, cardNumber, securityNumber, cardHolderName, expiration, orderId)
  PURPOSE: Verify that buyer's payment method already exists (by card type, number, expiration) or add new method; raise verification event
  SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/Buyer.cs` (VerifyOrAddPaymentMethod)

STEP 2: Buyer → PaymentMethod
  OPERATION: IsEqualTo(cardTypeId, cardNumber, expiration)
  PURPOSE: Check if payment method already exists in buyer's collection via tuple-based equality comparison
  SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/PaymentMethod.cs` (IsEqualTo)

STEP 3: Buyer → PaymentMethod
  OPERATION: new PaymentMethod(cardTypeId, alias, cardNumber, securityNumber, cardHolderName, expiration)
  PURPOSE: If payment not found, instantiate new PaymentMethod entity and add to buyer's collection
  SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/Buyer.cs` (VerifyOrAddPaymentMethod - new path)

STEP 4: Buyer → Entity
  OPERATION: AddDomainEvent(BuyerAndPaymentMethodVerifiedDomainEvent)
  PURPOSE: Register domain event (occurs in both existing and new payment paths); signals buyer verification complete
  SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/Buyer.cs` (VerifyOrAddPaymentMethod calls AddDomainEvent)

STEP 5: Ordering.Infrastructure (BuyerRepository) → IBuyerRepository
  OPERATION: Update(buyer)
  PURPOSE: Persist updated Buyer aggregate (including new or existing PaymentMethod) to database
  SOURCE: `src/Ordering.Infrastructure/Repositories/BuyerRepository.cs` (implementer of IBuyerRepository)

STEP 6: Ordering.Infrastructure (DbContext) → Entity
  OPERATION: ClearDomainEvents()
  PURPOSE: Retrieve BuyerAndPaymentMethodVerifiedDomainEvent from aggregate before SaveChanges, then clear
  SOURCE: `src/Ordering.Infrastructure/OrderingContext.cs` (event dispatcher logic post-SaveChanges)

STEP 7: Ordering.API (EventPublisher/MediatR) → BuyerAndPaymentMethodVerifiedDomainEvent (handlers)
  OPERATION: Publish(BuyerAndPaymentMethodVerifiedDomainEvent)
  PURPOSE: Dispatch event to payment authorization and order validation handlers
  SOURCE: `src/Ordering.Domain/Events/BuyerAndPaymentMethodVerifiedDomainEvent.cs` (INotification implementation for MediatR)

## Communication Channels

| CHANNEL_TYPE | ENDPOINT / EXCHANGE / TOPIC | SOURCE | NOTES |
|--------------|---------------------------|--------|-------|
| Domain Events | 7 event types (INotification) | `src/Ordering.Domain/Events/*.cs` | MediatR in-process event dispatch; no external transport at domain layer involvement |

## Dependency Registration and Wiring

DI_CONTAINER: Microsoft.Extensions.DependencyInjection (built-in .NET IoC)

REGISTRATION_FILE: `src/Ordering.Infrastructure/Extensions/ServiceCollectionExtensions.cs` and `src/Ordering.API/Program.cs`

Repository Registrations (Ordering.Infrastructure layer):

| Lifetime | Abstraction | Concrete Implementation | Source |
|----------|-------------|------------------------|--------|
| Scoped | IOrderRepository | OrderRepository | `src/Ordering.Infrastructure/Repositories/OrderRepository.cs` |
| Scoped | IBuyerRepository | BuyerRepository | `src/Ordering.Infrastructure/Repositories/BuyerRepository.cs` |
| Scoped | IUnitOfWork | OrderingContext | `src/Ordering.Infrastructure/OrderingContext.cs` |

Domain services and aggregates are not registered in the container; they are instantiated directly via constructors by application layer command handlers.

MediatR Registration (Ordering.API layer):

```csharp
services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining(typeof(Program));
    // Domain event handlers auto-registered by assembly scanning
    configuration.ConfigurePublisher((provider, _) =>
        new RequestPublisher(provider));
});
```

## Configuration and Secrets

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|-------------|------|-----------|----------|
| Connection String | `ConnectionStrings:OrderingDB` | YES | `src/Ordering.API/appsettings.json` and environment overrides |
| Entity Framework Logging | `Logging:LogLevel:Microsoft.EntityFrameworkCore` | NO | `src/Ordering.API/appsettings.Development.json` |

No secrets are stored in Ordering.Domain itself; all configuration is provided by Ordering.API at runtime.

## Persistence and Data Access

DATABASE: SQL Server (DbContext: OrderingContext in Ordering.Infrastructure)

DATA_ACCESS: Entity Framework Core (EF Core 8.0+) with LINQ-to-SQL

MIGRATIONS_PATH: `src/Ordering.Infrastructure/Migrations/`

REPOSITORY_PATTERN: YES

Repository Abstractions and Implementations:

| Repository Interface | Concrete Implementation | Source |
|----------------------|------------------------|--------|
| IOrderRepository | OrderRepository | `src/Ordering.Infrastructure/Repositories/OrderRepository.cs` |
| IBuyerRepository | BuyerRepository | `src/Ordering.Infrastructure/Repositories/BuyerRepository.cs` |

Entity Mappings (EF Core Fluent API):

```csharp
// Order aggregate configuration in OrderingContext.OnModelCreating()
modelBuilder.Entity<Order>(ConfigureOrder);
modelBuilder.Entity<Buyer>(ConfigureBuyer);

// Owned entity: Address value object persisted as table columns
modelBuilder.Entity<Order>().OwnsOne(o => o.Address, a=>
{
  a.Property(p => p.Street).HasColumnName("street");
  a.Property(p => p.City).HasColumnName("city");
  // ... etc
});

// Owned collection: OrderItems as separate table (child rows per Order)
modelBuilder.Entity<Order>().OwnsMany(o => o.OrderItems, oi =>
{
  oi.Property<int>("id").ValueGeneratedOnAdd();
  oi.HasKey("id");
});
```

## Patterns and Architecture Notes

| PATTERN | EVIDENCE | SNIPPET |
|---------|----------|---------|
| Domain-Driven Design (DDD) | `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs` (Aggregate Root with encapsulated collections and invariants) | `private readonly List<OrderItem> _orderItems; public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();` — prevents external manipulation of order items collection |
| Aggregate Root Pattern | Order.cs and Buyer.cs implement IAggregateRoot marker interface | Order maintains strict transactional boundary; all state mutations occur through Order methods (AddOrderItem, SetAwaitingValidationStatus, etc.) not by external callers |
| Event Sourcing (Deferred) | Entity.cs AddDomainEvent() and OrderingContext event dispatcher logic | Events accumulated during aggregate lifetime, dispatched after persistence: `var domainEvents = _order.DomainEvents.ToList(); foreach(var event in domainEvents) await mediator.Publish(event);` |
| Value Object Pattern | Address.cs inherits ValueObject; implements GetEqualityComponents() | Address equals by all five field values (Street, City, State, Country, ZipCode), not by reference |
| Repository Pattern | IOrderRepository, IBuyerRepository interface contracts at domain layer; implementations in infrastructure | Domain layer defines WHAT persistence operations exist, infrastructure layer (OrderRepository) defines HOW they're executed against SQL database |
| Transactional Outbox (implied) | Domain events published via MediatR after DbContext.SaveChanges() | Events stored on Entity until SaveChanges, then retrieved and published to prevent message loss on failure |
| Enum as Value Object | OrderStatus.cs with JsonStringEnumConverter | Persisted as string ("Submitted", "Paid") not numeric; provides semantic meaning and human readability |

## Security and Operational Considerations

AUTHN_AUTHZ: Order and Buyer aggregates do not enforce authentication/authorization; these concerns are delegated to Ordering.API layer (controller-level [Authorize] attributes and claims validation)

File: `src/Ordering.API/Controllers/OrdersController.cs` — HTTP security filtering occurs here before domain operations

KNOWN_RISKS:

- **No payment data validation in domain**: Order constructor and VerifyOrAddPaymentMethod accept card details as strings without PCI-DSS validation (must be delegated to infrastructure Payment Service)
- **No encryption in domain**: Card numbers, security codes stored as plaintext strings in memory; encryption/tokenization responsibility of infrastructure layer
- **Discount unchecked for negative values**: OrderItem constructor validates `unitPrice * units >= discount` but does not reject negative discount values (could enable fraud if coupled with business logic flaw upstream)
- **No audit trail in domain**: State transitions (e.g., SetPaidStatus) do not record who/when/why; audit logging responsibility of infrastructure/application layers

OBSERVABILITY:

Logging: Serilog (configured in Ordering.API, not in domain layer)
File: `src/Ordering.API/Program.cs` — Serilog sink configuration

Metrics: Application Insights via OpenTelemetry (configured in eShop.ServiceDefaults)
File: `src/eShop.ServiceDefaults/Extensions/ServiceDefaults.cs` — telemetry registration

Health Check Endpoints: NONE at domain layer
File: `src/Ordering.API/Program.cs` — health endpoints registered at API layer

Observability is **external** to Ordering.Domain; domain layer publishes events, infrastructure/application layers observe them.

DEPLOYMENT:

Container: Dockerfile
Location: `src/Ordering.API/Dockerfile` (no separate container for domain layer; domain is part of Ordering.API assembly)

Orchestration: Kubernetes manifests reference Ordering.API service
Location: `deploy/k8s/` (not part of domain layer; deployment configuration in eShop.AppHost)
