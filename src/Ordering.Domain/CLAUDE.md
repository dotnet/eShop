# Ordering.Domain

## Overview
The Ordering.Domain project contains the domain model for the ordering bounded context, following Domain-Driven Design (DDD) principles. It defines aggregates, entities, value objects, and domain events.

## Architecture Pattern
- **Domain-Driven Design (DDD)**
- **Aggregate Root Pattern**
- **Domain Events**
- **Value Objects**

## Key Components

### Aggregates

#### Order Aggregate
The main aggregate root for order management.
- **Root**: `Order`
- **Entities**: `OrderItem`
- **Value Objects**: `Address`

#### Buyer Aggregate
Represents the customer placing orders.
- **Root**: `Buyer`
- **Entities**: `PaymentMethod`

### Entities
| Entity | Description |
|--------|-------------|
| `Order` | Main order entity with status, items, address |
| `OrderItem` | Line item in an order |
| `Buyer` | Customer who places orders |
| `PaymentMethod` | Customer payment method |

### Value Objects
| Value Object | Description |
|--------------|-------------|
| `Address` | Shipping address (street, city, state, country, zip) |
| `OrderStatus` | Enumeration of order states |
| `CardType` | Payment card type enumeration |

### Domain Events
Domain events are raised within aggregates and handled in-process by MediatR.

| Event | Raised When |
|-------|-------------|
| `OrderStartedDomainEvent` | Order is created |
| `OrderStatusChangedToAwaitingValidationDomainEvent` | Status changes to awaiting validation |
| `OrderStatusChangedToStockConfirmedDomainEvent` | Stock is confirmed |
| `OrderStatusChangedToPaidDomainEvent` | Payment succeeds |
| `OrderShippedDomainEvent` | Order is shipped |
| `OrderCancelledDomainEvent` | Order is cancelled |
| `BuyerPaymentMethodVerifiedDomainEvent` | Payment method is verified |

### Exceptions
- `OrderingDomainException` - Base exception for domain rule violations

## Project Structure
```
Ordering.Domain/
├── AggregatesModel/
│   ├── BuyerAggregate/
│   │   ├── Buyer.cs           # Aggregate root
│   │   ├── PaymentMethod.cs   # Entity
│   │   ├── CardType.cs        # Value object
│   │   └── IBuyerRepository.cs
│   └── OrderAggregate/
│       ├── Order.cs           # Aggregate root
│       ├── OrderItem.cs       # Entity
│       ├── Address.cs         # Value object
│       ├── OrderStatus.cs     # Enumeration
│       └── IOrderRepository.cs
├── Events/                    # Domain events
├── Exceptions/                # Domain exceptions
└── SeedWork/                  # Base classes
    ├── Entity.cs
    ├── IAggregateRoot.cs
    ├── IRepository.cs
    ├── IUnitOfWork.cs
    └── ValueObject.cs
```

## Design Principles

### Aggregate Invariants
- Order items cannot have negative quantities
- Order total is calculated from items
- Status transitions follow defined state machine
- Address is immutable value object

### Domain Event Pattern
```csharp
// Raising domain events
public class Order : Entity, IAggregateRoot
{
    public Order(...)
    {
        AddDomainEvent(new OrderStartedDomainEvent(...));
    }

    public void SetAwaitingValidationStatus()
    {
        // Validate transition
        AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(...));
    }
}
```

### Repository Pattern
Each aggregate has a repository interface defined in the domain:
- `IOrderRepository`
- `IBuyerRepository`

Implementations are in `Ordering.Infrastructure`.

## Dependencies
- No external dependencies (pure domain model)
- Referenced by: `Ordering.API`, `Ordering.Infrastructure`

## Usage
This project is a class library containing the domain model. It should have no dependencies on infrastructure concerns (databases, messaging, etc.).
