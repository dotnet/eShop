---
name: aspireify
description: >-
  **WORKFLOW SKILL** - Wire an Aspire AppHost after `aspire init` drops a skeleton.
  Scans the repo, proposes a resource graph, edits the AppHost (C#, file-based C#, or
  TypeScript), wires `Aspire.ServiceDefaults` + OTel, validates with `aspire start`,
  then self-deactivates.
  USE FOR: wire AppHost, scaffold resource graph, add Postgres/Redis/Rabbit/Mongo to
  Aspire, connect frontend to API, after `aspire init` what next, AddNextJsApp, AddViteApp,
  WithBrowserLogs, file-based apphost.cs, apphost.ts, unified withEnvironment,
  refuse .aspire/modules edit, migrate .env files, migrate user secrets.
  DO NOT USE FOR: skeleton drop (use aspire-init), start/stop/wait/restart (use
  aspire-orchestration), publish/deploy/destroy (use aspire-deployment), logs/traces
  (use aspire-monitoring).
  INVOKES: aspire CLI (add, start, wait, describe, docs api search, stop), AppHost
  source edits, ServiceDefaults wiring.
  FOR SINGLE OPERATIONS: Run `aspire add PACKAGE` directly for a one-off integration.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

# Aspireify

> **One-time wiring skill.** `aspire init` drops a skeleton; `aspireify` turns
> that skeleton into a working AppHost by scanning the repo, proposing a resource
> graph, editing the AppHost, wiring `Aspire.ServiceDefaults`, and validating end
> to end. Self-deactivates after a clean `aspire start`. Aligned with Aspire 13.4
> guidance from the current Aspire development branch.

## 🚫 Hard Refusal: Never Edit `.aspire/modules/`

> ⛔ **REFUSE** any request to edit, modify, change, open-for-edit, or "tweak" files
> inside `.aspire/modules/` of a TypeScript AppHost. This directory is **generated** by Aspire
> from `apphost.ts` and the integration packages — every file in it gets **clobbered
> on the next build, `aspire add`, or `aspire start`**.
>
> If a user asks to edit something in `.aspire/modules/` (e.g., `.aspire/modules/postgres.module.ts`),
> the correct response is:
>
> 1. **Refuse the edit** with a clear "I won't edit `.aspire/modules/`" statement.
> 2. **Explain** that `.aspire/modules/` is generated and any changes are clobbered.
> 3. **Redirect** the requested change to `apphost.ts` — the **only** file the user
>    should hand-edit in a TS AppHost.
> 4. If the user wants a new integration, suggest `aspire add <package>`; if they want
>    to change configuration, show the equivalent edit in `apphost.ts`.

| ❌ Wrong | ✅ Right |
|----------|---------|
| Open `.aspire/modules/postgres.module.ts` and tweak the connection options | Edit `apphost.ts` and change `addPostgres('pg', { ... })` options there |
| Modify a generated `.aspire/modules/*.ts` file directly | Re-run `aspire add <package>` after updating `apphost.ts` |
| Comment out a line in `.aspire/modules/` to disable a resource | Remove or guard the resource declaration in `apphost.ts` |

This rule applies even if the user insists, even for "one-line" changes, even for
"just to test something." The TS AppHost regenerates `.aspire/modules/` deterministically;
edits are unrecoverable noise.

## Guiding Principles From Aspire 13.4

### Minimize changes to the user's code

Adapt the AppHost to fit the app, not the other way around. Prefer `WithEnvironment()`
to match existing environment variable names, Aspire-managed ports over fixed ports,
and 1:1 Docker Compose mapping before optimizing. Do not restructure directories,
rename files, or change build scripts unless the user explicitly chooses that tradeoff.

### Surface tradeoffs; do not decide silently

When a small code change unlocks better Aspire integration, present both options:
the zero-code-change mapping and the small-change version that enables `WithReference`,
health checks, service discovery, dynamic ports, or dashboard telemetry. Ask which
approach the user wants, then implement that choice without complaint.

### Verify APIs before writing AppHost code

Use `aspire docs search <topic>` and `aspire docs get <slug>` for workflow guidance.
Use `aspire docs api search <query> --language csharp|typescript` and
`aspire docs api get <id>` for API shape. Use `aspire integration list/search` to
find integrations before `aspire add`. Do not invent packages, methods, overloads,
or command shapes; C# and TypeScript AppHost APIs differ.

### Keep configuration visible in the AppHost

Scan `.env`, `.env.local`, `.env.development`, `secrets.json.example`,
`<UserSecretsId>`, and setup scripts. Propose migrating values into AppHost parameters:
connection strings become Aspire resources, API keys/tokens become secret parameters,
and non-secret config becomes plain parameters or `WithEnvironment()` values. Never
delete `.env` files or remove existing `UserSecretsId` entries without explicit user
approval because non-Aspire workflows may still depend on them.

