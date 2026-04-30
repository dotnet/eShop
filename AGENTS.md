# eShop - Agent Index

Use this file for repo-wide context. Read the nearest scoped `AGENTS.md` before changing code in that area:

- `src\AGENTS.md` - shared service, AppHost, event bus, package rules
- `src\eShop.AppHost\AGENTS.md` - Aspire orchestration, resource names, AI toggles
- `src\Ordering.API\AGENTS.md` and `src\Ordering.Domain\AGENTS.md` - Ordering CQRS/DDD flow
- `src\Catalog.API\AGENTS.md`, `src\Basket.API\AGENTS.md`, `src\WebApp\AGENTS.md` - service-specific rules
- `tests\AGENTS.md` and `e2e\AGENTS.md` - test harness specifics

## Verified command gotchas

- Run the app from the repo root: `dotnet run --project src\eShop.AppHost\eShop.AppHost.csproj` (requires Docker).
- This repo uses Microsoft.Testing.Platform. Use `dotnet test --project <path-to-csproj>` or `--solution eShop.slnx`; bare directory paths such as `dotnet test tests\Ordering.UnitTests` fail.
- Fast unit-test validation:
  - `dotnet test --project tests\Ordering.UnitTests\Ordering.UnitTests.csproj`
  - `dotnet test --project tests\Basket.UnitTests\Basket.UnitTests.csproj`
  - Single test: `dotnet test --project tests\Ordering.UnitTests\Ordering.UnitTests.csproj --filter "FullyQualifiedName~CreateOrderCommandHandlerTest"`
- Functional tests spin up Aspire resources/containers; check Docker/container startup before treating a slow full-solution test run as a code failure.
- Playwright defaults to `http://localhost:5045`; logged-in tests require `USERNAME1` and `PASSWORD`.

## Repo-wide facts

- `src\eShop.AppHost` wires Redis, RabbitMQ, PostgreSQL/pgvector, APIs, workers, and the Blazor frontend.
- Central NuGet versions live in `Directory.Packages.props`; do not add versions in project files.
- `TreatWarningsAsErrors` and `UseArtifactsOutput` are enabled globally.
- Services communicate through Aspire service discovery (HTTP/gRPC) and RabbitMQ integration events; EF-backed services use an outbox (`IntegrationEventLogEF`) when data changes publish events.
