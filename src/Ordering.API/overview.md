# Ordering.API — eShop Microservice for Order Management and Persistence

The Ordering Service manages the complete order lifecycle: order creation, payment validation, stock confirmation, shipment, and cancellation. It uses CQRS with MediatR, domain-driven design, RabbitMQ event-driven integration, and PostgreSQL persistence.

---

## Summary

- Order lifecycle state machine: Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped (or Cancelled)
- CQRS pattern with MediatR for command and query separation
- Domain-driven design with Order and Buyer aggregates; domain events dispatched within transactions
- RabbitMQ integration for inter-service communication (Stock, Payment, Basket, Identity services)
- PostgreSQL with Entity Framework Core; Outbox pattern for reliable event publishing
- API versioning (v1.0) with OpenAPI/Swagger and JWT Bearer authentication

---

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|------|---------|------------|
| `src/Ordering.API` | REST API layer; command/query routing and integration event handling | `Program.cs`, `Program.Testing.cs` |
| `src/Ordering.Domain` | Domain model; Order and Buyer aggregates; domain events | Order.cs, Buyer.cs |
| `src/Ordering.Infrastructure` | Data access layer; EF Core DbContext, repositories, migrations | `OrderingContext.cs`, `OrderRepository.cs`, `BuyerRepository.cs` |
| `src/Ordering.API/Apis` | REST endpoint definitions | `OrdersApi.cs`, `OrderServices.cs` |
| `src/Ordering.API/Application/Commands` | CQRS command handlers | `CreateOrderCommand/Handler.cs`, `SetAwaitingValidationOrderStatusCommand/Handler.cs` |
| `src/Ordering.API/Application/Queries` | CQRS query handlers and view models | `OrderQueries.cs`, `OrderViewModel.cs` |
| `src/Ordering.API/Application/DomainEventHandlers` | Domain event handlers (7 total) | Files named `*DomainEventHandler.cs` |
| `src/Ordering.API/Application/IntegrationEvents` | Integration event events and handlers | `OrderingIntegrationEventService.cs`, event handler files |
| `src/Ordering.API/Application/Behaviors` | MediatR pipeline behaviors for cross-cutting concerns | `LoggingBehavior.cs`, `ValidatorBehavior.cs`, `TransactionBehavior.cs` |
| `src/Ordering.Infrastructure/Migrations` | EF Core database migrations | `_Initial.cs`, `_Outbox.cs` |

---

## Components

COMPONENT_NAME: OrdersApi
TYPE: API
PURPOSE: REST API entry point for order creation, queries, status updates, and draft operations.
RESPONSIBILITIES:
  - Maps HTTP endpoints /api/orders with API versioning (v1.0)
  - Validates x-requestid header for idempotency
  - Wraps commands (CreateOrderCommand, CancelOrderCommand, ShipOrderCommand) in IdentifiedCommand for deduplication
  - Delegates to MediatR for CQRS processing
  - Extracts user identity from authentication claims
SOURCE: `src/Ordering.API/Apis/OrdersApi.cs`
CALLS:
  - MediatR — to route commands and queries
  - IdentityService — to extract current user identity
  - OrderQueries — to retrieve orders and card types
CALLED_BY:
  - NONE (external HTTP clients)

---

COMPONENT_NAME: OrderServices
TYPE: Utility
PURPOSE: Dependency injection container for order service dependencies exposed to endpoint handlers.
RESPONSIBILITIES:
  - Holds references to MediatR mediator, OrderQueries, IdentityService, and Logger
  - Enables parameter injection in endpoint handler methods
SOURCE: `src/Ordering.API/Apis/OrderServices.cs`
CALLS:
  - NONE
CALLED_BY:
  - OrdersApi

---

COMPONENT_NAME: CreateOrderCommandHandler
TYPE: Service
PURPOSE: Orchestrates the creation of a new order and validates buyer/payment method.
RESPONSIBILITIES:
  - Validates order request from CreateOrderCommand
  - Creates Order aggregate with address and payment metadata
  - Manages buyer and payment method verification via domain events
  - Persists order and buyer aggregates to repository
  - Initiates SaveEntitiesAsync to dispatch domain events
SOURCE: `src/Ordering.API/Application/Commands/CreateOrderCommand/Handler.cs`
CALLS:
  - Order — to construct new order aggregate
  - IOrderRepository — to persist the order
  - ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler — triggered via domain event dispatch
CALLED_BY:
  - IdentifiedCommandHandler

---

COMPONENT_NAME: IdentifiedCommandHandler
TYPE: Service
PURPOSE: Adds idempotency layer to prevent duplicate command processing.
RESPONSIBILITIES:
  - Checks IRequestManager for prior execution using x-requestid
  - On duplicate, returns cached result
  - On new request, delegates to the actual command handler (e.g., CreateOrderCommandHandler)
SOURCE: `src/Ordering.API/Application/Commands/IdentifiedCommand/Handler.cs`
CALLS:
  - IRequestManager — to check and record idempotent request
  - Actual command handler (CommandHandler<TCommand, TResult>) — to execute the command
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: SetAwaitingValidationOrderStatusCommandHandler
TYPE: Service
PURPOSE: Transitions order status to AwaitingValidation (typically triggered by grace period or payment processor).
RESPONSIBILITIES:
  - Retrieves order from IOrderRepository by OrderNumber
  - Calls Order.SetAwaitingValidationStatus() to update state and raise domain event
  - Persists via IUnitOfWork.SaveEntitiesAsync()
SOURCE: `src/Ordering.API/Application/Commands/SetAwaitingValidationOrderStatusCommand/Handler.cs`
CALLS:
  - IOrderRepository — to retrieve order
  - Order — to execute SetAwaitingValidationStatus()
  - IUnitOfWork — to persist and dispatch events
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: SetPaidOrderStatusCommandHandler
TYPE: Service
PURPOSE: Transitions order status to Paid upon successful payment.
RESPONSIBILITIES:
  - Retrieves order from IOrderRepository
  - Calls Order.SetPaidStatus() to update state and raise domain event
  - Persists via IUnitOfWork.SaveEntitiesAsync()
SOURCE: `src/Ordering.API/Application/Commands/SetPaidOrderStatusCommand/Handler.cs`
CALLS:
  - IOrderRepository — to retrieve order
  - Order — to execute SetPaidStatus()
  - IUnitOfWork — to persist and dispatch events
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: SetStockConfirmedOrderStatusCommandHandler
TYPE: Service
PURPOSE: Transitions order status to StockConfirmed upon inventory validation.
RESPONSIBILITIES:
  - Retrieves order from IOrderRepository
  - Calls Order.SetStockConfirmedStatus() to update state and raise domain event
  - Persists via IUnitOfWork.SaveEntitiesAsync()
SOURCE: `src/Ordering.API/Application/Commands/SetStockConfirmedOrderStatusCommand/Handler.cs`
CALLS:
  - IOrderRepository — to retrieve order
  - Order — to execute SetStockConfirmedStatus()
  - IUnitOfWork — to persist and dispatch events
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: SetStockRejectedOrderStatusCommandHandler
TYPE: Service
PURPOSE: Transitions order status to Cancelled if inventory validation fails.
RESPONSIBILITIES:
  - Retrieves order from IOrderRepository
  - Calls Order.SetCancelledStatus() to update state and raise domain event
  - Persists via IUnitOfWork.SaveEntitiesAsync()
