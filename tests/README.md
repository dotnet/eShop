# eShop Tests

This directory contains a collection of unit and functional tests for validating the behavior of various components in the eShop application.

**NOTE:** Functional tests default to the Aspire host and spin up test containers, which requires Docker.

`Catalog.FunctionalTests` supports:

- `Aspire`: full Aspire host with PostgreSQL (pgvector).
- `RepositoryMock`: replaces `ICatalogRepository` with a mock-backed in-memory store.
- `EfCoreInMemory`: keeps the real EF repository but swaps the database for EF Core InMemory.
- `Testcontainers`: starts PostgreSQL through Testcontainers instead of Aspire.

`Ordering.FunctionalTests` currently supports:

- `Aspire`: full Aspire host with PostgreSQL and Identity API.
- `RepositoryMock`: replaces repositories, query services, and external services with a mock-backed in-memory store (card types are seeded in the store; no EF database seeding).
- `EfCoreInMemory`: keeps the production repository implementations and swaps the database to EF Core InMemory.
- `Testcontainers`: starts PostgreSQL through Testcontainers instead of Aspire.

### Selecting a mode per test

Annotate a test class or individual test method with `[CatalogFunctionalTestMode(...)]` or `[OrderingFunctionalTestMode(...)]`. Method-level attributes override class-level defaults. Unmarked tests default to `Aspire`.

```csharp
[CatalogFunctionalTestMode(CatalogFunctionalTestMode.RepositoryMock)]
public sealed class CatalogApiTests(CatalogApiTestSession session) { ... }

[Fact]
[OrderingFunctionalTestMode(OrderingFunctionalTestMode.Testcontainers)]
public async Task MyIntegrationTest() { ... }
```

Run only tests for a specific mode with xUnit traits:

```bash
dotnet test tests/Catalog.FunctionalTests --filter-trait FunctionalTestMode=mock
dotnet test tests/Ordering.FunctionalTests --filter-trait FunctionalTestMode=mock
dotnet test tests/Ordering.FunctionalTests --filter-trait FunctionalTestMode=testcontainers
dotnet test tests/Ordering.FunctionalTests --filter-trait FunctionalTestMode=aspire
```

Trait values: `mock`, `inmemory`, `testcontainers`, `aspire`.

When no environment override is set, each test uses the mode from its attribute and fixtures are created lazily per mode.

Setting `ESHOP_CATALOG_FUNCTIONAL_TEST_MODE` or `ESHOP_ORDERING_FUNCTIONAL_TEST_MODE` still overrides the attributed mode for all executed tests and skips tests whose attribute does not match. The repo root `.runsettings` files remain available for Visual Studio Test Explorer.
