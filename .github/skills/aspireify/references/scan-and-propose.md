# Scan & Propose

Heuristics for the **scan** and **propose** phases of `aspireify`.

## Scan Checklist

| Inventory | How |
|-----------|-----|
| .NET projects | `find . -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*'` |
| Top-level Node services | `find . -maxdepth 4 -name 'package.json' -not -path '*/node_modules/*'` |
| Python services | `find . -maxdepth 4 -name 'pyproject.toml' -o -name 'requirements.txt'` |
| Container deps | `cat docker-compose*.y*ml compose*.y*ml 2>/dev/null` |
| Connection strings | `grep -rIE '(Postgres\|Redis\|Mongo\|RabbitMQ\|Cosmos\|ServiceBus\|AMQP)' --include='*.json' --include='.env*' --include='*.config'` |
| Hardcoded URLs | `grep -rIE 'http://localhost:[0-9]+' --include='*.ts' --include='*.tsx' --include='*.js' --include='*.cs'` |
| Existing integration packages | `dotnet list package` per `.csproj`; `jq .dependencies package.json` per Node project |
| Existing endpoints | `launchSettings.json`, `next.config.js`, `vite.config.ts`, `apphost.ts` modules |
| Existing AppHost references | `apphost.cs` / `Program.cs` / `apphost.ts` — what's already wired? |

## Heuristics

| Signal | Suggest |
|--------|---------|
| `.csproj` references `Microsoft.AspNetCore.App` | API project → `AddProject` + `WithExternalHttpEndpoints()` if user-facing |
| `.csproj` references `Microsoft.NET.Sdk.Worker` | Background worker → `AddProject`, no external endpoints |
| `package.json` has `"next"` dependency | `AddNextJsApp` — confirm `next.config.js` has `output: 'standalone'` |
| `package.json` has `"vite"` + SPA bundle | `AddViteApp` + `PublishAsStaticWebsite(apiPath, apiTarget)` |
| `package.json` has `"vite"` + `.output/server/index.mjs` (TanStack/SvelteKit) | `AddViteApp` (dev) + `PublishAsNodeServer` (publish) |
| `package.json` has Remix / Astro / Nitro Next | `PublishAsPackageScript` |
| `pyproject.toml` with FastAPI/Flask | `AddPythonApp` (or model under TS AppHost) |
| `Program.cs` reads `ConnectionStrings:Postgres*` / DI calls `AddNpgsql*` | `AddPostgres('pg').AddDatabase('appdb')` + `WithReference` |
| `Program.cs` calls `AddStackExchangeRedisCache` | `AddRedis('cache')` + `WithReference` |
| MongoClient / `MongoDB.Driver` | `AddMongoDB('mongo')` |
| Code refs `RabbitMQ.Client` / `IConnection` | `AddRabbitMQ('mq')` (v7 client — pub/sub tracing) |
| Code refs `Microsoft.Azure.Cosmos` | `AddAzureCosmosDB('cosmos')` |
| Code refs `Azure.Messaging.ServiceBus` | `AddAzureServiceBus('sb')` |
| Frontend hardcodes `http://localhost:5000` | Replace with service discovery: `endpoint.url` (TS) or `WithReference(api)` |

## Integration Catalog

### Datastores

| Service | C# integration | TS integration | Notes |
|---------|----------------|----------------|-------|
| Postgres | `AddPostgres("pg").AddDatabase("appdb")` | `addPostgres('pg').addDatabase('appdb')` | Npgsql metrics align to .NET 10 |
| SQL Server | `AddSqlServer("sql").AddDatabase("appdb")` | `addSqlServer('sql').addDatabase('appdb')` | Container-backed locally |
| MySQL | `AddMySql("my").AddDatabase("appdb")` | `addMySql('my').addDatabase('appdb')` | |
| MongoDB | `AddMongoDB("mongo").AddDatabase("app")` | `addMongoDB('mongo').addDatabase('app')` | |
| Redis | `AddRedis("cache")` | `addRedis('cache')` | |
| Azure Cache for Redis | `AddAzureRedis("cache")` | `addAzureRedis('cache')` | `Aspire.Microsoft.Azure.StackExchangeRedis` is GA |
| Cosmos DB | `AddAzureCosmosDB("cosmos")` | `addAzureCosmosDB('cosmos')` | |
| Azure SQL | `AddAzureSqlServer("sql")` | `addAzureSqlServer('sql')` | |
| Azure Storage | `AddAzureStorage("storage")` | `addAzureStorage('storage')` | |