SOURCE: `src/Ordering.API/Application/Commands/SetStockRejectedOrderStatusCommand/Handler.cs`
CALLS:
  - IOrderRepository — to retrieve order
  - Order — to execute SetCancelledStatus()
  - IUnitOfWork — to persist and dispatch events
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: CancelOrderCommandHandler
TYPE: Service
PURPOSE: Cancels an order if it is in a cancellable state (Paid or Shipped).
RESPONSIBILITIES:
  - Retrieves order from IOrderRepository by OrderNumber
  - Calls Order.SetCancelledStatus() to update state and raise domain event
  - Persists via IUnitOfWork.SaveEntitiesAsync()
SOURCE: `src/Ordering.API/Application/Commands/CancelOrderCommand/Handler.cs`
CALLS:
  - IOrderRepository — to retrieve order
  - Order — to execute SetCancelledStatus()
  - IUnitOfWork — to persist and dispatch events
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: ShipOrderCommandHandler
TYPE: Service
PURPOSE: Marks an order as shipped after payment confirmation.
RESPONSIBILITIES:
  - Retrieves order from IOrderRepository by OrderNumber
  - Calls Order.SetShippedStatus() to update state and raise domain event
  - Persists via IUnitOfWork.SaveEntitiesAsync()
SOURCE: `src/Ordering.API/Application/Commands/ShipOrderCommand/Handler.cs`
CALLS:
  - IOrderRepository — to retrieve order
  - Order — to execute SetShippedStatus()
  - IUnitOfWork — to persist and dispatch events
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: CreateOrderDraftCommandHandler
TYPE: Service
PURPOSE: Creates a temporary draft order before final submission.
RESPONSIBILITIES:
  - Constructs a new Order aggregate
  - Does not persist to main table; used for preview/validation
  - Returns order data for client display
SOURCE: `src/Ordering.API/Application/Commands/CreateOrderDraftCommand/Handler.cs`
CALLS:
  - Order — to construct order aggregate
CALLED_BY:
  - MediatR

---

COMPONENT_NAME: Order
TYPE: Utility
PURPOSE: Domain aggregate root for order state and behavior encapsulation.
RESPONSIBILITIES:
  - Enforces order lifecycle state machine
  - Maintains OrderItems collection (private, enforced via AddOrderItem method)
  - Publishes domain events on state transitions (OrderStarted, OrderStatusChanged*, OrderShipped, OrderCancelled)
  - Validates state transitions and item additions
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`
CALLS:
  - NONE (encapsulates state; side effects via domain events)
CALLED_BY:
  - CreateOrderCommandHandler, SetAwaitingValidationOrderStatusCommandHandler, SetPaidOrderStatusCommandHandler, SetStockConfirmedOrderStatusCommandHandler, SetStockRejectedOrderStatusCommandHandler, CancelOrderCommandHandler, ShipOrderCommandHandler, CreateOrderDraftCommandHandler

---

COMPONENT_NAME: Buyer
TYPE: Utility
PURPOSE: Domain aggregate root for buyer identity and payment method management.
RESPONSIBILITIES:
  - Maintains buyer identity (IdentityGuid from external Identity service)
  - Manages PaymentMethods collection
  - Publishes BuyerAndPaymentMethodVerifiedDomainEvent when payment method is verified or added
SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/Buyer.cs`
CALLS:
  - NONE (encapsulates state; side effects via domain events)
CALLED_BY:
  - ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler

---

COMPONENT_NAME: ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler
TYPE: Consumer
PURPOSE: Reacts to OrderStartedDomainEvent to validate or create the buyer and verify payment method.
RESPONSIBILITIES:
  - Handles OrderStartedDomainEvent published during order creation
  - Retrieves or creates Buyer aggregate via IBuyerRepository
  - Calls Buyer.VerifyOrAddPaymentMethod() to ensure payment method exists and is valid
  - Publishes OrderStatusChangedToSubmittedIntegrationEvent for external services (e.g., Basket)
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler.cs`
CALLS:
  - IBuyerRepository — to find or create buyer
  - Buyer — to verify/add payment method
  - IOrderingIntegrationEventService — to publish OrderStatusChangedToSubmittedIntegrationEvent
CALLED_BY:
  - OrderingContext (event dispatcher after SaveEntitiesAsync)

---

COMPONENT_NAME: OrderStatusChangedToAwaitingValidationDomainEventHandler
TYPE: Consumer
PURPOSE: Reacts to OrderStatusChangedToAwaitingValidationDomainEvent; triggers stock validation.
RESPONSIBILITIES:
  - Handles OrderStatusChangedToAwaitingValidationDomainEvent raised when order transitions to AwaitingValidation
  - Retrieves full Order and Buyer details for event payload
  - Publishes OrderStatusChangedToAwaitingValidationIntegrationEvent to notify Inventory/Stock service
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/OrderStatusChangedToAwaitingValidationDomainEventHandler.cs`
CALLS:
  - IOrderRepository — to load order with items
  - IBuyerRepository — to load buyer details
  - IOrderingIntegrationEventService — to publish OrderStatusChangedToAwaitingValidationIntegrationEvent
CALLED_BY:
  - OrderingContext (event dispatcher after SaveEntitiesAsync)

---

COMPONENT_NAME: OrderStatusChangedToPaidDomainEventHandler
TYPE: Consumer
PURPOSE: Reacts to OrderStatusChangedToPaidDomainEvent; records payment confirmation.
RESPONSIBILITIES:
  - Handles OrderStatusChangedToPaidDomainEvent raised when payment is confirmed
  - Publishes OrderStatusChangedToPaidIntegrationEvent for downstream services
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/OrderStatusChangedToPaidDomainEventHandler.cs`
CALLS:
  - IOrderingIntegrationEventService — to publish integration event
CALLED_BY:
  - OrderingContext (event dispatcher after SaveEntitiesAsync)

---

COMPONENT_NAME: OrderStatusChangedToStockConfirmedDomainEventHandler
TYPE: Consumer
PURPOSE: Reacts to OrderStatusChangedToStockConfirmedDomainEvent; confirms inventory reservation.
RESPONSIBILITIES:
  - Handles OrderStatusChangedToStockConfirmedDomainEvent raised when inventory is validated
  - Publishes OrderStatusChangedToStockConfirmedIntegrationEvent for downstream services
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/OrderStatusChangedToStockConfirmedDomainEventHandler.cs`
CALLS:
  - IOrderingIntegrationEventService — to publish integration event
CALLED_BY:
  - OrderingContext (event dispatcher after SaveEntitiesAsync)

---

COMPONENT_NAME: OrderCancelledDomainEventHandler
TYPE: Consumer
PURPOSE: Reacts to OrderCancelledDomainEvent; handles cancellation workflows.
RESPONSIBILITIES:
  - Handles OrderCancelledDomainEvent raised when order is cancelled
  - Publishes OrderStatusChangedToCancelledIntegrationEvent for downstream services (refund, inventory release)
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/OrderCancelledDomainEventHandler.cs`
CALLS:
  - IOrderingIntegrationEventService — to publish integration event
CALLED_BY:
  - OrderingContext (event dispatcher after SaveEntitiesAsync)

---

COMPONENT_NAME: OrderShippedDomainEventHandler
TYPE: Consumer
PURPOSE: Reacts to OrderShippedDomainEvent; records shipment confirmation.
RESPONSIBILITIES:
  - Handles OrderShippedDomainEvent raised when order is marked shipped
  - Publishes OrderStatusChangedToShippedIntegrationEvent for downstream services (shipping, invoicing)
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/OrderShippedDomainEventHandler.cs`
CALLS:
  - IOrderingIntegrationEventService — to publish integration event
