# AppHost Rules

- Resource names are part of service configuration: `redis`, `eventbus`, `postgres`; databases `catalogdb`, `identitydb`, `orderingdb`; projects `identity-api`, `basket-api`, `catalog-api`, `ordering-api`, `order-processor`, `payment-processor`, `webapp`.
- When adding a dependency, wire both sides: AppHost `WithReference(...)`/`WaitFor(...)` and the target service's matching connection/client registration.
- Workers can depend on APIs that own migrations; `OrderProcessor` waits for `ordering-api` because Ordering applies EF migrations.
- `ESHOP_USE_HTTP_ENDPOINTS=1` forces HTTP endpoints for tests/CI through `ShouldUseHttpForEndpoints()`.
- `AddForwardedHeaders()` injects `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` into all project resources.
- AI is opt-in through local booleans in `Program.cs`: `AddOpenAI(...)` injects `textEmbeddingModel` into Catalog and `chatModel` into WebApp; `AddOllama(...)` injects `embedding` and `chat` plus `OllamaEnabled=true`.
