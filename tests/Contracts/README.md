# Cross-runtime compatibility contracts

These fixtures are the migration boundary between the existing .NET services
and their Java replacements. A Java service is not eligible for cutover until
its automated tests prove compatibility with the applicable contracts.

## Sources of truth

- Catalog HTTP v1: `src/Catalog.API/Catalog.API.json`
- Catalog HTTP v2: `src/Catalog.API/Catalog.API_v2.json`
- Basket gRPC: `src/Basket.API/Proto/basket.proto`
- Ordering HTTP v1: `src/Ordering.API/Apis/OrdersApi.cs`
- Integration events: `src/**/IntegrationEvents/**/*.cs`
- RabbitMQ transport: `src/EventBusRabbitMQ/RabbitMQEventBus.cs`
- Basket Redis model: `src/Basket.API/Model/`
- JWT validation: `src/eShop.ServiceDefaults/AuthenticationExtensions.cs`
- Health endpoints: `src/eShop.ServiceDefaults/Extensions.cs`

## Fixed interoperability rules

1. RabbitMQ uses the durable direct exchange `eshop_event_bus`.
2. A routing key is the exact PascalCase integration-event type name.
3. Integration-event JSON uses PascalCase properties, UUID `Id`, and UTC
   `CreationDate`. Consumers tolerate unknown properties.
4. Messages are persistent, publishing is mandatory, and consumers acknowledge
   manually.
5. Basket values are JSON strings under `/basket/{sub}` and use PascalCase
   properties.
6. APIs use the Duende issuer and the `sub` claim. Existing .NET APIs do not
   validate the JWT audience; scopes remain the authorization boundary.
7. `/health` is readiness and `/alive` is liveness.
8. Existing API paths, response JSON, `api-version`, and `x-requestid` remain
   unchanged.

## Per-service acceptance gate

Before replacing a .NET service:

1. Unit tests and Java integration tests pass.
2. OpenAPI or protobuf compatibility checks pass.
3. Database/Redis compatibility tests pass against real containers.
4. Mixed .NET/Java RabbitMQ publish-consume tests pass.
5. Existing UI smoke tests pass.
6. Rollback to the .NET registration is rehearsed.
7. Only one state-changing consumer owns each service queue during cutover.