CALLED_BY:
  - OrderingContext (event dispatcher after SaveEntitiesAsync)

---

COMPONENT_NAME: UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler
TYPE: Consumer
PURPOSE: Reacts to BuyerAndPaymentMethodVerifiedDomainEvent; updates order with verified payment ID.
RESPONSIBILITIES:
  - Handles BuyerAndPaymentMethodVerifiedDomainEvent raised when buyer payment method is verified
  - Updates the Order.PaymentId with the newly verified payment method ID
  - Persists order modification
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler.cs`
CALLS:
  - IOrderRepository — to retrieve and update order
  - IUnitOfWork — to persist changes
CALLED_BY:
  - OrderingContext (event dispatcher after SaveEntitiesAsync)

---

COMPONENT_NAME: GracePeriodConfirmedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Consumes GracePeriodConfirmedIntegrationEvent from external service (grace period completed).
RESPONSIBILITIES:
  - Handles GracePeriodConfirmedIntegrationEvent indicating grace period has ended and order may proceed
  - Issues SetAwaitingValidationOrderStatusCommand to transition order state
SOURCE: `src/Ordering.API/Application/IntegrationEvents/EventHandling/GracePeriodConfirmedIntegrationEventHandler.cs`
CALLS:
  - MediatR — to send SetAwaitingValidationOrderStatusCommand
CALLED_BY:
  - RabbitMQ/EventBus

---

COMPONENT_NAME: OrderPaymentSucceededIntegrationEventHandler
TYPE: Consumer
PURPOSE: Consumes OrderPaymentSucceededIntegrationEvent from Payment service.
RESPONSIBILITIES:
  - Handles OrderPaymentSucceededIntegrationEvent indicating payment has succeeded
  - Issues SetPaidOrderStatusCommand to transition order to Paid state
SOURCE: `src/Ordering.API/Application/IntegrationEvents/EventHandling/OrderPaymentSucceededIntegrationEventHandler.cs`
CALLS:
  - MediatR — to send SetPaidOrderStatusCommand
CALLED_BY:
  - RabbitMQ/EventBus

---

COMPONENT_NAME: OrderPaymentFailedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Consumes OrderPaymentFailedIntegrationEvent from Payment service.
RESPONSIBILITIES:
  - Handles OrderPaymentFailedIntegrationEvent indicating payment has failed
  - Issues CancelOrderCommand to transition order to Cancelled state
SOURCE: `src/Ordering.API/Application/IntegrationEvents/EventHandling/OrderPaymentFailedIntegrationEventHandler.cs`
CALLS:
  - MediatR — to send CancelOrderCommand
CALLED_BY:
  - RabbitMQ/EventBus

---

COMPONENT_NAME: OrderStockConfirmedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Consumes OrderStockConfirmedIntegrationEvent from Stock/Inventory service.
RESPONSIBILITIES:
  - Handles OrderStockConfirmedIntegrationEvent indicating inventory items are reserved
  - Issues SetStockConfirmedOrderStatusCommand to transition order state
SOURCE: `src/Ordering.API/Application/IntegrationEvents/EventHandling/OrderStockConfirmedIntegrationEventHandler.cs`
CALLS:
  - MediatR — to send SetStockConfirmedOrderStatusCommand
CALLED_BY:
  - RabbitMQ/EventBus

---

COMPONENT_NAME: OrderStockRejectedIntegrationEventHandler
TYPE: Consumer
PURPOSE: Consumes OrderStockRejectedIntegrationEvent from Stock/Inventory service.
RESPONSIBILITIES:
  - Handles OrderStockRejectedIntegrationEvent indicating inventory items are unavailable
  - Issues SetStockRejectedOrderStatusCommand to cancel the order
SOURCE: `src/Ordering.API/Application/IntegrationEvents/EventHandling/OrderStockRejectedIntegrationEventHandler.cs`
CALLS:
  - MediatR — to send SetStockRejectedOrderStatusCommand (which cancels the order)
CALLED_BY:
  - RabbitMQ/EventBus

---

COMPONENT_NAME: OrderQueries
TYPE: Service
PURPOSE: Query handler for read operations; provides order data and reference data (card types).
RESPONSIBILITIES:
  - Retrieves order by order ID with eager-loaded OrderItems
  - Retrieves all orders for a given user (by identity GUID)
  - Returns card types reference data (Amex, Visa, MasterCard)
  - Maps database entities to view models (OrderViewModel, OrderSummary)
SOURCE: `src/Ordering.API/Application/Queries/OrderQueries.cs`
CALLS:
  - OrderingContext (DbContext) — to query Orders, OrderItems, CardTypes tables
CALLED_BY:
  - OrdersApi

---

COMPONENT_NAME: IdentityService
TYPE: Service
PURPOSE: Extracts user identity from HTTP request authentication claims.
RESPONSIBILITIES:
  - Retrieves current user's identity GUID from ClaimsPrincipal in HttpContext
  - Provides user context to order queries and domain event handlers
SOURCE: `src/Ordering.API/Infrastructure/Services/IdentityService.cs`
CALLS:
  - NONE (reads from HttpRequest)
CALLED_BY:
  - OrdersApi, OrderQueries

---

COMPONENT_NAME: IOrderingIntegrationEventService
TYPE: Service
PURPOSE: Abstracts publishing of integration events to external event bus (RabbitMQ).
RESPONSIBILITIES:
  - AddAndSaveEventAsync(event) — saves integration event to Outbox table and publishes to event bus
  - Ensures reliable event delivery via Outbox pattern
SOURCE: `src/Ordering.API/Application/Integration/IOrderingIntegrationEventService.cs` (interface)
CALLS:
  - IIntegrationEventLogService — to persist event to Outbox
  - IEventBus — to publish event to RabbitMQ
CALLED_BY:
  - Domain event handlers

---

COMPONENT_NAME: OrderingIntegrationEventService
TYPE: Service
PURPOSE: Implementation of IOrderingIntegrationEventService; orchestrates Outbox-based event publishing.
RESPONSIBILITIES:
  - Persists integration events to IntegrationEventLog table (Outbox)
  - Publishes events to RabbitMQ EventBus
  - Handles transactional consistency between database and message bus
SOURCE: `src/Ordering.API/Application/Services/OrderingIntegrationEventService.cs`
CALLS:
  - IIntegrationEventLogService — to save event to Outbox
  - IEventBus — to publish event (but within same transaction scope)
CALLED_BY:
  - Domain event handlers

---

COMPONENT_NAME: OrderRepository
TYPE: Repository
PURPOSE: Data access abstraction for Order aggregate persistence.
RESPONSIBILITIES:
  - Add(order) — stages order in EF DbContext for insertion
  - GetAsync(orderId) — retrieves order with eager-loaded OrderItems collection
  - Update(order) — marks order as modified
  - Exposes IUnitOfWork UnitOfWork property for transaction management
SOURCE: `src/Ordering.Infrastructure/Repositories/OrderRepository.cs`
CALLS:
  - OrderingContext — to query/stage orders
CALLED_BY:
  - Command handlers, domain event handlers, queries

---

COMPONENT_NAME: BuyerRepository
TYPE: Repository
PURPOSE: Data access abstraction for Buyer aggregate persistence.
RESPONSIBILITIES:
  - Add(buyer) — stages buyer in EF DbContext (with transience check)
  - Update(buyer) — marks buyer as modified
  - FindAsync(identityGuid) — retrieves buyer by external identity with PaymentMethods loaded
  - FindByIdAsync(id) — retrieves buyer by database ID with PaymentMethods loaded
SOURCE: `src/Ordering.Infrastructure/Repositories/BuyerRepository.cs`
CALLS:
  - OrderingContext — to query/stage buyers
CALLED_BY:
  - Domain event handlers (ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler)

---

COMPONENT_NAME: OrderingContext
TYPE: Store
PURPOSE: Entity Framework Core DbContext for PostgreSQL order database.
RESPONSIBILITIES:
  - Defines DbSets for Order, OrderItem, Buyer, PaymentMethod, CardType, ClientRequest (for idempotency)
  - Configures database schema, indexes, sequences, and relationships via entity configurations
  - Implements IUnitOfWork interface with SaveEntitiesAsync() for transactional domain event dispatch
  - Dispatches collected domain events via MediatorExtension.DispatchDomainEventsAsync() before SaveChanges
  - Manages transaction isolation level (ReadCommitted) and connection pooling (disabled per comment)
SOURCE: `src/Ordering.Infrastructure/OrderingContext.cs`
CALLS:
  - MediatorExtension.DispatchDomainEventsAsync — to dispatch domain events before persistence
CALLED_BY:
  - OrderRepository, BuyerRepository, UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler, Order lifecycle

---

COMPONENT_NAME: RequestManager
TYPE: Service
PURPOSE: Manages idempotent request deduplication to prevent duplicate processing.
RESPONSIBILITIES:
  - ExistAsync(requestId) — checks if request ID already recorded
  - CreateRequestForCommandAsync<T>(requestId) — records new request in ClientRequest table
  - Enables safe retry logic by command callers
SOURCE: `src/Ordering.Infrastructure/Idempotency/RequestManager.cs`
CALLS:
  - OrderingContext — to query/insert ClientRequest records
CALLED_BY:
  - IdentifiedCommandHandler

---

COMPONENT_NAME: LoggingBehavior
TYPE: Service
PURPOSE: MediatR pipeline behavior for logging command/query execution.
RESPONSIBILITIES:
  - Logs command/query name, ID properties, and full request object on entry
  - Logs response or exception on exit
SOURCE: `src/Ordering.API/Application/Behaviors/LoggingBehavior.cs`
CALLS:
  - Logger — to record events
CALLED_BY:
  - MediatR pipeline

---

COMPONENT_NAME: ValidatorBehavior
TYPE: Service
PURPOSE: MediatR pipeline behavior for FluentValidation integration.
RESPONSIBILITIES:
  - Validates incoming commands/queries against registered validators
  - Throws ValidationException if validation fails
SOURCE: `src/Ordering.API/Application/Behaviors/ValidatorBehavior.cs`
CALLS:
  - Registered validators (e.g., CreateOrderCommandValidator, CancelOrderCommandValidator)
CALLED_BY:
  - MediatR pipeline

---

COMPONENT_NAME: TransactionBehavior
TYPE: Service
PURPOSE: MediatR pipeline behavior for transactional command wrapping.
RESPONSIBILITIES:
  - Begins database transaction before command execution
  - Commits transaction on success; rolls back on failure
  - Catches and rethrows exceptions with context
SOURCE: `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`
CALLS:
  - IUnitOfWork — to start, commit, and rollback transactions
CALLED_BY:
  - MediatR pipeline

---

COMPONENT_NAME: OrderingContextSeed
TYPE: Utility
PURPOSE: Database seed data loader for reference entities.
RESPONSIBILITIES:
  - Implements IDbSeeder<OrderingContext>
  - Seeds CardType reference data (Amex, Visa, MasterCard) on first run
SOURCE: `src/Ordering.API/Infrastructure/OrderingContextSeed.cs`
CALLS:
  - OrderingContext — to query/insert seed data
CALLED_BY:
  - Startup/migration pipeline

---

COMPONENT_NAME: EventBus (RabbitMQ)
TYPE: MessageQueue
PURPOSE: MediatR-style event bus for inter-service integration event publishing and subscription.
RESPONSIBILITIES:
  - Publishes integration events from OrderingIntegrationEventService to exchange
  - Receives and routes integration events to registered handlers
  - Manages subscriptions for 5 event types (GracePeriodConfirmed, OrderPayment*, OrderStock*, etc.)
SOURCE: `src/EventBusRabbitMQ/` (external project)
CALLS:
  - Ordering handlers — to notify of external events
CALLED_BY:
  - OrderingIntegrationEventService, integration event handlers

---

## Component Call Sequences

### Use-Case 1: Create/Place Order

STEP 1: HTTP Client → OrdersApi
  OPERATION: CreateOrderAsync(CreateOrderCommand)
  PURPOSE: REST endpoint handler validates x-requestid header and wraps command for deduplication
  SOURCE: `src/Ordering.API/Apis/OrdersApi.cs`

STEP 2: OrdersApi → MediatR
  OPERATION: Mediator.Send(IdentifiedCommand<CreateOrderCommand, bool>)
  PURPOSE: Delegates command to CQRS pipeline via MediatR
  SOURCE: `src/Ordering.API/Apis/OrdersApi.cs`

STEP 3: MediatR → ValidatorBehavior
  OPERATION: Validate(CreateOrderCommand)
  PURPOSE: Pipeline behavior checks FluentValidation rules
  SOURCE: `src/Ordering.API/Application/Behaviors/ValidatorBehavior.cs`

STEP 4: MediatR → LoggingBehavior
  OPERATION: Log command entry with ID properties
  PURPOSE: Records request metadata for tracing
  SOURCE: `src/Ordering.API/Application/Behaviors/LoggingBehavior.cs`

STEP 5: MediatR → TransactionBehavior
  OPERATION: BeginTransactionAsync()
  PURPOSE: Wraps command execution in database transaction
  SOURCE: `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`

STEP 6: TransactionBehavior → IdentifiedCommandHandler
  OPERATION: Handle(IdentifiedCommand<CreateOrderCommand, bool>)
  PURPOSE: Deduplication layer — checks RequestManager for prior execution
  SOURCE: `src/Ordering.API/Application/Commands/IdentifiedCommand/Handler.cs`

STEP 7: IdentifiedCommandHandler → RequestManager
  OPERATION: ExistAsync(requestId) or CreateRequestForCommandAsync(requestId)
  PURPOSE: Checks or records idempotent request ID
  SOURCE: `src/Ordering.Infrastructure/Idempotency/RequestManager.cs`

STEP 8: IdentifiedCommandHandler → CreateOrderCommandHandler
  OPERATION: Handle(CreateOrderCommand)
  PURPOSE: Core order creation business logic
  SOURCE: `src/Ordering.API/Application/Commands/CreateOrderCommand/Handler.cs`

STEP 9: CreateOrderCommandHandler → Order
  OPERATION: new Order(...) constructor
  PURPOSE: Creates Order aggregate root; initializes state and raises OrderStartedDomainEvent
  SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`

