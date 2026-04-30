# Basket.API Rules

- Basket is gRPC-first: service implementation is in `Grpc\BasketService.cs`, contract in `Proto`, and WebApp consumes the generated `Basket.BasketClient`.
- Basket state is persisted through `IBasketRepository`/`RedisBasketRepository`; keep Redis access behind that repository.
- Basket consumes `OrderStartedIntegrationEvent` to clear baskets after checkout. Update the subscription and `IntegrationEventContext` JSON source generation when adding consumed event types.
- Authentication is added in `AddApplicationServices()` with `AddDefaultAuthentication()`; keep auth concerns out of the gRPC service methods unless a method needs user-specific behavior.
