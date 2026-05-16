# eShop Shared Utilities

Cross-cutting, reusable extensions for observability (OpenTelemetry tracing and exception handling) and database migration operations with seeding support.

## Summary

- **Purpose**: Provides shared utility extensions for ASP.NET Core applications within the eShop microservices ecosystem.
- **Primary Technologies**: .NET, Entity Framework Core, OpenTelemetry, ASP.NET Core DI container.
- **Major Components**: OpenTelemetry exception tagging utility; database migration and seeding DI container extensions; hosted service for background database operations.
- **Scope**: Internal utilities consumed by service startup wiring (AppHost, individual APIs).
- **Architecture Pattern**: Extension methods pattern for seamless integration into existing DI pipelines.

## Projects and Folder Map

| PATH | PURPOSE | ENTRY_FILES |
|---|---|---|
| `src/Shared/` | Shared extension utilities for logging, tracing, and database operations | `ActivityExtensions.cs`<br/>`MigrateDbContextExtensions.cs` |

## Components

COMPONENT_NAME: ActivityExtensions
TYPE: Utility
PURPOSE: Extends System.Diagnostics.Activity to tag exceptions with OpenTelemetry semantic conventions for distributed tracing.
RESPONSIBILITIES:
  - Adds exception message, stacktrace, and type as activity tags
  - Sets activity status to Error when an exception occurs
  - Implements OpenTelemetry exception semantic conventions
SOURCE: `src/Shared/ActivityExtensions.cs`
CALLS:
  - NONE
CALLED_BY:
  - MigrateDbContextExtensions (MigrationHostedService)
  - Any service using System.Diagnostics.Activity for exception tracking

---

COMPONENT_NAME: MigrateDbContextExtensions
TYPE: Service
PURPOSE: Provides extension methods for IServiceCollection to register and execute database migrations with optional seeding at application startup.
RESPONSIBILITIES:
  - Register AddMigration<TContext>() extension methods on IServiceCollection
  - Configure OpenTelemetry ActivitySource for migration tracing
  - Register MigrationHostedService<TContext> as a background service
  - Execute database migrations asynchronously during application startup
  - Execute seeding logic after migrations complete
  - Implement resilient execution strategies for database operations
  - Log migration status and errors
SOURCE: `src/Shared/MigrateDbContextExtensions.cs`
CALLS:
  - ActivityExtensions — to tag exceptions during migration and seeding operations
  - OpenTelemetry ActivitySource — to create distributed traces for migration operations
  - DbContext.Database.MigrateAsync() — to apply Entity Framework Core migrations
  - IDbSeeder<TContext>.SeedAsync() — to execute custom seeding logic
CALLED_BY:
  - eShop.AppHost and individual service APIs (via IServiceCollection extension at startup)

---

COMPONENT_NAME: IDbSeeder
TYPE: Abstraction
PURPOSE: Defines a contract for custom database seeding logic that runs after migrations complete.
RESPONSIBILITIES:
  - Define async SeedAsync() method for implementations to provide database seeding logic
SOURCE: `src/Shared/MigrateDbContextExtensions.cs`
CALLS:
  - NONE
CALLED_BY:
  - MigrationHostedService<TContext> (via AddMigration<TContext, TDbSeeder> overload)

---

COMPONENT_NAME: MigrationHostedService
TYPE: Worker
PURPOSE: Background service that initiates database migrations and seeding during application startup.
RESPONSIBILITIES:
  - Implement BackgroundService lifecycle for application startup hooks
  - Create DI service scope for migration operations
  - Execute database migrations within an execution strategy
  - Invoke attached seeder logic after successful migrations
  - Capture and log errors during migration and seeding
  - Tag exceptions with OpenTelemetry metadata
SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (nested private class)
CALLS:
  - ActivityExtensions — to tag exceptions with OpenTelemetry conventions
  - DbContext.Database.CreateExecutionStrategy() — to get resilient execution strategy
  - DbContext.Database.MigrateAsync() — to apply schema changes
  - Seeder function — to run custom seeding logic (injected as delegate)
CALLED_BY:
  - ASP.NET Core Host (via IHostedService)
  - IServiceCollection.AddHostedService() registration

## Component Call Sequences

### Use-Case: Application Startup with Database Migration and Seeding