STEP 10: CreateOrderCommandHandler → IOrderRepository
  OPERATION: Add(order)
  PURPOSE: Stages order in DbContext for persistence
  SOURCE: `src/Ordering.Infrastructure/Repositories/OrderRepository.cs`

STEP 11: CreateOrderCommandHandler → IUnitOfWork
  OPERATION: SaveEntitiesAsync()
  PURPOSE: Persists order and dispatches domain events (OrderStartedDomainEvent)
  SOURCE: `src/Ordering.Infrastructure/OrderingContext.cs`

STEP 12: OrderingContext → MediatorExtension
  OPERATION: DispatchDomainEventsAsync()
  PURPOSE: Publishes collected domain events to registered handlers within same transaction
  SOURCE: `src/Ordering.Infrastructure/MediatorExtension.cs`

STEP 13: OrderingContext → ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler
  OPERATION: Handle(OrderStartedDomainEvent)
  PURPOSE: Validates/creates Buyer aggregate and verifies payment method
  SOURCE: `src/Ordering.API/Application/DomainEventHandlers/ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler.cs`

STEP 14: ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler → IBuyerRepository
  OPERATION: FindAsync(buyerIdentityGuid) or Add(newBuyer)
  PURPOSE: Retrieves or creates buyer aggregate
  SOURCE: `src/Ordering.Infrastructure/Repositories/BuyerRepository.cs`