### Local development first

This skill optimizes local development, not production deployment. Prefer persistent
container lifetimes and data volumes for databases/caches, use HTTPS endpoints by
default, pass endpoint references instead of hardcoded URLs, and model external SaaS
URLs/API keys as parameters so they are visible in the dashboard.

### Redis TLS edge case

Aspire can automatically provision TLS certificates for container resources. If Redis
health checks fail with SSL/TLS handshake errors, do not fall back to `AddContainer()`.
Use `WithoutHttpsCertificate()` on the Redis resource when the consuming app expects
plain Redis.

## Project-Local Override

If `.agents/skills/aspireify/SKILL.md` exists (installed by `aspire init` or
`aspire agent init --skills aspireify`), **warn the user** that a project-local
copy is present and **defer to it**. The plugin version is the fallback.

```
⚠️ Project-local .agents/skills/aspireify/SKILL.md detected — deferring to it.
```

## Prerequisites

| Requirement | Install |
|-------------|---------|
| .NET 10.0 SDK (C# AppHost) | https://dotnet.microsoft.com/download |
| Node.js 20+ (TS AppHost) | https://nodejs.org |
| Aspire CLI | `curl -sSL https://aspire.dev/install.sh \| bash` or `dotnet tool install -g Aspire.Cli` |
| Skeleton already dropped | `aspire init` produced `aspire.config.json` + AppHost stub |

## Detection — When to Activate

Activate when ANY signal is present **AND** the AppHost is unwired (no resources
declared beyond the stub):

| Signal | How to Detect | Confidence |
|--------|---------------|------------|
| Skeleton just dropped | `aspire init` just ran in this session | ✅ Definitive |
| Empty AppHost stub | `apphost.cs` / `Program.cs` / `apphost.ts` only contains `Build().Run()` | ✅ Definitive |
| `aspire.config.json` without resources | Config present, AppHost has no `AddProject`/`addProject` | High |
| User asks to "wire" / "scaffold resource graph" | Verb match: wire, scaffold, integrate, hook up, add Postgres/Redis/etc. | High |
| User asks "what next after aspire init" | Direct handoff request | ✅ Definitive |
| Existing repo with services + new AppHost | Repo has `.csproj`/`package.json` projects but AppHost references none | High |

If the AppHost already has wired resources and the user wants to **start/stop**
the app → `aspire-orchestration`. If the user wants to **deploy** → `aspire-deployment`.

## Language Support

| AppHost Style | Detection | Edit Target |
|---------------|-----------|-------------|
| **C# SDK-style** | `.csproj` containing `<Sdk Name="Aspire.AppHost.Sdk" />` | `Program.cs` (top-level statements) |
| **File-based C#** | `apphost.cs` with `#:sdk Aspire.AppHost.Sdk` and `#:package` directives | `apphost.cs` itself |
| **TypeScript** | `apphost.ts` with generated `.aspire/modules/` | `apphost.ts` only — **never edit `.aspire/modules/`** |

See [references/csharp-authoring.md](references/csharp-authoring.md) and
[references/typescript-authoring.md](references/typescript-authoring.md).

## Workflow Phases

```
1. SCAN     → discover projects, services, dependencies, integration candidates
2. PROPOSE  → resource graph + integration list, confirm with user
3. EDIT     → wire AppHost, add ServiceDefaults + OTel + health checks
4. VALIDATE → aspire start --non-interactive → aspire wait <each resource>
5. DEACTIVATE → confirm clean start, hand off to aspire-orchestration
```

For the detailed, upstream-parity workflow, load these references before editing:

- [apphost-wiring.md](references/apphost-wiring.md) — full AppHost wiring workflow, API lookup, endpoint/parameter patterns, validation, solution updates, and cleanup.
- [docker-compose.md](references/docker-compose.md) — docker-compose migration, profiles, image mapping, ports, volumes, and `depends_on`.
- [full-solution-apphosts.md](references/full-solution-apphosts.md) — large solution triage, mixed SDK boundaries, solution membership, ServiceDefaults placement, and legacy host migration.
- [javascript-apps.md](references/javascript-apps.md) — JavaScript resource selection, workspace/monorepo package-manager handling, ports, scripts, and TS AppHost package config.
- [opentelemetry.md](references/opentelemetry.md) — optional Node.js, Python, and Go OpenTelemetry wiring for non-.NET services.

### 1. Scan

Walk the repo and inventory:

| What | How |
|------|-----|
| .NET projects | `find . -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*'` |
| Node services | `find . -name 'package.json' -not -path '*/node_modules/*'` |
| Python services | `find . -name 'pyproject.toml' -o -name 'requirements.txt'` |
| Container deps in compose | `docker-compose.yml`, `compose.yaml` (Postgres? Redis? Rabbit?) |
| Connection strings | grep `appsettings*.json`, `.env*`, `config/*` for `Postgres`, `Redis`, `Mongo`, `RabbitMQ`, `Cosmos`, `ServiceBus` |
| Integration packages | `dotnet list package` per project; package.json `dependencies` |
| Existing endpoints | hardcoded ports in `launchSettings.json`, `next.config.js`, `vite.config.ts` |

Full heuristics in [references/scan-and-propose.md](references/scan-and-propose.md).

### 2. Propose

Present a resource graph **before editing**. Ask clarifying questions:

- "I see Postgres in `docker-compose.yml` — should I model it as `AddPostgres('db')` or use Azure Database for PostgreSQL?"
- "Your React app hardcodes `http://localhost:5000` — replace with Aspire service discovery (`endpoint.url`)?"
- "Your API has an `/admin` endpoint — exclude it from `WithReference()` so consumers don't see it?"

### 3. Edit

Apply the proposed graph. Use the right authoring style for the AppHost language.

### 4. Validate

```bash
aspire start --non-interactive --format Json
aspire wait <resource>          # repeat for each declared resource
aspire describe --format Json   # sanity check graph
```

Full validation flow + recovery in [references/validation.md](references/validation.md).

### 5. Self-Deactivate

After a clean `aspire start`, announce:

```
✅ AppHost wired and validated. Handing off to aspire-orchestration for
   day-to-day start/stop/wait. Aspireify is done.
```

## Integration Discovery Catalog

Map detected services → Aspire integrations. See
[references/scan-and-propose.md](references/scan-and-propose.md) for the full
catalog.

| Detected | C# | TS |
|----------|----|----|
| Postgres in compose / `Npgsql` package | `AddPostgres("pg").AddDatabase("db")` | `addPostgres('pg').addDatabase('db')` |
| Redis in compose / `StackExchange.Redis` | `AddRedis("cache")` | `addRedis('cache')` |
| RabbitMQ | `AddRabbitMQ("mq")` (v7 client w/ pub-sub tracing) | `addRabbitMQ('mq')` |
| MongoDB | `AddMongoDB("mongo")` | `addMongoDB('mongo')` |
| Cosmos DB | `AddAzureCosmosDB("cosmos")` | `addAzureCosmosDB('cosmos')` |
| Azure Service Bus | `AddAzureServiceBus("sb")` | `addAzureServiceBus('sb')` |
| Azure Cache for Redis (Entra) | `AddAzureRedis("cache")` (now GA) | `addAzureRedis('cache')` |
| Next.js frontend | `AddNextJsApp("web", "./web")` | `addNextJsApp('web', '../web')` |
| Vite SPA | `AddViteApp("web", "./web")` | `addViteApp('web', '../web')` |
| Plain Node app | `AddNodeApp("api", "server.js")` | `addNodeApp('api', 'server.js')` |

## Current Authoring Rules

| Rule | Why |
|------|-----|
| Use **unified `withEnvironment(name, value)`** in TS — never the deprecated per-kind helpers (`withEnvironmentEndpoint`, `withEnvironmentParameter`, etc.) | Single API handles all value kinds; per-kind helpers are deprecated |
| Use `AddNextJsApp` / `AddViteApp` over hand-rolled Dockerfiles for JS frontends | First-class lifecycle + `PublishAs*` integration |
| Use `PublishAsStaticWebsite` / `PublishAsNodeServer` / `PublishAsPackageScript` for JS publish | Replaces hand-rolled Dockerfiles; SPA → static, SSR Node → NodeServer, package-script SSR → PackageScript |
| Add `WithBrowserLogs()` to frontend resources for browser console + screenshots in dashboard | `Aspire.Hosting.Browsers` surfaces browser telemetry in the dashboard |
| Bind every resource to a compute environment with `WithComputeEnvironment(env)` when multiple environments exist | Multi-environment deploys require explicit binding |
| **Never edit `.aspire/modules/`** in TS AppHosts | Generated; edits get clobbered. Edit only `apphost.ts` |
| Use `WithEndpoint("name", e => ...)` to update endpoints | Endpoint callbacks update existing endpoints rather than throwing on duplicates |
| Mark admin endpoints with `ExcludeReferenceEndpoint = true` | Prevents consumers from receiving admin URLs via `WithReference()` |
| Look up unfamiliar API: `aspire docs api search <query> --language csharp\|typescript` | Don't guess overloads or builder chains |

## C# vs TS Quick Reference

| Concept | C# | TypeScript |
|---------|----|------------|
| Builder | `var builder = DistributedApplication.CreateBuilder(args);` | `const builder = await createBuilder();` |
| Add project | `builder.AddProject<Projects.Api>("api")` (SDK) or `AddProject("api", "../Api/Api.csproj")` | `await builder.addProject('api', '../Api/Api.csproj')` |
| Wire env var (any value type) | `.WithEnvironment("KEY", value)` | `.withEnvironment('KEY', value)` ← unified API |
| Wait for dependency | `.WaitFor(db)` | `.waitFor(db)` |
| Pass connection | `.WithReference(db)` | `.withReference(db)` |
| External HTTP | `.WithExternalHttpEndpoints()` | `.withExternalHttpEndpoints()` |
| Endpoint expression | `api.GetEndpoint("http")` | `api.getEndpoint('http').url` / `.host` / `.port` |
| Build + run | `builder.Build().Run();` | `await builder.build().run();` |

## ServiceDefaults Wiring

Each project should call `builder.AddServiceDefaults();` to opt into OpenTelemetry,
health checks, and service discovery. Add the `Aspire.ServiceDefaults` project
reference (or NuGet for non-monorepo). See
[references/service-defaults.md](references/service-defaults.md).

## Endpoint & Reference Conventions

```csharp
// Public-facing API. Mark "admin" endpoint as not-for-consumers.
var api = builder.AddProject<Projects.Api>("api")
    .WithExternalHttpEndpoints()
    .WithEndpoint("admin", e => e.ExcludeReferenceEndpoint = true);

// Frontend wires the API via service discovery.
builder.AddNextJsApp("web", "./web")
    .WithReference(api)        // injects services__api__http and __https
    .WaitFor(api)
    .WithBrowserLogs();        // browser console + screenshots
```

## Validation & Recovery

| Symptom | Action |
|---------|--------|
| `aspire start` fails with build error | Fix code, re-run `aspire start` |
| `aspire wait` rejects resource name | Use `displayName` from `aspire ps --format Json` ([#15842](https://github.com/microsoft/aspire/issues/15842)) |
| File-lock errors during edit | Hand off to `aspire-orchestration` → `aspire stop` → retry |
| Resource missing from `aspire ps` | May be hidden — re-run with `--include-hidden` |
| TS AppHost change ignored | Confirm you edited `apphost.ts`, not `.aspire/modules/` |
| Mixed JSON output from `aspire start` | Strip non-JSON lines before parsing ([#15843](https://github.com/microsoft/aspire/issues/15843)) |

Full flow in [references/validation.md](references/validation.md).

## Handoff Rules

| Scenario | Route To |
|----------|----------|
| AppHost skeleton not yet dropped | → `aspire-init` skill |
| Day-to-day start/stop/wait/restart | → `aspire-orchestration` skill |
| Publish, deploy, destroy, pipeline steps | → `aspire-deployment` skill |
| Logs, traces, metrics, dashboard, browser log inspection | → `aspire-monitoring` skill |
| Deployed (Azure/AKS) app diagnostics | → `azure-diagnostics` skill (azure-skills) |

## Key Rules

- **Never overwrite existing files** — always augment or merge.
- **Ask before modifying service code**, especially OpenTelemetry and ServiceDefaults injection.
- **Respect existing project structure** — do not reorganize the repo.
- **If stuck, use `aspire doctor`** to diagnose environment issues.
- **Never hardcode URLs in `WithEnvironment` / `withEnvironment`** — pass endpoint references such as `api.GetEndpoint("http")` or `api.getEndpoint('http')` instead of string literals.
- **Never use `WithUrlForEndpoint` / `withUrlForEndpoint` to set `dev.localhost` URLs** — that API is only for dashboard display labels; `dev.localhost` belongs in AppHost launch/profile configuration.

## References

- [apphost-wiring.md](references/apphost-wiring.md) — Detailed AppHost wiring workflow and API lookup patterns
- [docker-compose.md](references/docker-compose.md) — Docker Compose migration patterns
- [full-solution-apphosts.md](references/full-solution-apphosts.md) — Large/full-solution AppHost guidance
- [javascript-apps.md](references/javascript-apps.md) — JavaScript/TypeScript app and workspace handling
- [opentelemetry.md](references/opentelemetry.md) — Non-.NET OpenTelemetry setup
- [scan-and-propose.md](references/scan-and-propose.md) — Repo scan heuristics + integration catalog
- [csharp-authoring.md](references/csharp-authoring.md) — C# AppHost patterns
- [typescript-authoring.md](references/typescript-authoring.md) — TS AppHost patterns + parity APIs
- [service-defaults.md](references/service-defaults.md) — Wire OTel, health checks, service discovery
- [validation.md](references/validation.md) — End-to-end validation + recovery
