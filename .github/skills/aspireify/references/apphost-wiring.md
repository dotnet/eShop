# AppHost wiring and API lookup reference

Use this reference when writing Step 5 (Wire up the AppHost) or when you need to look up Aspire APIs, integration packages, or wiring patterns.

> **⚠️ Always look up APIs before writing code.** Do not guess builder method names or parameter shapes. Use `aspire docs search "<topic>"` and `aspire docs get "<slug>"` for documented patterns, then `aspire docs api search "<query>" --language csharp|typescript` and `aspire docs api get "<id>"` to confirm the exact reference entry for the API you are about to call.

## Looking up APIs and integrations

Before writing AppHost code for an unfamiliar resource type or integration, **always** look it up. **Do not assume APIs exist or guess their shapes** — Aspire has many resource types with specific overloads.

### Tiered preference for modeling resources

#### Tier 1: First-party Aspire hosting packages (always prefer)

Packages named `Aspire.Hosting.*` — maintained by the Aspire team and ship with every release. Examples:

| Package | Unlocks |
|---------|---------|
| `Aspire.Hosting.Python` | `AddPythonApp()`, `AddUvicornApp()` |
| `Aspire.Hosting.JavaScript` | `AddJavaScriptApp()`, `AddNodeApp()`, `AddViteApp()`, `.WithYarn()`, `.WithPnpm()` |
| `Aspire.Hosting.PostgreSQL` | `AddPostgres()`, `AddDatabase()` |
| `Aspire.Hosting.Redis` | `AddRedis()` |

#### Tier 2: Community Toolkit packages (use when no first-party exists)

Packages named `CommunityToolkit.Aspire.Hosting.*` — maintained by the community, documented on aspire.dev, and installable via `aspire add`. Examples:

| Package | Unlocks |
|---------|---------|
| `CommunityToolkit.Aspire.Hosting.Golang` | `AddGolangApp()` — handles `go run .`, working dir, PORT env |
| `CommunityToolkit.Aspire.Hosting.Rust` | `AddRustApp()` |
| `CommunityToolkit.Aspire.Hosting.Java` | Java hosting support |

These provide typed APIs with proper endpoint handling, health checks, and dashboard integration — significantly better than raw executables.

#### Tier 3: Raw fallbacks (last resort)

`AddExecutable()`, `AddDockerfile()`, `AddContainer()` — use only when no Tier 1 or Tier 2 package exists for the technology, or when the user's setup is too custom for a typed integration.

### How to discover available packages

```bash
# Search for documentation on a topic
aspire docs search "redis"
aspire docs search "golang"
aspire docs search "python uvicorn"

# Get a specific doc page by slug (returned from search results)
aspire docs get "redis-integration"
aspire docs get "go-integration"

# Find the exact C# / TypeScript API reference entry for a builder method
aspire docs api search "AddRedis" --language csharp
aspire docs api search "AddViteApp" --language typescript
aspire docs api get "<id-from-api-search>"

# List or search available integrations before mutating the AppHost
aspire integration list --format Json
aspire integration search postgres --format Json
```

Use `aspire docs search` / `aspire docs get` to find the right builder methods, configuration options, and patterns. Use `aspire docs api search` / `aspire docs api get` when you need the exact reference entry (parameter shapes, return types, overloads) for the API you are about to call. Use `aspire integration list` and `aspire integration search <query>` to discover packages you might not have known about.

**Don't invent APIs** — if docs search and integration list don't return it, it doesn't exist. Fall back to Tier 3 and note the limitation to the user. **API shapes differ between C# and TypeScript** — always check the correct language docs.

### Check what integrations auto-manage

Before modeling environment variables, passwords, ports, or volumes for a typed integration, **check the docs to see what the integration handles automatically**. Many typed integrations auto-generate passwords, manage ports dynamically, and handle volumes — duplicating this config causes errors or conflicts.

```bash
# Check what AddPostgres manages automatically
aspire docs get "postgresql-hosting-integration" --section "Connection properties"

# Check what AddSqlServer manages
aspire docs get "sql-server-integration" --section "Hosting integration"
```

Look for the **"Connection properties"** section — it lists what the integration injects into consuming services. If it lists `Password`, `Host`, `Port` — the integration manages those. Do not create `AddParameter()` for values the integration already handles.

Common auto-managed values (do NOT model these manually):