STEP 15: ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler → Buyer
  OPERATION: VerifyOrAddPaymentMethod(...)
  PURPOSE: Ensures payment method exists; publishes BuyerAndPaymentMethodVerifiedDomainEvent
  SOURCE: `src/Ordering.Domain/AggregatesModel/BuyerAggregate/Buyer.cs`

STEP 16: ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler → IOrderingIntegrationEventService
  OPERATION: AddAndSaveEventAsync(OrderStatusChangedToSubmittedIntegrationEvent)
  PURPOSE: Publishes integration event to notify external services (e.g., Basket to clear items)
  SOURCE: `src/Ordering.API/Application/Services/OrderingIntegrationEventService.cs`

STEP 17: IOrderingIntegrationEventService → EventBus (RabbitMQ)
  OPERATION: Publish(OrderStatusChangedToSubmittedIntegrationEvent)
  PURPOSE: Sends event to RabbitMQ exchange for other microservices to consume
  SOURCE: `src/EventBusRabbitMQ/`

STEP 18: TransactionBehavior → IUnitOfWork
  OPERATION: CommitTransactionAsync()
  PURPOSE: Commits database transaction; transaction closes
  SOURCE: `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`

STEP 19: OrdersApi → HTTP Response
  OPERATION: Return 200 OK
  PURPOSE: Confirms order creation to client
  SOURCE: `src/Ordering.API/Apis/OrdersApi.cs`

---

### Use-Case 2: Order Status Transition (Payment Processing)

STEP 1: External Service (Payment) → EventBus (RabbitMQ)
  OPERATION: Publish(OrderPaymentSucceededIntegrationEvent)
  PURPOSE: Payment service notifies ordering service of successful payment
  SOURCE: `src/EventBusRabbitMQ/`

STEP 2: EventBus → OrderPaymentSucceededIntegrationEventHandler
  OPERATION: Handle(OrderPaymentSucceededIntegrationEvent)
  PURPOSE: Routes published event to registered handler
  SOURCE: `src/Ordering.API/Application/IntegrationEvents/EventHandling/OrderPaymentSucceededIntegrationEventHandler.cs`

STEP 3: OrderPaymentSucceededIntegrationEventHandler → MediatR
  OPERATION: Mediator.Send(SetPaidOrderStatusCommand)
  PURPOSE: Issues command to transition order state to Paid
  SOURCE: `src/Ordering.API/Application/IntegrationEvents/EventHandling/OrderPaymentSucceededIntegrationEventHandler.cs`

STEP 4: MediatR → TransactionBehavior
  OPERATION: BeginTransactionAsync()
  PURPOSE: Wraps command execution in database transaction
  SOURCE: `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`

STEP 5: TransactionBehavior → SetPaidOrderStatusCommandHandler
  OPERATION: Handle(SetPaidOrderStatusCommand)
  PURPOSE: Core state transition business logic
  SOURCE: `src/Ordering.API/Application/Commands/SetPaidOrderStatusCommand/Handler.cs`

STEP 6: SetPaidOrderStatusCommandHandler → IOrderRepository
  OPERATION: GetAsync(orderId) or GetByOrderNumberAsync(orderNumber)
  PURPOSE: Retrieves order aggregate from database
  SOURCE: `src/Ordering.Infrastructure/Repositories/OrderRepository.cs`

