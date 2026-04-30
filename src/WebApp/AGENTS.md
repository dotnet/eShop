# WebApp Rules

- This is the Blazor Server frontend. API clients are registered in `Extensions.AddApplicationServices()` using Aspire service-discovery addresses.
- AppHost injects `IdentityUrl` and `CallBackUrl`; authentication/OIDC setup is centralized in `AddAuthenticationServices()`.
- Basket uses the generated gRPC client for `basket-api`; catalog and ordering use typed HTTP clients with API versioning and auth token forwarding.
- Order status UI updates come from RabbitMQ handlers under `Services\OrderStatus\IntegrationEvents` and flow through `OrderStatusNotificationService`.
- Chat/AI is optional: WebApp uses `chatModel` or `OllamaEnabled=true`, then enables function invocation on the chat client.