| Integration | Auto-managed |
|-------------|-------------|
| `AddPostgres()` | Password, host, port, connection string |
| `AddSqlServer()` | SA password, host, port, connection string |
| `AddRedis()` | Connection string, port |
| `AddMySql()` | Root password, host, port, connection string |
| `AddRabbitMQ()` | Username, password, host, port, connection string |
| `AddMongoDB()` | Connection string, port |

To add an integration package (which unlocks typed builder methods):

```bash
# First-party
aspire add redis
aspire add python
aspire add nodejs

# Community Toolkit
aspire add communitytoolkit-golang
aspire add communitytoolkit-rust
```

After adding, run `aspire restore` (TypeScript) or `dotnet restore` (C#) to update available APIs, then check what methods are now available.

**Always prefer a typed integration over raw `AddExecutable`/`AddContainer`.** Typed integrations handle working directories, port injection, health checks, and dashboard integration automatically.

## Service communication: `WithReference` vs `WithEnvironment`

**`WithReference()`** is the primary way to connect services. It does two things:

1. Injects the referenced resource's connection information (connection string or URL) into the consuming service
2. Enables Aspire service discovery — .NET services can resolve the referenced resource by name

```csharp
// C#: api gets the database connection string injected automatically
var db = builder.AddPostgres("pg").AddDatabase("mydb");
var api = builder.AddCSharpApp("api", "../src/Api")
    .WithReference(db);

// C#: frontend gets service discovery URL for api
var frontend = builder.AddCSharpApp("web", "../src/Web")
    .WithReference(api);
```

```typescript
// TypeScript equivalent
const db = await builder.addPostgres("pg").addDatabase("mydb");
const api = await builder.addCSharpApp("api", "./src/Api")
    .withReference(db);
```

**How services consume references**: Services receive connection info as environment variables. The naming convention is:
- Connection strings: `ConnectionStrings__<resourceName>` (e.g., `ConnectionStrings__mydb=Host=...`)
- Service URLs: `services__<resourceName>__<endpointName>__0` (e.g., `services__api__http__0=http://localhost:5123`)

**`WithEnvironment()`** injects raw environment variables. Use this for custom config that isn't a service reference:

```csharp
var api = builder.AddCSharpApp("api", "../src/Api")
    .WithEnvironment("FEATURE_FLAG_X", "true")
    .WithEnvironment("API_KEY", someParameter);
```

**When to use which:**
- Connecting service A to service B or a database/cache/queue → `WithReference()`
- Passing configuration values, feature flags, API keys → `WithEnvironment()`
- Never manually construct connection strings with `WithEnvironment()` when `WithReference()` would work

## Endpoints and ports

**Prefer HTTPS by default.** Use `WithHttpsEndpoint()` for all services and fall back to `WithHttpEndpoint()` only if HTTPS doesn't work for that resource.

**Prefer Aspire-managed ports by default.** For most local development scenarios, let Aspire assign the port and inject it into the service. This avoids port collisions, makes multiple AppHosts easier to run side-by-side, and keeps cross-service wiring flexible.

**Ask before pinning a fixed port.** If the repo already uses a hardcoded port, do **not** silently preserve it just because it exists. Ask whether that port is actually required. Good reasons to keep a fixed port include:

- OAuth/callback URLs or external webhooks that expect a stable local address
- Browser extensions or desktop/mobile clients that are already hardcoded to a specific port
- Repo docs, scripts, or test tooling that explicitly depend on that exact port

If none of those apply, steer the user toward managed ports.

**`WithHttpsEndpoint()`** — expose an HTTPS endpoint. For services that serve traffic:

```csharp
// Let Aspire assign a random port (recommended for most cases)
var api = builder.AddCSharpApp("api", "../src/Api")
    .WithHttpsEndpoint();

// Use a specific port only when the user confirms it is required
var api = builder.AddCSharpApp("api", "../src/Api")
    .WithHttpsEndpoint(port: 5001);

// For services that read the port from an env var
var nodeApi = builder.AddJavaScriptApp("api", "../api", "start")
    .WithHttpsDeveloperCertificate()
    .WithHttpsEndpoint(env: "PORT");  // Aspire injects PORT=<assigned-port>
```

**`WithHttpsDeveloperCertificate()`** — required for JavaScript and Python apps to serve HTTPS. Configures the ASP.NET Core dev cert. .NET apps handle this automatically.

```csharp
var frontend = builder.AddViteApp("frontend", "../frontend")
    .WithHttpsDeveloperCertificate();

var pyApi = builder.AddUvicornApp("api", "../api", "app:main")
    .WithHttpsDeveloperCertificate();
```

> If `WithHttpsDeveloperCertificate()` causes issues for a resource, fall back to `WithHttpEndpoint()` and leave a comment explaining why.

**`WithHttpEndpoint()`** — fallback for HTTP when HTTPS doesn't work:

```csharp
// Use when HTTPS causes issues with a specific integration
var legacy = builder.AddJavaScriptApp("legacy", "../legacy", "start")
    .WithHttpEndpoint(env: "PORT");  // HTTP fallback
```

**`WithEndpoint()`** — expose a non-HTTP endpoint (gRPC, TCP, custom protocols):

```csharp
var grpcService = builder.AddCSharpApp("grpc", "../src/GrpcService")
    .WithEndpoint("grpc", endpoint =>
    {
        endpoint.Port = 5050;
        endpoint.Protocol = "grpc";
    });
```

**`WithExternalHttpEndpoints()`** — mark a resource's HTTP endpoints as externally visible. Use this for user-facing frontends so the URL appears prominently in the dashboard:

```csharp
var frontend = builder.AddViteApp("frontend", "../frontend")
    .WithHttpsDeveloperCertificate()
    .WithHttpsEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();
```

**Port injection**: Many frameworks (Express, Vite, Flask) need to know which port to listen on. Use the `env:` parameter:
- `withHttpsEndpoint({ env: "PORT" })` (TypeScript)
- `.WithHttpsEndpoint(env: "PORT")` (C#)

Aspire assigns a port and injects it as the specified environment variable. The service should read it and listen on that port.

**Recommended ask when a repo already hardcodes ports:**

> "I found this service pinned to port 3000 today. Unless that exact port is needed for an external callback or another hard requirement, I recommend switching it to read PORT from env and letting Aspire manage the port. That avoids collisions and makes the AppHost more portable. Should I keep 3000 or make it Aspire-managed?"

## Cross-service environment variable wiring

When a service expects a **specific env var name** for a dependency's URL (not the standard `services__` format from `WithReference`), use `WithEnvironment` with an endpoint reference — **never a hardcoded string**:

```typescript
// ✅ CORRECT — endpoint reference resolves to the actual URL at runtime
const roomEndpoint = await room.getEndpoint("http");

const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withEnvironment("VITE_APP_WS_SERVER_URL", roomEndpoint)  // EndpointReference, not a string
    .withReference(room)   // also sets up standard service discovery
    .waitFor(room);

// ❌ WRONG — hardcoded URL breaks when Aspire assigns different ports
    .withEnvironment("VITE_APP_WS_SERVER_URL", "http://localhost:3002")  // NEVER DO THIS
```

```csharp
// C# equivalent
var roomEndpoint = room.GetEndpoint("http");
var frontend = builder.AddViteApp("frontend", "../frontend")
    .WithEnvironment("VITE_APP_WS_SERVER_URL", roomEndpoint)
    .WithReference(room)
    .WaitFor(room);
```

Use `WithEnvironment(name, endpointRef)` when the consuming service reads a **specific env var name**. Use `WithReference()` when the service uses Aspire service discovery or standard connection string patterns. You can use both together.

## URL labels and dashboard niceties

Customize how endpoints appear in the Aspire dashboard:

```csharp
// Named endpoints for clarity
var api = builder.AddCSharpApp("api", "../src/Api")
    .WithHttpsEndpoint(name: "public", port: 8443)
    .WithHttpsEndpoint(name: "internal", port: 8444);
```

For `dev.localhost` cookie isolation and config-based subdomain setup, see Step 9 in the main SKILL.md.

## Dependency ordering: `WaitFor` and `WaitForCompletion`

**`WaitFor()`** — delay starting a resource until another resource is healthy/ready:

```csharp
var db = builder.AddPostgres("pg").AddDatabase("mydb");
var api = builder.AddCSharpApp("api", "../src/Api")
    .WithReference(db)
    .WaitFor(db);  // Don't start api until db is healthy
```

Always pair `WithReference()` with `WaitFor()` for infrastructure dependencies (databases, caches, queues). Services that depend on other services should generally also wait for them.

**`WaitForCompletion()`** — wait for a resource to run to completion (exit successfully). Use for init containers, database migrations, or seed data scripts:

```csharp
var migration = builder.AddCSharpApp("migration", "../src/MigrationRunner")
    .WithReference(db)
    .WaitFor(db);

var api = builder.AddCSharpApp("api", "../src/Api")
    .WithReference(db)
    .WaitFor(db)
    .WaitForCompletion(migration);  // Don't start until migration finishes
```

## Secrets in process arguments — avoid `WithArgs` for sensitive values

**⚠️ Never pass connection strings, passwords, or other secrets via `WithArgs()`.** Process arguments are visible in task managers, `ps` output, process inspection tools, and often end up in logs. This is a secret leakage risk.

```csharp
// ❌ WRONG — connection string visible in process arguments
var migrator = builder.AddCSharpApp("migrator", "../util/Migrator")
    .WithArgs(context =>
    {
        context.Args.Add(db.Resource.ConnectionStringExpression);
    });

// ✅ CORRECT — pass secrets via environment variables
var migrator = builder.AddCSharpApp("migrator", "../util/Migrator")
    .WithEnvironment("DB_CONNECTION_STRING", db.Resource.ConnectionStringExpression);
```

If the tool only accepts the connection string as a CLI argument (e.g., a third-party migration runner), note this limitation to the user and suggest modifying the tool to read from an environment variable or config file instead. If modification isn't possible, using `WithArgs` is acceptable as a pragmatic tradeoff — but flag it explicitly.

## Container lifetimes

By default, containers are stopped when the AppHost stops. Use **persistent lifetime** to keep containers running across restarts (useful for databases during development):

```csharp
var db = builder.AddPostgres("pg")
    .WithLifetime(ContainerLifetime.Persistent);
```

This prevents data loss when restarting the AppHost — the container stays running and the AppHost reconnects.

**TypeScript equivalent:**

```typescript
const db = await builder.addPostgres("pg")
    .withLifetime("persistent");
```

Recommend persistent lifetime for databases and caches during local development.

**⚠️ Stale persistent volumes can cause auth failures.** Typed integrations like `AddSqlServer()`, `AddPostgres()`, `AddRedis()`, and `AddMySql()` auto-generate passwords on first run. Those passwords are stored inside the container's data volume. If the AppHost is recreated or its user-secrets are reset, Aspire generates a *new* password — but the persistent volume still has the *old* one. The symptom is repeated `Login failed` or `password authentication failed` errors in the container logs.

To fix: stop the AppHost, remove the stale container and its volume (`docker rm -f <name>; docker volume rm <volume>`), then restart. Aspire will recreate both with a matching password. Mention this to the user if they see auth failures on persistent infrastructure containers after recreating the AppHost.

## Explicit start (manual start)

Some resources shouldn't auto-start with the AppHost. Mark them for explicit start:

```csharp
var debugTool = builder.AddContainer("profiler", "myregistry/profiler")
    .WithLifetime(ContainerLifetime.Persistent)
    .ExcludeFromManifest()
    .WithExplicitStart();
```

The resource appears in the dashboard but stays stopped until the user manually starts it. Useful for debugging tools, admin UIs, or optional services.

## Parent resources (grouping in the dashboard)

Group related resources under a parent for a cleaner dashboard:

```csharp
var postgres = builder.AddPostgres("pg");
var ordersDb = postgres.AddDatabase("orders");
var inventoryDb = postgres.AddDatabase("inventory");
// ordersDb and inventoryDb appear nested under pg in the dashboard
```

This happens automatically for databases added to a server resource. For custom grouping of arbitrary resources, use `WithParentRelationship()`:

```csharp
var backend = builder.AddResource(new ContainerResource("backend-group"));
var api = builder.AddCSharpApp("api", "../src/Api")
    .WithParentRelationship(backend);
var worker = builder.AddCSharpApp("worker", "../src/Worker")
    .WithParentRelationship(backend);
```

Use `aspire docs search "parent relationship"` to verify the current API shape.

## Volumes and data persistence

```csharp
// Named volume (managed by Docker, persists across container recreations)
var db = builder.AddPostgres("pg")
    .WithDataVolume("pg-data");

// Bind mount (maps to a host directory)
var db = builder.AddPostgres("pg")
    .WithBindMount("./data/pg", "/var/lib/postgresql/data");
```

```typescript
const db = await builder.addPostgres("pg")
    .withDataVolume("pg-data");
```
