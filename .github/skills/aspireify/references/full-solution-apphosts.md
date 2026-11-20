# Full-solution C# AppHosts

Use this reference when `aspire init` created a **full project mode** AppHost because a `.sln` or `.slnx` was discovered.

This is the high-friction path: solution-backed repos often have older bootstrap patterns, SDK pins, existing ServiceDefaults-like code, build constraints, and significantly more projects than single-file repos. Some of these solutions have dozens or hundreds of projects — the skill must triage smartly, not try to wire everything.

## What this reference is for

Load this reference when any of the following are true:

- `appHost.path` points to a directory containing `apphost.cs` and a `.csproj`
- a `.sln` or `.slnx` exists near the AppHost
- the repo has a root `global.json`
- selected .NET services still use `Program.cs` + `Startup.cs`, `Host.CreateDefaultBuilder`, `ConfigureWebHostDefaults`, `UseStartup`, or other `IHostBuilder` patterns

## Core rule: solution-backed AppHosts are not single-file AppHosts

Treat these repos as **solution-aware C# init**, not as generic AppHost setup.

- The AppHost may need project references
- The AppHost may need its own SDK boundary
- The solution may or may not be able to own the AppHost safely
- ServiceDefaults changes may require bootstrap modernization

Do not apply single-file assumptions here.

## Large solution triage

When a solution contains more than a handful of projects, don't try to model everything at once. Classify projects first, then present a focused list.

### Step 1: Classify all projects

For every `.csproj` in the solution, determine its role:

| Classification | How to detect | Action |
|---------------|---------------|--------|
| **Runnable service** | `OutputType` = `Exe` or `WinExe`, not a test project, not the AppHost | Candidate for AppHost modeling |
| **Class library** | `OutputType` = `Library` | Skip — these are dependencies, not services |
| **Test project** | References xUnit/NUnit/MSTest, or name ends in `.Test`/`.Tests`/`.IntegrationTest` | Skip |
| **Migration runner** | Name contains `Migrat`, or references `DbUp`/`FluentMigrator`/EF migrations tooling | Special handling — see below |
| **Utility/tool** | Located in `util/`, `tools/`, or `scripts/` directories; not a long-running service | Skip unless user requests |
| **AppHost** | `IsAspireHost` = `true` | Skip — this is the host itself |

Run `dotnet msbuild <project> -getProperty:OutputType` to classify. For large solutions, batch these calls.

### Step 2: Check for multiple source roots

Some repos keep code in more than one top-level directory. Common patterns:

- `src/` + `bitwarden_license/src/` (open-source vs commercial)
- `src/` + `util/` (services vs utilities)
- `apps/` + `packages/` (monorepo with shared packages)

Scan all directories in the solution, not just `src/`. If you find projects outside `src/`, note them and ask the user if they should be included.

### Step 3: Present the focused list

Group runnable services by category and present them concisely. For a repo with 60+ projects, show something like:

> *"I found 8 runnable web services (Api, Admin, Identity, Billing, Events, EventsProcessor, Icons, Notifications), 5 class libraries (Core, SharedWeb, Infrastructure.Dapper, Infrastructure.EntityFramework, Sql), 3 migration runners (Migrator, MsSqlMigratorUtility, PostgresMigrations), and 40+ test projects.*
>
> *I recommend starting with the core services. Which of the 8 web services should I include in the AppHost?"*

**Do not dump a flat list of 60+ projects.** Classify, summarize, and let the user choose.

### Incremental "core loop" wiring

For solutions with 5 or more runnable services, recommend starting with a **core loop** — the minimum set of services needed for a useful local dev session — and expanding from there.

The core loop is typically:

1. The primary API service
2. Its database dependency
3. Any authentication/identity service
4. The essential cache (Redis, etc.)

Present this explicitly:

> *"You have 8 services. I recommend wiring the core loop first — Api, Identity, and the database — so we can validate `aspire start` works. Then we'll add the remaining services. Sound good?"*

**After the core loop succeeds with `aspire start`:**

1. Stop the AppHost
2. Add the next batch of services (2-3 at a time) to the AppHost
3. Run `aspire start` again to validate
4. If it fails, diagnose and fix before adding more
5. Repeat until all selected services are wired

Present progress to the user as you go:

> *"Core loop is working (Api + Identity + Postgres + Redis). Adding the next batch: Admin, Billing, and Events..."*

Do not consider the skill complete until all services the user selected in Step 3 are wired and `aspire start` runs with all of them healthy. The core loop is a risk-reduction strategy, not an excuse to stop early.

## Migration runners and setup utilities

Migration runners (database migrations, schema updates, data seeders) deserve special handling. They aren't long-running services — they run once and exit.

Present the user with options:

1. **Model as a project resource with `WaitForCompletion()`** — the migration runs at startup and dependent services wait for it to finish before starting:

```csharp
var db = builder.AddSqlServer("mssql").AddDatabase("vault");
var migrator = builder.AddProject<Projects.Migrator>("migrator")
    .WithReference(db)
    .WaitFor(db);

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(db)
    .WaitForCompletion(migrator);  // api waits for migrations to finish
```

2. **Leave as manual** — the developer runs migrations separately before `aspire start`. Note this in an AppHost comment:

```csharp
// Run migrations manually: dotnet run --project ../util/Migrator
var db = builder.AddSqlServer("mssql").AddDatabase("vault");
```

Recommend option 1 for repos that currently run migrations as part of their docker-compose or startup scripts. Recommend option 2 for repos where migrations are a deliberate, explicit step.

## Custom MSBuild SDKs

Check `global.json` for `msbuild-sdks` entries beyond the standard Microsoft ones. Common SDKs like `Microsoft.Build.Traversal` are well-known, but custom SDKs (e.g., `Bitwarden.Server.Sdk`) are opaque.

When you find a custom MSBuild SDK:

- **Don't assume project properties are reliable** — the custom SDK may override `OutputType`, inject implicit references, or modify build behavior in ways you can't see
- **Note it to the user** — *"This repo uses a custom MSBuild SDK (Bitwarden.Server.Sdk). I'll classify projects based on their directory structure and names as well as MSBuild properties, since the custom SDK may affect property evaluation."*
- **Cross-reference with directory structure** — if `OutputType` says `Library` but the project is in `src/Api/` and has a `Program.cs` and `Startup.cs`, it's likely a runnable service whose OutputType is set by the custom SDK

## Conditional compilation

Some repos use `#if` / `#endif` to maintain multiple build variants from the same source:

```csharp
#if OSS
    services.AddOosServices();
#else
    services.AddCommercialCoreServices();
#endif
```

When you detect conditional compilation in `Program.cs` or `Startup.cs`:

1. **Surface it early** — *"Your services use `#if OSS` conditional compilation, which means the app behaves differently depending on the build configuration. Which variant should the AppHost target — OSS or commercial?"*
2. **Don't try to model both** — pick the variant the user selects and wire accordingly
3. **Note the other variant** — leave a comment in the AppHost: `// This AppHost targets the OSS build. For commercial, adjust service registrations.`

This is not a priority to solve perfectly — just make sure the agent doesn't silently pick the wrong variant.

## Mixed SDK repos

Some repos pin the root `global.json` to an older SDK such as .NET 8. A `.csproj`-based Aspire AppHost should still stay on the current Aspire-supported SDK (for example, .NET 10), while existing service projects can remain on `net8.0`.

**Do not downgrade the AppHost project to match the repo's root SDK pin. Do not change the root `global.json`. Do not change any existing project's `<TargetFramework>`.**

### Create a nested `global.json` for the AppHost

If the repo's root `global.json` pins an older SDK and the AppHost is in full project mode, you **must** create a nested `global.json` inside the AppHost directory so it builds with the correct SDK. Check whether one already exists before creating it.

Steps:

1. Keep the repo root `global.json` unchanged.
2. Check if a `global.json` already exists in the AppHost directory — if so, skip this.
3. Create a `global.json` next to the AppHost `.csproj` that pins the Aspire-supported SDK:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

4. Leave existing services targeting their current TFM unless the user explicitly asks to migrate them.

### Important solution caveat

If the repo's normal root build runs under SDK 8, do **not** assume it can safely own a `net10.0` AppHost project.

When that's likely to break the repo's normal build:

- tell the user explicitly
- prefer keeping the AppHost isolated in its own folder
- only add it to the root solution if the user wants that tradeoff

## Solution membership

A discovered solution means the AppHost was created in project mode, but that does **not** always mean every new project should be added to the root solution automatically.

Use this decision order:

1. If the root solution already includes the services being modeled and is the normal local entry point, prefer adding the AppHost and ServiceDefaults there.
2. If the root solution is tightly coupled to an older SDK/toolchain and adding a `net10.0` AppHost is likely to break routine builds, keep the AppHost outside the solution or in a safer sibling solution boundary.
3. If you're unsure, ask instead of guessing.

## ServiceDefaults in solution-backed repos

Before creating or wiring ServiceDefaults:

1. Look for an existing ServiceDefaults project or equivalent shared bootstrap code.
2. Check whether selected services already have tracing, health checks, or service discovery setup.
3. Check whether the service bootstrap is modern enough for `AddServiceDefaults()` and `MapDefaultEndpoints()`.

If a ServiceDefaults project already exists, reuse it instead of creating another one.

