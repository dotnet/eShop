# Docker Compose migration

Use this reference when the repo has a `docker-compose.yml` or `compose.yml` file. Docker Compose files are one of the most valuable discovery sources — they document the infrastructure the app actually needs to run locally.

## When to load this reference

- A `docker-compose.yml`, `compose.yml`, or `docker-compose.override.yml` exists anywhere in the repo
- The repo has setup scripts that call `docker compose up` as part of the dev workflow

## Profiles

Docker Compose files can use `profiles:` to organize services into named groups. Not all services run at once — the developer chooses which profiles to activate.

Example from a real repo:

```yaml
services:
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    profiles: [cloud, mssql]
  postgres:
    image: postgres:14
    profiles: [postgres, ef]
  redis:
    image: redis:alpine
    profiles: [redis, cloud]
  storage:
    image: mcr.microsoft.com/azure-storage/azurite:latest
    profiles: [storage, cloud]
  mail:
    image: sj26/mailcatcher:latest
    profiles: [mail]
```

When profiles are present:

1. **List them clearly** — show the user which profiles exist and what services each activates
2. **Ask which to target** — *"Your docker-compose has profiles: cloud, mssql, postgres, storage, redis, mail. Which ones represent your typical local dev stack?"*
3. **Model only selected profiles** — services in unselected profiles are skipped entirely
4. **Services without profiles always run** — if a service has no `profiles:` key, include it regardless of profile selection
5. **Profile-specific infrastructure implies choices** — `mssql` vs `postgres` profiles often mean the repo supports multiple database backends. Ask which one to model in the AppHost.

## Image-to-integration mapping

Prefer typed Aspire integrations over raw `AddContainer()`. Use `aspire docs search <technology>` to check for available integrations.

Common mappings:

| Compose image | Aspire integration | Method |
|---------------|-------------------|--------|
| `postgres:*` | `Aspire.Hosting.PostgreSQL` | `AddPostgres()` |
| `mcr.microsoft.com/mssql/server:*` | `Aspire.Hosting.SqlServer` | `AddSqlServer()` |
| `mysql:*` / `mariadb:*` | `Aspire.Hosting.MySql` | `AddMySql()` |
| `redis:*` | `Aspire.Hosting.Redis` | `AddRedis()` |
| `rabbitmq:*` | `Aspire.Hosting.RabbitMQ` | `AddRabbitMQ()` |
| `mongo:*` | `Aspire.Hosting.MongoDB` | `AddMongoDB()` |
| `mcr.microsoft.com/azure-storage/azurite:*` | `Aspire.Hosting.Azure.Storage` | `AddAzureStorage().RunAsEmulator()` |
| `kafka`, `confluentinc/cp-kafka:*` | `Aspire.Hosting.Kafka` | `AddKafka()` |
| `nats:*` | `Aspire.Hosting.Nats` | `AddNats()` |
| `mcr.microsoft.com/azure-messaging/servicebus-emulator:*` | `Aspire.Hosting.Azure.ServiceBus` | `AddAzureServiceBus().RunAsEmulator()` |

For images not in this list, use `aspire docs search` to check, then fall back to `AddContainer()`.

## Environment variable interpolation

Compose files use `${VAR}` syntax to reference variables from `.env` files:

```yaml
environment:
  MSSQL_SA_PASSWORD: ${MSSQL_PASSWORD}
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
```

**⚠️ CRITICAL: Do not model passwords for typed Aspire integrations.**

`AddPostgres()`, `AddSqlServer()`, `AddRedis()`, `AddMySql()`, `AddRabbitMQ()`, and other typed integrations **auto-generate secure passwords**. The compose file needed `POSTGRES_PASSWORD` because Compose doesn't manage credentials — Aspire does. If you see a compose password variable that maps to a typed integration, **skip it entirely**. Do not create an `AddParameter` for it.

```csharp
// ❌ WRONG — don't model passwords that Aspire auto-generates
var pgPassword = builder.AddParameter("postgres-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: pgPassword);

// ✅ RIGHT — let Aspire handle the password
var postgres = builder.AddPostgres("postgres");
```

Use `aspire docs get <integration-slug>` to check what each typed integration manages automatically. Look for the "Connection properties" section — if it lists `Password`, the integration handles it.

When you see a `${VAR}` pattern in compose:

1. **Check if it maps to a typed integration** — if `POSTGRES_PASSWORD`, `MSSQL_SA_PASSWORD`, `MYSQL_ROOT_PASSWORD`, `RABBITMQ_DEFAULT_PASS`, etc. are used by a typed Aspire integration, **skip them** — Aspire manages these
2. **Trace non-integration variables** — find them in the `.env` or `.env.example` file
3. **Classify** — is it a secret (API key, token) or plain config?
4. **Model it** — secrets become `AddParameter(name, secret: true)`, plain config becomes `AddParameter(name)` with a default or `WithEnvironment()` directly

