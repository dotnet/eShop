# Diagnostics Bridge — Local vs Deployed

> **Purpose**: Route diagnostics requests to the correct tool based on where the application is running.

## Decision Flowchart

```
Is the request about AppHost code or deployment definition?
│
├── YES → Route to aspire-deployment skill (and aspireify for code edits)
│
└── NO → Is the app running locally (via aspire start)?
    │
    ├── YES → Use Aspire CLI
    │   ├── Console logs       → aspire logs <resource>
    │   ├── Structured logs    → aspire otel logs
    │   ├── Traces             → aspire otel traces
    │   ├── Spans              → aspire otel spans
    │   ├── Resource state     → aspire describe   (add --include-hidden if missing)
    │   ├── Export telemetry   → aspire export
    │   ├── Filter by trace    → aspire otel logs --trace-id <id> (verify flag)
    │   └── Standalone dash    → aspire dashboard run  (foreground/blocking)
    │
    └── NO (deployed) → Route by target
        ├── Azure Kubernetes Service (AKS)
        │   ├── Pod logs           → kubectl logs <pod>
        │   ├── Pod / workload state → kubectl describe pod <pod>, kubectl get pods
        │   ├── Cluster Azure resources → azure-diagnostics skill
        │   └── Cluster-wide telemetry → Azure Monitor Container Insights
        │
        ├── Other Azure (App Service, Container Apps) → azure-diagnostics skill
        │   ├── App logs           → az containerapp logs show / az webapp log tail
        │   ├── Metrics            → az monitor metrics list
        │   ├── App Insights       → az monitor app-insights query
        │   ├── Resource health    → az resource show / AppLens
        │   └── Front Door / NSP / private endpoint → azure-diagnostics
        │
        └── Docker / Compose → Use Docker tooling
            ├── Container logs     → docker logs <container>
            ├── Service logs       → docker compose logs <service>
            └── Resource state     → docker ps / docker compose ps
```

---

## Local Development — Full Aspire CLI Support

When the app is running locally via `aspire start`, the Aspire CLI provides complete observability:

### How It Works

The Aspire CLI communicates with the running AppHost through a **backchannel socket** at `~/.aspire/backchannels/`. This is a local-only IPC mechanism — it cannot connect to remote instances.

### Available Commands

| Command | Purpose | Example |
|---------|---------|---------|
| `aspire logs <resource>` | Console stdout/stderr from a resource | `aspire logs apiservice` |
| `aspire logs --follow` | Stream logs in real-time | `aspire logs apiservice --follow` |
| `aspire otel logs` | Structured OpenTelemetry log records | `aspire otel logs` |
| `aspire otel traces` | Distributed trace data | `aspire otel traces` |
| `aspire otel spans` | Individual span-level detail | `aspire otel spans` |
| `aspire otel logs --trace-id <id>` | Logs correlated to a specific trace (⚠️ verify flag in your version) | `aspire otel logs --trace-id abc123` |
| `aspire otel logs --dashboard-url` | Query a standalone or deployed dashboard | `aspire otel logs --dashboard-url "https://localhost:18888/login?t=TOKEN"` |
| `aspire describe` | Resource state, endpoints, health (filtered) | `aspire describe --format Json` |
| `aspire describe --include-hidden` | Include hidden resources (proxies, helpers, migrations) | `aspire describe --include-hidden --format Json` |
| `aspire ps --include-hidden --format Json` | Resource list including hidden resources | `aspire ps --include-hidden --format Json` |
| `aspire export` | Export portable telemetry bundle | `aspire export` |
| `aspire dashboard run` | Run the Aspire Dashboard standalone (foreground/blocking) | `aspire dashboard run` |

### Tips for Agents

```bash
# ✅ Always use --format Json for machine parsing
aspire describe --format Json

# ✅ When an expected resource is missing, retry with --include-hidden
#    Hidden-by-default resources (proxies, helper containers, migrations)
aspire ps --include-hidden --format Json

# ✅ Get endpoints from describe, not guessing ports
ENDPOINT=$(aspire describe apiservice --format Json | jq -r '.endpoints[0].url')

# ✅ Correlate logs to a specific request
aspire otel logs --trace-id <trace-id-from-otel-traces>
```

---

## Standalone Dashboard — `aspire dashboard run`

`aspire dashboard run` runs the Aspire Dashboard without an AppHost — point any OTLP-emitting application at it (Aspire or not) and telemetry shows up live.

```bash
aspire dashboard run
# Dashboard:  http://localhost:18888/login?t=<TOKEN>
# OTLP/gRPC:  http://localhost:4317
# OTLP/HTTP:  http://localhost:4318
```

> ⚠️ **Foreground / blocking.** This command does not return until you stop it (Ctrl-C). Agents must start it as a long-running background process (e.g., bash `mode="async"`), capture the dashboard URL and `t=` token from initial output, and leave it running. Do **not** treat it as a one-shot synchronous command.

### Connect the CLI to a standalone dashboard

The `aspire otel logs` and `aspire otel traces` commands accept `--dashboard-url` (and `--api-key` when the dashboard is configured with API-key auth) so the CLI can query a standalone dashboard without an AppHost.

The simplest form passes the full login URL printed by `aspire dashboard run` — the CLI normalizes login URLs automatically:

```bash
# Stream structured logs (login URL form — token in URL)
aspire otel logs --dashboard-url "http://localhost:18888/login?t=TOKEN" --follow

# Search recent traces
aspire otel traces --dashboard-url "http://localhost:18888/login?t=TOKEN"
```