### Messaging

| Service | C# | TS | Notes |
|---------|----|----|-------|
| RabbitMQ | `AddRabbitMQ("mq")` | `addRabbitMQ('mq')` | v7 client, OTel pub/sub tracing |
| Azure Service Bus | `AddAzureServiceBus("sb")` | `addAzureServiceBus('sb')` | |
| Kafka | `AddKafka("kafka")` | `addKafka('kafka')` | |
| Azure Event Hubs | `AddAzureEventHubs("eh")` | `addAzureEventHubs('eh')` | |
| Durable Task Scheduler | `AddDurableTaskScheduler(...)` | n/a | Experimental: `ASPIREDURABLETASK001` |

### Frontends (JS/TS)

| Pattern | Add | Publish |
|---------|-----|---------|
| Next.js (SSR or static) | `AddNextJsApp("web", "./web")` | Auto — Next.js standalone (set `output: 'standalone'` in `next.config.js`) |
| Vite SPA | `AddViteApp("web", "./web")` | `PublishAsStaticWebsite(apiPath: "/api", apiTarget: api)` |
| Vite + TanStack/SvelteKit (SSR via Node) | `AddViteApp("web", "./web")` | `PublishAsNodeServer(entryPoint: ".output/server/index.mjs", outputPath: ".output")` |
| Remix / Astro SSR / Nitro | `AddNodeApp` or `AddViteApp` | `PublishAsPackageScript(scriptName: "start")` |
| Plain Node | `AddNodeApp("api", "server.js")` | `PublishAsNodeServer` |

Bun, Yarn, and pnpm are first-class in TS AppHosts (npm remains the default).

### AI / Foundry

| Pattern | API |
|---------|-----|
| Azure AI Foundry Prompt Agent | `AddPromptAgent(...)` (replaces non-functional `AddAndPublishPromptAgent`) |
| Predefined Foundry models in TS | `[AspireValue]` + catalogs like `FoundryModels.OpenAI.Gpt41Mini` |

### Compute environments (binding)

| Target | API |
|--------|-----|
| Azure Container Apps | `AddAzureContainerAppEnvironment("aca")` |
| Azure App Service | `AddAzureAppServiceEnvironment("appsvc")` |
| Azure Kubernetes Service | `AddAzureKubernetesEnvironment("aks").WithSystemNodePool(...)` |
| Plain Kubernetes | `AddKubernetesEnvironment("k8s")` (Helm-based) |
| Docker Compose | `AddDockerComposeEnvironment("compose")` |

Bind a resource: `.WithComputeEnvironment(env)`. **Required** when multiple
environments are declared.

## Proposal Template

When presenting the proposed graph to the user, structure it as:

```
SCAN RESULTS
  Projects: Api (csproj), Worker (csproj)
  Frontends: web (Next.js)
  External deps: Postgres (compose), Redis (compose)
  Connection strings hardcoded in: Api/appsettings.Development.json

PROPOSED RESOURCE GRAPH
  - pg (Postgres)
    - appdb (database)
  - cache (Redis)
  - api (Project) → references pg, cache; waits for both; external HTTP
  - worker (Project) → references pg
  - web (Next.js) → references api; waits for api; WithBrowserLogs

QUESTIONS BEFORE I EDIT
  1. Replace appsettings Postgres connection string with Aspire service discovery? [Y/n]
  2. Mark Api's /admin endpoint as ExcludeReferenceEndpoint? [Y/n]
  3. Bind everything to a default compute environment, or wait for deploy? [skip/aca/aks]
```

Wait for confirmation before editing.
