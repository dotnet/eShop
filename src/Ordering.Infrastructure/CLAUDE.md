# Ordering.Infrastructure

## Purpose
Infrastructure layer implementing data access and external service integrations for the Ordering bounded context.

## Key Components
- **OrderingContext**: EF Core DbContext
- **Repositories**: Order and Buyer repositories
- **Entity Configurations**: Fluent API configurations

## Database
- PostgreSQL (`orderingdb`)
- EF Core migrations
- Fluent API for entity mapping

## Repository Pattern
- `IOrderRepository` - Order aggregate persistence
- `IBuyerRepository` - Buyer aggregate persistence
- Unit of Work via DbContext

## Key Files
- `EntityConfigurations/` - EF Core entity configurations
- `Repositories/` - Repository implementations
- `OrderingContext.cs` - Main DbContext