## Legacy bootstrap detection: `IHostBuilder` vs `IHostApplicationBuilder`

This is the easy-to-forget gotcha.

The generated ServiceDefaults extensions typically target **`IHostApplicationBuilder`** and **`WebApplication`** patterns:

```csharp
builder.AddServiceDefaults();
app.MapDefaultEndpoints();
```

That drops cleanly into modern code such as:

- `var builder = WebApplication.CreateBuilder(args);`
- `var builder = Host.CreateApplicationBuilder(args);`

It does **not** automatically map onto older patterns such as:

- `Host.CreateDefaultBuilder(args)`
- `ConfigureWebHostDefaults(...)`
- `UseStartup<Startup>()`
- `IHostBuilder`-only worker/bootstrap code

### What to do when you find legacy hosting

Do **not** silently jam ServiceDefaults into the old shape. **Do not create adapter extension methods on `IHostBuilder`** — ServiceDefaults is designed for `IHostApplicationBuilder` and should only be used with the modern bootstrap pattern.

There are exactly two options. Present them clearly:

1. **Skip ServiceDefaults for now** (recommended for initial setup)
   - Model the service in the AppHost with `AddProject<T>()` or `AddCSharpApp()`
   - The service appears in the dashboard, gets environment wiring, and shows logs
   - No code changes to the service project needed
   - Health checks, service discovery, and OTel from ServiceDefaults are deferred

2. **Modernize the service's bootstrap** (larger change, per-service)
   - Convert `Program.cs` from `Host.CreateDefaultBuilder()` to `WebApplication.CreateBuilder()`
   - Inline the `Startup.ConfigureServices()` into `builder.Services.*` calls
   - Inline the `Startup.Configure()` into the `app.*` middleware pipeline
   - Then add `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`
   - Existing `IHostBuilder` extensions (like custom logging, SDK setup) can be called via `builder.Host.*`

**When multiple services share the same legacy pattern, batch the decision.** If 8 services all use `Host.CreateDefaultBuilder` + `UseStartup<T>`, don't ask 8 times. Ask once:

> *"All 8 of your web services use the legacy IHostBuilder + Startup pattern. I can either (a) model them all in the AppHost without ServiceDefaults for now — they'll appear in the dashboard and get environment wiring but won't have health checks or service discovery — or (b) modernize each service's bootstrap to the WebApplicationBuilder pattern so ServiceDefaults works fully. Which approach do you prefer? You can also mix — modernize a few key services and leave the rest."*

If the repo is conservative or large, default to **asking**, not migrating automatically.

## Modernization guidance

### ASP.NET Core app using `IHostBuilder` / `Startup`

If the user wants full ServiceDefaults support, migrate toward a `WebApplicationBuilder` shape.

Target pattern:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
```

Preserve existing service registrations and middleware ordering carefully. Move only what is required to land on a `WebApplicationBuilder`/`WebApplication` pipeline.

When the `Startup` class has custom extension methods (e.g., `UseBitwardenSdk()`, `AddGlobalSettingsServices()`), those typically need to be called on the new `builder` or `app` in the appropriate phase. Don't silently drop them — trace each call to its registration phase (services vs middleware) and preserve it.

### Worker/background service using `IHostBuilder`

If the service is a worker and the user wants ServiceDefaults, migrate toward `Host.CreateApplicationBuilder(args)`:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
```

For non-web workers, `MapDefaultEndpoints()` usually does not apply unless the app exposes HTTP endpoints.

## AppHost project references

For full project mode, prefer explicit project references from the AppHost to selected .NET services:

```bash
dotnet add <AppHost.csproj> reference <Api.csproj>
```

This keeps solution-backed AppHosts easier to navigate and build.

## Validation checklist for full-solution mode

Before declaring success:

1. The AppHost project builds under its intended SDK boundary.
2. The root solution still behaves the way the user expects, or the user has explicitly accepted any tradeoff.
3. Any ServiceDefaults changes compile in the selected services.
4. `aspire start` works from the AppHost context, and long-lived app resources are healthy rather than merely `Finished`.
5. Legacy `IHostBuilder` services were either modernized intentionally or explicitly left unchanged.
6. Migration runners, if modeled, complete successfully before dependent services start.

## When to ask the user instead of deciding

Ask when:

- adding the AppHost to the root solution might break the repo's normal SDK/build
- a service uses `Startup.cs` / `IHostBuilder` and would need real bootstrap surgery
- there are multiple plausible ServiceDefaults/shared-bootstrap projects to reuse
- the repo has mixed solution boundaries and it's unclear which one is the real developer entry point
- the repo has a custom MSBuild SDK and project classification is ambiguous
- the repo uses conditional compilation and the target variant is unclear
- there are more than 5 runnable services and you need to decide which to wire first