STEP 1: eShop.AppHost / Identity.API / Ordering.API → MigrateDbContextExtensions
  OPERATION: AddMigration<TContext>() or AddMigration<TContext, TDbSeeder>()
  PURPOSE: Register migration and seeding configuration during startup dependency injection setup
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (lines 8-13, 15-24)

STEP 2: ASP.NET Core Host → MigrationHostedService
  OPERATION: StartAsync()
  PURPOSE: Invoke background service at application startup
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (line 85)

STEP 3: MigrationHostedService → MigrateDbContextAsync<TContext>()
  OPERATION: MigrateDbContextAsync(services, seeder)
  PURPOSE: Orchestrate migration execution within a service scope
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (line 86)

STEP 4: MigrateDbContextAsync → ActivitySource.StartActivity()
  OPERATION: StartActivity($"Migration operation {DbContextName}")
  PURPOSE: Begin distributed trace for migration operation
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (line 33)

STEP 5: InvokeSeeder → DbContext.Database.MigrateAsync()
  OPERATION: Apply Entity Framework Core migrations
  PURPOSE: Execute pending database schema migrations
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (line 54)

STEP 6: InvokeSeeder → Seeder Function
  OPERATION: seeder(context, services)
  PURPOSE: Invoke custom database seeding logic (e.g., seed initial data)
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (line 55)

STEP 7: MigrationHostedService → ActivityExtensions.SetExceptionTags() (on error)
  OPERATION: SetExceptionTags(activity, exception)
  PURPOSE: Tag activity with exception metadata if migration or seeding fails
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (line 64)

### Use-Case: Custom Database Seeding during Startup

STEP 1: Service API → AddMigration<TContext, TDbSeeder>()
  OPERATION: Register sealed seeder implementation for a specific DbContext
  PURPOSE: Wire custom seeder into migration pipeline
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (lines 25-28)

STEP 2: MigrationHostedService → IDbSeeder<TContext>.SeedAsync()
  OPERATION: SeedAsync(context)
  PURPOSE: Execute custom seeding logic injected via IDbSeeder implementation
  SOURCE: `src/Shared/MigrateDbContextExtensions.cs` (line 27)

## Communication Channels

CHANNEL_TYPE: Internal (no external channels)
NOTES: All invocations are internal to the application process, triggered by ASP.NET Core host startup. No HTTP, gRPC, or message queue communication.

## Dependency Registration and Wiring

DI_CONTAINER: Microsoft.Extensions.DependencyInjection (built-in ASP.NET Core DI)

### Registration 1: AddMigration<TContext> (no-op seeder)
REGISTRATION_FILE: `src/Shared/MigrateDbContextExtensions.cs`, lines 10-12
LIFETIME: Transient (HostedService registration)
ABSTRACTION: IHostedService
IMPLEMENTATION: MigrationHostedService<TContext>
SNIPPET:
```csharp
public static IServiceCollection AddMigration<TContext>(
    this IServiceCollection services) where TContext : DbContext
  => services.AddMigration<TContext>((_, _) => Task.CompletedTask);
```

### Registration 2: AddMigration<TContext> with seeder delegate
REGISTRATION_FILE: `src/Shared/MigrateDbContextExtensions.cs`, lines 14-23
LIFETIME: Transient (HostedService registration)
ABSTRACTION: IHostedService
IMPLEMENTATION: MigrationHostedService<TContext>
SNIPPET:
```csharp
services.AddOpenTelemetry().WithTracing(
  tracing => tracing.AddSource(ActivitySourceName));
return services.AddHostedService(
  sp => new MigrationHostedService<TContext>(sp, seeder));
```

### Registration 3: AddMigration<TContext, TDbSeeder> (typed seeder)
REGISTRATION_FILE: `src/Shared/MigrateDbContextExtensions.cs`, lines 25-28
LIFETIME: Scoped (IDbSeeder), Transient (HostedService)
ABSTRACTION: IDbSeeder<TContext>, IHostedService
IMPLEMENTATION: TDbSeeder (generic), MigrationHostedService<TContext>
SNIPPET:
```csharp
services.AddScoped<IDbSeeder<TContext>, TDbSeeder>();
return services.AddMigration<TContext>(
  (context, sp) => sp.GetRequiredService<IDbSeeder<TContext>>()
    .SeedAsync(context));
```