STEP 7: SetPaidOrderStatusCommandHandler → Order
  OPERATION: SetPaidStatus()
  PURPOSE: Updates order state to Paid; raises OrderStatusChangedToPaidDomainEvent
  SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`

STEP 8: SetPaidOrderStatusCommandHandler → IUnitOfWork
  OPERATION: SaveEntitiesAsync()
  PURPOSE: Persists order state change and dispatches domain events in same transaction
  SOURCE: `src/Ordering.Infrastructure/OrderingContext.cs`

STEP 9: OrderingContext → OrderStatusChangedToPaidDomainEventHandler
  OPERATION: Handle(OrderStatusChangedToPaidDomainEvent)
  PURPOSE: Reacts to Paid status; publishes integration event for downstream services
  SOURCE: `src/Ordering.API/Application/DomainEventHandlers/OrderStatusChangedToPaidDomainEventHandler.cs`

STEP 10: OrderStatusChangedToPaidDomainEventHandler → IOrderingIntegrationEventService
  OPERATION: AddAndSaveEventAsync(OrderStatusChangedToPaidIntegrationEvent)
  PURPOSE: Publishes integration event for shipping, fulfillment, or analytics services
  SOURCE: `src/Ordering.API/Application/Services/OrderingIntegrationEventService.cs`

STEP 11: IOrderingIntegrationEventService → EventBus (RabbitMQ)
  OPERATION: Publish(OrderStatusChangedToPaidIntegrationEvent)
  PURPOSE: Broadcasts event to external subscribers
  SOURCE: `src/EventBusRabbitMQ/`

STEP 12: TransactionBehavior → IUnitOfWork
  OPERATION: CommitTransactionAsync()
  PURPOSE: Commits database transaction; transaction closes
  SOURCE: `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`

---

## Communication Channels

CHANNEL_TYPE: HTTP
ENDPOINT: /api/orders
RESOURCE_METHODS:
  - POST / — Create new order
  - POST /draft — Create draft order
  - GET /{orderId} — Retrieve order by ID
  - GET / — List orders for current user
  - PUT /ship — Mark order as shipped
  - PUT /cancel — Cancel order
  - GET /cardtypes — Retrieve payment card type reference data
SOURCE: `src/Ordering.API/Apis/OrdersApi.cs`
NOTES: API versioning v1.0; all write operations require Bearer JWT token authorization; request ID via x-requestid header for idempotency

---

CHANNEL_TYPE: gRPC
ENDPOINT: NONE
SOURCE: NONE
NOTES: Ordering.API does not use gRPC communication

---

CHANNEL_TYPE: MessageQueue
ENDPOINT: RabbitMQ amqp://localhost (configurable)
EXCHANGE: Ordering (subscription client name)
PUBLISHED_EVENTS:
  - OrderStartedIntegrationEvent — order creation begins
  - OrderStatusChangedToSubmittedIntegrationEvent — buyer verified, order submitted
  - OrderStatusChangedToAwaitingValidationIntegrationEvent — requesting stock validation
  - OrderStatusChangedToStockConfirmedIntegrationEvent — stock confirmed
  - OrderStatusChangedToPaidIntegrationEvent — payment confirmed
  - OrderStatusChangedToShippedIntegrationEvent — order shipped
  - OrderStatusChangedToCancelledIntegrationEvent — order cancelled
SUBSCRIBED_EVENTS:
  - GracePeriodConfirmedIntegrationEvent — from grace period service
  - OrderPaymentSucceededIntegrationEvent — from payment service
  - OrderPaymentFailedIntegrationEvent — from payment service
  - OrderStockConfirmedIntegrationEvent — from stock service
  - OrderStockRejectedIntegrationEvent — from stock service
SOURCE: `src/Ordering.API/Extensions/Extensions.cs` (AddEventBusSubscriptions method), `src/Ordering.API/Application/IntegrationEvents/`
NOTES: Uses Outbox pattern via IntegrationEventLog table for reliable event delivery; events persisted before sending to ensure at-least-once semantics

---

CHANNEL_TYPE: Webhook
ENDPOINT: NONE
SOURCE: NONE
NOTES: Ordering.API does not publish or consume webhooks

---

## Dependency Registration and Wiring

DI_CONTAINER: Microsoft.Extensions.DependencyInjection (built-in .NET DI with extension methods)

REGISTRATION_FILE: `src/Ordering.API/Extensions/Extensions.cs`, method `AddApplicationServices()`

### Registrations

**1. Database and ORM**
- **Lifetime**: Scoped
- **Abstraction**: DbContext
- **Concrete**: OrderingContext
- **Code Snippet**:
```csharp
services.AddDbContext<OrderingContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("orderingdb"));
});
builder.EnrichNpgsqlDbContext<OrderingContext>();
```

**2. Database Seeding**
- **Lifetime**: Transient
- **Abstraction**: IDbSeeder<OrderingContext>
- **Concrete**: OrderingContextSeed
- **Code Snippet**:
```csharp
services.AddMigration<OrderingContext, OrderingContextSeed>();
```

**3. Integration Event Logging**
- **Lifetime**: Transient
- **Abstraction**: IIntegrationEventLogService
- **Concrete**: IntegrationEventLogService<OrderingContext>
- **Code Snippet**:
```csharp
services.AddTransient<IIntegrationEventLogService, 
    IntegrationEventLogService<OrderingContext>>();
```

**4. Integration Event Publishing**
- **Lifetime**: Transient
- **Abstraction**: IOrderingIntegrationEventService
- **Concrete**: OrderingIntegrationEventService
- **Code Snippet**:
```csharp
services.AddTransient<IOrderingIntegrationEventService, 
    OrderingIntegrationEventService>();
```

**5. RabbitMQ Event Bus**
- **Lifetime**: Per event bus builder registration
- **Abstraction**: IEventBus
- **Concrete**: RabbitMQ implementation (from EventBusRabbitMQ project)
- **Code Snippet**:
```csharp
builder.AddRabbitMqEventBus("eventbus")
       .AddEventBusSubscriptions();
