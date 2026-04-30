# Ordering.Domain Rules

- Keep behavior inside aggregates. State changes should go through aggregate methods, not public setters or external mutation.
- `Order` owns its item collection through a private `List<OrderItem>` and exposes `IReadOnlyCollection<OrderItem>`; preserve that encapsulation pattern for aggregate collections.
- Domain entities raise MediatR `INotification` domain events through `AddDomainEvent(...)`. Mapping domain events to integration events belongs in `Ordering.API`, not the domain project.
- EF needs protected/private constructors and private setters; do not widen visibility just to make tests easier.
- Invalid status transitions should fail through the existing domain exception pattern instead of silently no-oping.