## Configuration and Secrets

| SOURCE_TYPE | KEYS | SENSITIVE | LOCATION |
|---|---|---|---|
| Implicit (OpenTelemetry) | ActivitySourceName = "DbMigrations" | NO | `src/Shared/MigrateDbContextExtensions.cs` (line 8) |

No configuration files, environment variables, or secrets are used. All configuration is semantic (activity tracing metadata).

## Persistence and Data Access

DATABASE: Context-agnostic (injected DbContext type parameter)
DATA_ACCESS: Entity Framework Core (DbContext.Database.MigrateAsync)
MIGRATIONS_PATH: Determined by DbContext implementation in consuming service (not managed by Shared)
REPOSITORY_PATTERN: NO

Note: Shared utilities do not specify database engine or location. Consuming services (Ordering.API, Catalog.API, etc.) provide DbContext implementations and migration folders.

## Patterns and Architecture Notes

| PATTERN | EVIDENCE | SNIPPET |
|---|---|---|
| **Extension Methods (Vertical Slice)** | IServiceCollection and Activity extended with custom methods | AddMigration<TContext>() as extension method |
| **Generic Type Parameters** | DbContext abstraction via generic TContext and TDbSeeder | `AddMigration<TContext, TDbSeeder>()` |
| **Dependency Injection (DI Container Pattern)** | Services registered with AddHostedService, AddScoped, AddOpenTelemetry | Registration file: `src/Shared/MigrateDbContextExtensions.cs`, lines 14-23 |
| **Background Service (Hosted Service)** | MigrationHostedService<TContext> implements BackgroundService | Runs migrations during application startup |
| **Resilient Execution (Polly Pattern)** | DbContext.Database.CreateExecutionStrategy() | Retry and circuit-breaker support for migrations |
| **Distributed Tracing (OpenTelemetry)** | ActivitySource created and traces propagated | Line 9: `new ActivitySource(ActivitySourceName)` |
| **Semantic Tagging (OpenTelemetry Conventions)** | Exception tags follow OTEL semantic conventions | `activity.AddTag("exception.message", ex.Message)` |
| **Template Method** | Seeder function as delegate parameter for custom logic injection | `Func<TContext, IServiceProvider, Task> seeder` |
| **Strategy Pattern** | Execution strategy abstraction for database resilience | `context.Database.CreateExecutionStrategy()` |

SNIPPET: Generic seeder registration enabling custom implementations:
```csharp
public static IServiceCollection AddMigration<TContext, TDbSeeder>(
    this IServiceCollection services)
    where TContext : DbContext
    where TDbSeeder : class, IDbSeeder<TContext>
{
    services.AddScoped<IDbSeeder<TContext>, TDbSeeder>();
    return services.AddMigration<TContext>(
        (context, sp) => sp.GetRequiredService<IDbSeeder<TContext>>()
            .SeedAsync(context));
}
```

## Security and Operational Considerations

AUTHN_AUTHZ: Not applicable (internal utilities, no external API boundary)

OBSERVABILITY:
  - Logging Framework: Microsoft.Extensions.Logging
    - Migration start/end logged at Information level
    - Errors logged at Error level with exception details
    - Configuration: `src/Shared/MigrateDbContextExtensions.cs`, lines 31, 37
  - Distributed Tracing: OpenTelemetry ActivitySource
    - Activity source created: "DbMigrations" (`src/Shared/MigrateDbContextExtensions.cs`, line 8)
    - Activities started for overall migration and per-operation (`src/Shared/MigrateDbContextExtensions.cs`, lines 33, 50)
    - Exception tags follow OTEL semantic conventions (`src/Shared/ActivityExtensions.cs`, lines 9-12)
  - Metrics: Not implemented in Shared (handled by consuming services)

DEPLOYMENT:
  - No Dockerfile or deployment configuration in Shared
  - Shared is compiled into consuming service projects (Identity.API, Ordering.API, etc.)
  - Referenced in their .csproj files as direct project references

KNOWN_RISKS:
  - NONE: Utilities are internal, no exposed secrets or credentials
  - Execution strategy resilience depends on DbContext provider implementation; Shared does not enforce retry policy configuration