```

**6. Authentication**
- **Lifetime**: Per service defaults
- **Abstraction**: Built-in authentication pipeline
- **Code Snippet**:
```csharp
builder.AddDefaultAuthentication();
```

**7. HTTP Context Accessor**
- **Lifetime**: Singleton
- **Abstraction**: IHttpContextAccessor
- **Code Snippet**:
```csharp
services.AddHttpContextAccessor();
```

**8. User Identity Service**
- **Lifetime**: Transient
- **Abstraction**: IIdentityService
- **Concrete**: IdentityService
- **Code Snippet**:
```csharp
services.AddTransient<IIdentityService, IdentityService>();
```

**9. MediatR (CQRS)**
- **Lifetime**: Varies (handlers are transient by default)
- **Abstraction**: IMediator
- **Concrete**: Mediator from MediatR
- **Code Snippet**:
```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining(typeof(Program));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});
```

**10. FluentValidation**
- **Lifetime**: Transient
- **Abstraction**: IValidator<T> (implicit via registration)
- **Code Snippet**:
```csharp
services.AddValidatorsFromAssemblyContaining<CancelOrderCommandValidator>();
```

**11. Query Handler**
- **Lifetime**: Scoped
- **Abstraction**: IOrderQueries
- **Concrete**: OrderQueries
- **Code Snippet**:
```csharp
services.AddScoped<IOrderQueries, OrderQueries>();
```

**12. Repositories**
- **Lifetime**: Scoped
- **Abstractions/Concretes**:
  - `IBuyerRepository` → `BuyerRepository`
  - `IOrderRepository` → `OrderRepository`
- **Code Snippet**:
```csharp
services.AddScoped<IBuyerRepository, BuyerRepository>();
services.AddScoped<IOrderRepository, OrderRepository>();
```

**13. Idempotency Manager**
- **Lifetime**: Scoped
- **Abstraction**: IRequestManager
- **Concrete**: RequestManager
- **Code Snippet**:
```csharp
services.AddScoped<IRequestManager, RequestManager>();
```

---

## Configuration and Secrets

### Configuration Sources

**SOURCE_TYPE**: config file (appsettings.json)
KEYS:
  - Logging:LogLevel:Default — log level (typically "Information")
  - Logging:LogLevel:Microsoft.AspNetCore — framework log level (typically "Warning")
  - AllowedHosts — allowed host origins (commonly "*" for local dev)
  - OpenApi:Endpoint:Name — Swagger UI title
  - OpenApi:Document:Title — API documentation title
  - OpenApi:Document:Description — API documentation description
  - OpenApi:Document:Version — API documentation version
  - OpenApi:Auth:ClientId — OAuth/OpenID client ID for Swagger UI
  - OpenApi:Auth:AppName — OAuth application name
  - ConnectionStrings:EventBus — RabbitMQ AMQP connection string
  - EventBus:SubscriptionClientName — RabbitMQ subscription client name
  - Identity:Audience — JWT audience claim validation
  - Identity:Scopes — OAuth scopes
SENSITIVE: NO
LOCATION: `src/Ordering.API/appsettings.json`

---

**SOURCE_TYPE**: config file (appsettings.Development.json)
KEYS:
  - ConnectionStrings:OrderingDB — PostgreSQL connection string (includes host, database, username, password)
SENSITIVE: YES (contains password; example shows "yourWeak(!)Password")
LOCATION: `src/Ordering.API/appsettings.Development.json`
NOTES: Development file; excluded from source control in production; uses local PostgreSQL host=localhost; database name=OrderingDB; default user=postgres

---

**SOURCE_TYPE**: environment variable (implicit)
KEYS:
  - ASPNETCORE_ENVIRONMENT — controls which appsettings file is loaded (Development, Production, Staging)
  - Secrets via user-secrets (development-only)
  - Certificate paths for HTTPS (if configured outside config files)
SENSITIVE: Variable by environment
LOCATION: Process environment or `dotnet user-secrets` (per UserSecretsId: 7161b768-033d-41c7-bc5d-37528275e1f3)

---

**SOURCE_TYPE**: Azure Key Vault or cloud config service (conditional)
KEYS: NONE currently configured in code
SENSITIVE: N/A
LOCATION: N/A
NOTES: Infrastructure layer (Aspire) may integrate Key Vault via Aspire.Azure packages; not explicitly configured in Extensions.cs

---

## Persistence and Data Access

DATABASE: OrderingDB (PostgreSQL 12+)
ENGINE: PostgreSQL
SCHEMA: "ordering" (explicit schema per OrderingContext configuration)
CONNECTION_STRING (Development): Host=localhost;Database=OrderingDB;Username=postgres;Password=...

---

DATA_ACCESS_LAYER: Entity Framework Core 8.0+ with NpgSQL provider
ORM: Entity Framework Core
DRIVER: Npgsql (NpgSQL.EntityFrameworkCore.PostgreSQL)
QUERY_BUILDER: LINQ-to-SQL (via EF Core)

---

MIGRATIONS_PATH: `src/Ordering.Infrastructure/Migrations/`

Existing migrations:
- `_20230925222426_Initial.cs` — Creates core schema: Orders, OrderItems, Buyers, PaymentMethods, CardTypes tables; defines sequences (buyerseq, orderseq, orderitemseq, paymentseq) for ID generation
- `_20231021004633_FixOrderitemseqSchema.cs` — Schema corrections
- `_20231026091055_Outbox.cs` — Introduces IntegrationEventLog table for Outbox pattern
- `_20240106121712_UseEnumForOrderStatus.cs` — Converts OrderStatus to enum type

---

REPOSITORY_PATTERN: YES

**Order Repository**
- **Abstraction**: `IOrderRepository` (interface in Ordering.Domain)
- **Concrete**: `OrderRepository` (in Ordering.Infrastructure/Repositories/)
- **Lifetime**: Scoped
- **Methods**:
  - `Add(Order order)` — stages order for insertion
  - `GetAsync(int orderId)` — retrieves order with OrderItems eager-loaded
  - `Update(Order order)` — marks order as modified
  - `UnitOfWork` property — exposes OrderingContext for transaction control

---

**Buyer Repository**
- **Abstraction**: `IBuyerRepository` (interface in Ordering.Domain)
- **Concrete**: `BuyerRepository` (in Ordering.Infrastructure/Repositories/)
- **Lifetime**: Scoped
- **Methods**:
  - `Add(Buyer buyer)` — stages buyer for insertion (transience check applied)
  - `Update(Buyer buyer)` — marks buyer as modified
  - `FindAsync(string identityGuid)` — retrieves buyer by identity with PaymentMethods loaded
  - `FindByIdAsync(int id)` — retrieves buyer by database ID with PaymentMethods loaded

---

**Unit of Work**
- **Pattern**: OrderingContext implements `IUnitOfWork`
- **Methods**:
  - `SaveEntitiesAsync()` — dispatches domain events via MediatorExtension, then SaveChangesAsync
  - `BeginTransactionAsync()` — starts new transaction (IsolationLevel.ReadCommitted)
  - `CommitTransactionAsync()` — commits current transaction
  - `RollbackTransaction()` — rolls back current transaction
- **Source**: `src/Ordering.Infrastructure/OrderingContext.cs`

---

**Request Manager (Idempotency)**
- **Abstraction**: `IRequestManager`
- **Concrete**: `RequestManager`
- **Lifetime**: Scoped
- **Table**: ClientRequest (tracks processed idempotent request IDs)
- **Methods**:
  - `ExistAsync(Guid commandId)` — checks if requestId already recorded
  - `CreateRequestForCommandAsync<T>(Guid commandId)` — records new requestId for command type T
- **Source**: `src/Ordering.Infrastructure/Idempotency/RequestManager.cs`

---

**Entity Configurations** (in `src/Ordering.Infrastructure/EntityConfigurations/`)
- `OrderEntityTypeConfiguration` — HiLo sequence "orderseq"; OwnsOne Address value object
- `OrderItemEntityTypeConfiguration` — HiLo sequence "orderitemseq"
- `BuyerEntityTypeConfiguration` — HiLo sequence "buyerseq"; unique index on IdentityGuid
- `PaymentMethodEntityTypeConfiguration` — HiLo sequence "paymentseq"
- `CardTypeEntityTypeConfiguration` — ValueGeneratedNever (reference data)
- `ClientRequestEntityTypeConfiguration` — idempotency tracking

---

## Patterns and Architecture Notes

**PATTERN**: Domain-Driven Design (DDD)
EVIDENCE:
- Order and Buyer are Aggregate Roots; OrderItem and PaymentMethod are child entities
- Domain events (OrderStarted, OrderStatusChanged*, OrderShipped, OrderCancelled) encapsulate state transitions
- Repository abstraction mediates access to aggregates
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs`, `src/Ordering.Domain/AggregatesModel/BuyerAggregate/Buyer.cs`
SNIPPET:
```csharp
public class Order : Entity, IAggregateRoot
{
    public void SetAwaitingValidationStatus()
    {
        this.OrderStatus = OrderStatus.AwaitingValidation;
        this.AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(...));
    }
}
```

---

**PATTERN**: Command Query Responsibility Segregation (CQRS)
EVIDENCE:
- Commands (CreateOrderCommand, SetAwaitingValidationOrderStatusCommand, etc.) modify state
- Queries (OrderQueries) provide read-only access with view models
- MediatR routes commands and queries to separate handler classes
- Command handlers may delegate cross-cutting concerns to MediatR behaviors
SOURCE: `src/Ordering.API/Application/Commands/`, `src/Ordering.API/Application/Queries/OrderQueries.cs`, `src/Ordering.API/Extensions/Extensions.cs`
SNIPPET:
```csharp
public record CreateOrderCommand(...) : IRequest<bool> { }
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, bool> { }
```

---

**PATTERN**: Mediator (Gang of Four behavioral pattern)
EVIDENCE:
- MediatR library provides IMediator interface
- Command handlers registered with MediatR.RegisterServicesFromAssemblyContaining(...)
- Pipeline behaviors (LoggingBehavior, ValidatorBehavior, TransactionBehavior) intercept requests
SOURCE: `src/Ordering.API/Extensions/Extensions.cs`, `src/Ordering.API/Application/Behaviors/`
SNIPPET:
```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining(typeof(Program));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
});
```

---

**PATTERN**: Outbox (Transactional Outbox for reliable event publishing)
EVIDENCE:
- IntegrationEventLog table stores events before sending to RabbitMQ
- OrderingIntegrationEventService.AddAndSaveEventAsync(event) persists to Outbox
- External process (Outbox Worker or similar) polls IntegrationEventLog and publishes via EventBus
- Ensures at-least-once delivery; avoids dual-write problem
SOURCE: `src/Ordering.Infrastructure/Migrations/_20231026091055_Outbox.cs`, `src/Ordering.API/Application/Services/OrderingIntegrationEventService.cs`, `src/IntegrationEventLogEF/`
SNIPPET:
```csharp
public async Task AddAndSaveEventAsync(IntegrationEvent @event)
{
    await _integrationEventLogService.SaveEventAsync(@event);
    await _eventBus.PublishAsync(@event);
}
```

---

