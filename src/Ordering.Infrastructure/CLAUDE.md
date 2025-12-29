# Ordering.Infrastructure

## Overview
The Ordering.Infrastructure project provides the data access layer for the Ordering bounded context. It implements repository interfaces defined in `Ordering.Domain` using Entity Framework Core.

## Technology Stack
- **ORM**: Entity Framework Core 10
- **Database**: PostgreSQL
- **Pattern**: Repository + Unit of Work

## Key Components

### DbContext
- `OrderingContext` - EF Core context for orders and buyers

### Repositories
| Repository | Interface | Description |
|------------|-----------|-------------|
| `OrderRepository` | `IOrderRepository` | CRUD operations for Order aggregate |
| `BuyerRepository` | `IBuyerRepository` | CRUD operations for Buyer aggregate |

### Entity Configurations
EF Core entity type configurations for:
- `Order` - Order entity mapping
- `OrderItem` - Order item mapping
- `Buyer` - Buyer entity mapping
- `PaymentMethod` - Payment method mapping
- `Address` - Value object mapping (owned entity)

### Mediator Extension
- `MediatorExtension` - Dispatches domain events after saving changes

## Project Structure
```
Ordering.Infrastructure/
├── EntityConfigurations/     # EF Core type configurations
│   ├── OrderEntityTypeConfiguration.cs
│   ├── OrderItemEntityTypeConfiguration.cs
│   ├── BuyerEntityTypeConfiguration.cs
│   └── PaymentMethodEntityTypeConfiguration.cs
├── Repositories/             # Repository implementations
│   ├── OrderRepository.cs
│   └── BuyerRepository.cs
├── MediatorExtension.cs      # Domain event dispatcher
└── OrderingContext.cs        # EF Core DbContext
```

## Database Schema

### Tables
| Table | Description |
|-------|-------------|
| `orders` | Order header information |
| `orderItems` | Order line items |
| `buyers` | Customer information |
| `paymentmethods` | Saved payment methods |
| `cardtypes` | Payment card type lookup |
| `orderstatus` | Order status lookup |

### Key Relationships
- `Order` → `OrderItem` (1:N)
- `Order` → `Buyer` (N:1)
- `Buyer` → `PaymentMethod` (1:N)
- `Order` → `Address` (1:1, owned entity)

## Unit of Work Pattern
```csharp
public class OrderingContext : DbContext, IUnitOfWork
{
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await _mediator.DispatchDomainEventsAsync(this);

        // Save changes
        await base.SaveChangesAsync(cancellationToken);

        return true;
    }
}
```

## Domain Event Dispatching
Domain events are collected from aggregate roots and dispatched via MediatR after changes are saved:
```csharp
public static async Task DispatchDomainEventsAsync(this IMediator mediator, OrderingContext ctx)
{
    var domainEntities = ctx.ChangeTracker
        .Entries<Entity>()
        .Where(x => x.Entity.DomainEvents?.Any() == true);

    var domainEvents = domainEntities
        .SelectMany(x => x.Entity.DomainEvents)
        .ToList();

    domainEntities.ToList().ForEach(entity => entity.Entity.ClearDomainEvents());

    foreach (var domainEvent in domainEvents)
        await mediator.Publish(domainEvent);
}
```

## Migrations
Database migrations are located in the `Migrations/` folder. Run migrations using:
```bash
dotnet ef database update --project src/Ordering.Infrastructure
```

## Dependencies
- **Ordering.Domain**: Domain models and interfaces
- **Entity Framework Core**: ORM
- **MediatR**: Domain event dispatching
- **Npgsql.EntityFrameworkCore.PostgreSQL**: PostgreSQL provider

## Configuration
Connection string is configured via .NET Aspire service discovery in the host application.
