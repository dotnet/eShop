# Source Project Rules

- Executable services start with `builder.AddServiceDefaults()`. API projects usually follow `AddApplicationServices()` -> `MapDefaultEndpoints()` -> API route mapping -> `UseDefaultOpenApi()`.
- Register new services and infrastructure in `src\eShop.AppHost\Program.cs`; use `WithReference(...)` and `WaitFor(...)` to model dependencies.
- Service-discovery clients use Aspire names, e.g. `https+http://catalog-api`, `https+http://ordering-api`, and gRPC `http://basket-api`.
- Event contracts are intentionally copied into each participating service's `IntegrationEvents\Events` folder. If an event payload changes, update every local copy and every handler that consumes it.
- Event handlers live in `IntegrationEvents\EventHandling` and must be registered with `AddSubscription<,>()` in that service's `AddApplicationServices()` or `AddEventBusSubscriptions()`.
- Projects that already configure event JSON source generation (`Basket.API`, `OrderProcessor`) need matching `[JsonSerializable]` entries and `ConfigureJsonOptions(...)` when new event types are added.
- EF-backed services use `AddMigration<TContext, TSeed>()` for startup migration/seeding.
- Do not add NuGet versions to `.csproj` files; add or update versions in `Directory.Packages.props`.
