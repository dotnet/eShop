# Catalog.API Rules

- Endpoints live in `Apis\CatalogApi.cs` as versioned Minimal API route groups. New routes should include route name, summary, description, and tags like the existing endpoints.
- Keep `builder.Environment.IsBuild()` handling in `Extensions.AddApplicationServices()`; build-time OpenAPI generation must not require database configuration or migrations.
- Catalog uses PostgreSQL with pgvector. Embeddings are enabled by either `OllamaEnabled=true` or a `textEmbeddingModel` connection string; AI work should go through `ICatalogAI`.
- Catalog consumes order-status events to validate stock and publishes stock/price events through `CatalogIntegrationEventService`.
- When a catalog data change publishes an event, use `SaveEventAndCatalogContextChangesAsync(...)` so the catalog update and outbox entry stay atomic.
