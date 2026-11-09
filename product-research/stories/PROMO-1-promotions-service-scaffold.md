# PROMO-1: [BE - Promotions.API, eShop.AppHost, Identity.API] - Promotions service scaffold

**User Story:**

As a developer, I want a `Promotions.API` service with its own database, event bus connection and auth, running under Aspire, so that promotion data lives in its own bounded context and other teams can build on it from Day 1.

**Inputs:**

- New project `src/Promotions.API` (ASP.NET Core minimal API) that references `eShop.ServiceDefaults`, `EventBusRabbitMQ` and `IntegrationEventLogEF`, and is added to `eShop.slnx`
- AppHost registration, following the existing `AddDotnetProject` pattern:
  ```csharp
  var promotionsDb = postgres.AddDatabase("promotionsdb");

  var promotionsApi = builder.AddDotnetProject("promotions-api", "../Promotions.API")
      .WithReference(rabbitMq).WaitFor(rabbitMq)
      .WithReference(promotionsDb).WaitFor(promotionsDb)
      .WithHttpHealthCheck("/health")
      .WithEnvironment("Identity__Url", identityEndpoint);

  webApp.WithReference(promotionsApi);
  orderingApi.WithReference(promotionsApi);
  ```
- Identity: a new API resource and scope named `promotions`, added to the `webapp` client's allowed scopes
- `appsettings.json`:
  ```json
  {
    "Identity": { "Audience": "promotions" },
    "EventBus": { "SubscriptionClientName": "Promotions" }
  }
  ```

**Outputs:**

- `promotions-api` and `promotionsdb` show as healthy in the Aspire dashboard
- On startup, the EF migration creates the tables `Promotions`, `PromotionTargets`, `Coupons`, `CouponRedemptions` and `IntegrationEventLog`
- On first run, a seeder inserts:
  - 4 sample promotions, one per rule type, plus a coupon-only promotion
  - the coupons `FALL10` and `WELCOME5`
- `GET /api/promotions/active` returns `200 OK` with the seeded active promotions. It allows anonymous access; detailed endpoint behavior is in [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md)
- Endpoints `/health` and `/alive` (from `MapDefaultEndpoints`)

**Acceptance Criteria:**

* **Given** a clean machine with Docker running and the repo checked out
* **When** a developer runs the AppHost
    * **Then** `promotions-api` starts after `eventbus` and `promotionsdb` are ready, with no manual steps
    * **Then** the `promotionsdb` schema is created by migration, and the seed data appears exactly once (re-running doesn't duplicate rows)
    * **Then** `GET /api/promotions/active` returns the seeded active promotions as JSON
* **When** the WebApp signs a user in
    * **Then** the access token includes the `promotions` scope, and Promotions.API accepts it on authenticated endpoints
* **When** an authenticated endpoint is called without a token
    * **Then** it returns `401`

**Technical Notes:**

* **Interaction history:** This story is PROMO-1 from the [promotions & recommendations epic](epic-promotions-and-recommendations.md). The epic's AppHost snippet used `AddProject<Projects.Promotions_API>`; this repo actually uses `builder.AddDotnetProject(name, path)`, so the snippet above follows the real pattern. We agreed promotions get their own service (not a module inside Catalog.API or Basket.API) so that teams can work in parallel during the workshop.
* **Existing code to reuse:**
    * `src/Webhooks.API/Program.cs:1-24` and `src/Webhooks.API/Extensions/Extensions.cs:3-24`: the smallest existing template with service defaults, auth, RabbitMQ, Npgsql, `AddMigration` and event subscriptions all in one place. Copy its structure.
    * `src/eShop.AppHost/AppHost.cs:17-20`: where the databases are declared.
    * `src/eShop.AppHost/AppHost.cs:42-46,56-59`: project registrations to copy, with health check and `Identity__Url`.
    * `src/eShop.AppHost/AppHost.cs:71-79`: the `webApp` references.
    * `src/Shared/MigrateDbContextExtensions.cs:25-27`: `AddMigration<TContext, TDbSeeder>()` with `IDbSeeder<TContext>`. The seeder pattern is in `src/Catalog.API/Infrastructure/CatalogContextSeed.cs:11`, and it's registered at `src/Catalog.API/Extensions/Extensions.cs:24`.
    * `src/Identity.API/Configuration/Config.cs:10-12,22-24`: where `ApiResource` and `ApiScope` are added.
    * `src/Identity.API/Configuration/Config.cs:78`: the `webapp` client, whose `AllowedScopes` gets `promotions`.
    * `src/eShop.ServiceDefaults`: `AddDefaultAuthentication()` reads `Identity:Audience`.
* **New code (proposal):**
    * a `PromotionsContext` (EF Core) and a `PromotionsContextSeed`
    * the `Promotions.API` project files
    * an `Apis/PromotionsApi.cs` stub that maps the `/api/promotions` group
    * The entity shapes come from [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md) and [PROMO-4](PROMO-4-coupon-codes-and-redemption.md). This story only needs enough of them for the migration to run.

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocks:** [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md), [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md), [PROMO-4](PROMO-4-coupon-codes-and-redemption.md)

**Open Questions:**

* Should Promotions.API be added to the `mobile-bff` YARP routes (`src/eShop.AppHost/AppHost.cs:62-64`) now, or left out until a mobile client needs it?
* Should the Identity `promotions` scope also go to the `maui`/mobile clients, or only to `webapp`?
