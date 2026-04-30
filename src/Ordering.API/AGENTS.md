# Ordering.API Rules

- Endpoints in `Apis\OrdersApi.cs` are thin Minimal API handlers. Keep authorization at the route group (`RequireAuthorization()` in `Program.cs`) and use `[AsParameters] OrderServices` for shared dependencies.
- Commands live in `Application\Commands` as command + handler pairs. Add a FluentValidation validator when command inputs have business rules.
- Mutating endpoints that accept `x-requestid` wrap commands in `IdentifiedCommand<TCommand, bool>` for idempotency; keep that pattern for new mutating order operations.
- MediatR pipeline order is logging -> validation -> transaction. `TransactionBehavior` commits the EF transaction, then publishes pending outbox events via `IOrderingIntegrationEventService`.
- Do not publish RabbitMQ events directly from command handlers; enqueue via the existing integration-event service/outbox pattern.
- Read-side models and Dapper queries live in `Application\Queries`; do not expose domain entities as query DTOs.
- Inbound event subscriptions are centralized in `Extensions.AddEventBusSubscriptions()`.