**PATTERN**: Repository (Abstraction over data access)
EVIDENCE:
- IOrderRepository, IBuyerRepository interfaces defined in Ordering.Domain
- OrderRepository, BuyerRepository implementations in Ordering.Infrastructure
- Command handlers depend on repository abstractions, not concrete EF Core DbContext
SOURCE: `src/Ordering.Domain/AggregatesModel/OrderAggregate/IOrderRepository.cs`, `src/Ordering.Infrastructure/Repositories/OrderRepository.cs`

---

**PATTERN**: Event-Driven Architecture (intra-process and inter-service)
EVIDENCE:
- Domain events (OrderStarted, OrderStatusChanged*) published within SaveEntitiesAsync
- Domain event handlers receive and react to events within same database transaction
- Integration events (OrderStatusChangedToAwaitingValidationIntegrationEvent, etc.) published to RabbitMQ
- Integration event handlers receive external events (payment, stock confirmation) and issue commands
SOURCE: `src/Ordering.API/Application/DomainEventHandlers/`, `src/Ordering.API/Application/IntegrationEvents/EventHandling/`, `src/Ordering.Infrastructure/OrderingContext.cs`

---

**PATTERN**: Idempotency (Idempotent Command Processing)
EVIDENCE:
- IdentifiedCommand wraps command with x-requestid header value
- IdentifiedCommandHandler checks RequestManager before processing
- ClientRequest entity persists request IDs to prevent duplicate execution
- Safe retries enabled by idempotent keys
SOURCE: `src/Ordering.API/Application/Commands/IdentifiedCommand/Handler.cs`, `src/Ordering.Infrastructure/Idempotency/RequestManager.cs`

---

**PATTERN**: Transactional Consistency (ACID semantics within single database)
EVIDENCE:
- TransactionBehavior wraps command execution in BeginTransactionAsync / CommitTransactionAsync
- IsolationLevel.ReadCommitted prevents dirty reads
- Domain events dispatched before SaveChanges ensures all writes in same transaction
- Rollback on handler exception
SOURCE: `src/Ordering.API/Application/Behaviors/TransactionBehavior.cs`, `src/Ordering.Infrastructure/OrderingContext.cs`

---

**PATTERN**: Validation Pipeline (FluentValidation integration)
EVIDENCE:
- ValidatorBehavior checks FluentValidation rules before command execution
- Validators registered in DI container via AddValidatorsFromAssemblyContaining
- Validators defined for CreateOrderCommand, CancelOrderCommand, ShipOrderCommand, IdentifiedCommand
SOURCE: `src/Ordering.API/Application/Behaviors/ValidatorBehavior.cs`, `src/Ordering.API/Application/Validations/`

---

**PATTERN**: Dependency Injection (Inversion of Control via constructor injection)
EVIDENCE:
- OrderRepository, BuyerRepository, command handlers depend on OrderingContext via constructor
- IOrderingIntegrationEventService injected into domain event handlers
- IIdentityService injected into IdentityService and query handlers
- MediatR behaviors receive dependencies via constructor
SOURCE: All handler and service classes

---

## Security and Operational Considerations

### Authentication and Authorization

AUTHN_AUTHZ: JWT Bearer token (via `AddDefaultAuthentication()`)
CONFIGURED_IN: `src/Ordering.API/Extensions/Extensions.cs` (builder.AddDefaultAuthentication())
MECHANISM:
- OpenID Connect / OAuth 2.0 integration (via service defaults)
- ClientId for Swagger UI: "orderingswaggerui" (appsettings.json)
- JWT token validation on incoming requests
- All write operations (PUT, POST) require authorization via `.RequireAuthorization()` on route group
- Read operations (GET) may be authorized depending on route-level configuration
POLICIES: NONE currently defined; implicit authorization check

---

### Known Risks / Security Concerns

1. **Hardcoded Development Password**: appsettings.Development.json contains example password "yourWeak(!)Password" — must **never** be committed with real credentials; use `dotnet user-secrets` or environment variables in production
2. **CORS Not Explicitly Configured**: AllowedHosts = "*" in appsettings; CORS policy not visible in Extensions.cs — may allow overly permissive cross-origin requests if not configured at infrastructure level
3. **No Input Validation Snippet Visible**: ValidatorBehavior registered but specific numeric range limits, string length limits, or business rule violations not shown in code review — recommend reviewing CreateOrderCommandValidator, etc. for SQL injection / XSS prevention
4. **Idempotency Key Not Enforced**: x-requestid header validated only for empty GUID; no enforcement of UUID v4 format or collision resistance algorithm
5. **Integration Event Payload Not Encrypted**: RabbitMQ events published in plaintext; sensitive order data (customer ID, payment method) may be exposed on the message bus — consider message encryption at rest/in transit
6. **No Rate Limiting**: No observable rate-limiting behavior in OrdersApi; no IP-based or user-based throttling — risk of abuse or denial-of-service

---

### Observability

**LOGGING_FRAMEWORK**: Microsoft.Extensions.Logging (ILogger<T>)
USES: LoggingBehavior logs command/query entry and exit; IdentityService, OrderingContext may also log
CONFIGURED_IN: `src/Ordering.API/appsettings.json` — Logging:LogLevel
SOURCE: `src/Ordering.API/Application/Behaviors/LoggingBehavior.cs`
LOG_LEVELS:
  - Default: Information
  - Microsoft.AspNetCore: Warning

---

**METRICS**: None observable via built-in logging configuration
RECOMMENDATION: Integrate Application Insights (Azure Monitor) or Prometheus for performance metrics

---

**HEALTH_CHECKS**: MapDefaultEndpoints() in Program.cs likely registers health-check endpoint
ENDPOINT: /health (inferred; typically mapped by service defaults)
SOURCE: `src/Ordering.API/Program.cs` (via MapDefaultEndpoints)
CHECKS: Database connectivity, RabbitMQ connectivity (inferred from event bus registration)

---

**TRACING / DISTRIBUTED TRACING**: Activity/W3C Trace Context likely configured via service defaults
SOURCE: `src/Ordering.API/Extensions/OrderingApiTrace.cs` (if present)
NOTES: Trace context propagated across RabbitMQ event publishing for end-to-end observability

---

### Deployment

DOCKERFILES: Not present in src/Ordering.API; typically at repository root or build/
DOCKER_COMPOSE: Not present in src/Ordering.API; typically at repository root or docker/
KUBERNETES_MANIFESTS: Not present in src/Ordering.API; typically in deploy/ or k8s/
NOTES: Project targets .NET 10.0; containerization handled by Aspire AppHost (eShop.AppHost project); Docker image likely built as part of CI/CD pipeline

---

**Hosting Environment**:
- **Framework**: ASP.NET Core on .NET 10.0
- **Database**: PostgreSQL 12+ (connection pooling disabled per code comment)
- **Message Broker**: RabbitMQ (AMQP)
- **Service Mesh**: None visible (potential enhancement: Dapr or Istio for service-to-service resilience)

---

**Configuration Management**:
- **Secrets**: UserSecretsId 7161b768-033d-41c7-bc5d-37528275e1f3 for development
- **Environment-Specific Settings**: appsettings.json (base) + appsettings.Development.json override
- **Cloud Integration**: Aspire NpgSQL enrichment and service defaults imply cloud-native deployment (e.g., Azure Container Instances, App Service)

---