For dashboards that use a separate API key (e.g., the standalone container image with API-key auth configured), pass `--api-key` alongside the base URL:

```bash
aspire otel logs --dashboard-url https://my-dashboard.example.com --api-key "$DASHBOARD_API_KEY" --follow
```

The container-image standalone dashboard is still available where the CLI isn't an option.

---

## Browser Telemetry (`Aspire.Hosting.Browsers`)

The `Aspire.Hosting.Browsers` integration captures **browser console logs, network requests, and screenshots** from frontend resources during local development. Frontend resources opt in by calling `WithBrowserLogs()` in the AppHost. The data shows up in the dashboard alongside server-side telemetry.

| Need | Action |
|------|--------|
| Inspect existing browser telemetry | Open the dashboard or run `aspire otel logs <frontend-resource>` |
| Check whether a frontend has it enabled | Look for `.WithBrowserLogs()` in the AppHost |
| Add `WithBrowserLogs()` to a resource | → **`aspireify` skill** (AppHost authoring) |

---

## Deployed Applications — Routing

### Why Aspire CLI Cannot Help Directly

The Aspire CLI's `aspire logs`, `aspire describe`, and other backchannel commands use the local backchannel socket at `~/.aspire/backchannels/`. This is **by design** — there is no remote backchannel. When an app is deployed, the Aspire CLI cannot reach it directly.

**Exception:** if a Dashboard is reachable (deployed alongside the app, or running standalone), `aspire otel logs --dashboard-url` and `aspire otel traces --dashboard-url` (with `--api-key` when the dashboard requires it) can query it remotely. This does **not** extend to `aspire logs` or `aspire describe`.

```bash
# Limited remote support via deployed Dashboard — login URL form
aspire otel logs --dashboard-url "https://my-dashboard.azurecontainerapps.io/login?t=TOKEN"
aspire otel traces --dashboard-url "https://my-dashboard.azurecontainerapps.io/login?t=TOKEN"

# Or with separate API-key auth
aspire otel logs --dashboard-url https://my-dashboard.azurecontainerapps.io --api-key "$DASHBOARD_API_KEY"
```

### Three-way deployed routing

| Target | Use | Examples |
|--------|-----|----------|
| **AKS workload (pod logs, pod state, container insights)** | `kubectl` + Azure Monitor Container Insights | `kubectl logs <pod>`, `kubectl describe pod <pod>`, Container Insights queries |
| **Azure resource health** (App Insights, Front Door, NSP, private endpoint, ACA, App Service) | `azure-diagnostics` skill (azure-skills) | `az containerapp logs show`, `az monitor app-insights query`, AppLens |
| **Docker / Compose** | Docker CLI | `docker logs <container>`, `docker compose logs <service>` |

### azure-diagnostics — quick reference

| Need | azure-diagnostics Approach |
|------|---------------------------|
| Application logs | `az containerapp logs show --name APP -g RG --follow` |
| Metrics | `az monitor metrics list --resource RESOURCE_ID` |
| App Insights queries | `az monitor app-insights query --analytics-query "KQL"` |
| Resource health | AppLens MCP tool or `az resource show` |
| Activity log | `az monitor activity-log list -g RG` |
| Front Door / NSP / private endpoint | `az network front-door`, `az network perimeter`, AppLens |

### Production Telemetry — Automatic Configuration

Aspire auto-configures Application Insights when `AddAzureApplicationInsights()` is used in the AppHost. Deployed apps export OpenTelemetry data to App Insights automatically, providing:

- Request traces and dependency tracking
- Exception logging
- Performance metrics
- Live Metrics stream
- Application Map (service topology)

No additional configuration is needed — Aspire wires the connection string during deployment.

## Known Diagnostics Issues

| Issue | Symptom | Workaround |
|-------|---------|-----------|
| TS AppHost DNS failure ([#15782](https://github.com/microsoft/aspire/issues/15782)) | `aspire otel` returns "No such host" for `*.dev.localhost` | Use `--dashboard-url localhost:PORT` directly |
| `--isolated` mode telemetry ([#16107](https://github.com/microsoft/aspire/issues/16107)) | OTEL port not randomized in isolated mode | Avoid `--isolated` if telemetry is needed |
| Resource missing from `aspire ps` / `aspire describe` | Hidden-by-default resources such as proxies, helpers, or migrations | Re-run with `--include-hidden` |

> **Resolved in 13.3**: The standalone-dashboard workaround for [#16236](https://github.com/microsoft/aspire/issues/16236) is obsolete — `aspire dashboard run` ships in-box (see Standalone Dashboard section above).

---

## Summary: Where to Look

| Question | Local Dev | Deployed |
|----------|-----------|----------|
| "What's the status of my resources?" | `aspire describe` (try `--include-hidden` if missing) | Azure Portal / `az containerapp show` / `kubectl describe pod` |
| "Show me the logs" | `aspire logs <resource>` | `az containerapp logs show` / `kubectl logs <pod>` / `docker logs` |
| "Show me distributed traces" | `aspire otel traces` | App Insights → Transaction Search |
| "Why is this resource unhealthy?" | `aspire describe` + `aspire logs` | AppLens / azure-diagnostics / `kubectl describe pod` |
| "What metrics are available?" | Aspire Dashboard (auto-launched or `aspire dashboard run`) | Azure Monitor / App Insights / Container Insights |
| "Export telemetry for analysis" | `aspire export` | App Insights export / KQL query |
| "Browser console / network logs" | Dashboard (with `WithBrowserLogs()` enabled) — N/A in production |