## Volume mapping

| Compose volume type | Aspire equivalent | Notes |
|--------------------|--------------------|-------|
| Named volume (`mssql_data:/var/opt/mssql`) | `WithDataVolume()` | Preferred — Aspire manages lifecycle |
| Named volume (custom name) | `WithDataVolume(name: "custom")` | Preserves the name for familiarity |
| Bind mount (`./data:/app/data`) | `WithBindMount("./data", "/app/data")` | Use for config files, scripts, or shared data |
| Bind mount for config (`./config.json:/etc/config.json`) | `WithBindMount(...)` | Preserve for config injection |

**Tip:** If the compose file mounts migration scripts or SQL files into a database container, those are likely init scripts. See "Init and setup scripts" below.

## Dependency ordering

Compose `depends_on` maps to Aspire's `WaitFor()`:

```yaml
# Compose
services:
  api:
    depends_on:
      - mssql
      - redis
```

```csharp
// Aspire
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(mssql)
    .WithReference(redis)
    .WaitFor(mssql)
    .WaitFor(redis);
```

`WithReference()` establishes the connection. `WaitFor()` ensures the dependency is healthy before the consuming service starts. Use both together.

For `depends_on` with `condition: service_healthy`, the `WaitFor()` mapping is especially important — it replicates the same behavior.

## Build contexts

Compose services with `build:` are built from source, not pulled as images:

```yaml
services:
  worker:
    build:
      context: ./worker
      dockerfile: Dockerfile
```

Map these to `AddDockerfile()`:

```csharp
var worker = builder.AddDockerfile("worker", "../worker")
    .WithHttpEndpoint(targetPort: 8080);
```

However, **prefer native Aspire project hosting over Dockerfiles when possible**. If the build context contains a `.csproj`, use `AddProject<T>()`. If it's a Node.js app, use `AddNodeApp()` or `AddViteApp()`. Dockerfiles are a last resort for Aspire modeling — they lose service discovery, health checks, and hot reload.

## Init and setup scripts

Repos often have setup scripts alongside their compose files:

- `setup_azurite.ps1` — initializes storage emulator containers and queues
- `migrate.ps1` / `ef_migrate.ps1` — runs database migrations
- `setup_secrets.ps1` — configures .NET user secrets
- `create_certificates_*.sh` — generates dev certificates

**Present the user with options for how to handle these:**

1. **Model as a lifecycle command on the relevant resource** — for example, a database migration script can be a startup command on the database resource. This runs automatically when the resource starts.
   → *"Your repo has a migrate.ps1 that runs SQL migrations against the database. I can model this as a startup lifecycle hook on the database resource so migrations run automatically when you `aspire start`. Want that?"*

2. **Model as a standalone executable resource** — for scripts that don't map cleanly to a single resource, use `AddExecutable()` with `WaitForCompletion()` so dependent services wait for the script to finish.

3. **Leave as manual** — some setup scripts are one-time-only (like certificate generation) and don't need to run every time. Note them in the AppHost as a comment and move on.

The right choice depends on the script. Present the tradeoff and let the user decide.

## Putting it together

A compose file like:

```yaml
services:
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      MSSQL_SA_PASSWORD: ${MSSQL_PASSWORD}
    volumes:
      - mssql_data:/var/opt/mssql
    ports:
      - "1433:1433"
  redis:
    image: redis:alpine
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"
```

Becomes:

```csharp
// ✅ Let Aspire auto-generate the SA password — don't model MSSQL_PASSWORD
var mssql = builder.AddSqlServer("mssql")
    .WithDataVolume();

var redis = builder.AddRedis("redis")
    .WithDataVolume();
```

Note: for typed integrations like `AddSqlServer()` and `AddRedis()`, you don't need to map ports or passwords — Aspire handles both. You also don't need to model `redis_data` as a named volume — `WithDataVolume()` handles persistence. The `${MSSQL_PASSWORD}` from the compose file is skipped entirely because `AddSqlServer()` auto-generates a secure SA password.

## Common pitfalls

- **Don't model every compose service** — some are dev-only tools (mailcatcher, reverse proxies, SAML IdPs for testing). Ask the user which are essential vs nice-to-have.
- **Don't preserve hardcoded ports from compose** — Aspire manages ports dynamically. Only preserve a port if the user confirms it's required for external reasons (OAuth callbacks, etc.).
- **Don't duplicate compose's `.env` interpolation** — Aspire parameters replace this pattern. Trace each `${VAR}` to its source and model it properly.
- **Watch for services that conflict on the same port** — compose profiles often have services sharing ports (e.g., `mssql` and `postgres` both on different profiles). If the user selects conflicting profiles, surface the conflict.
