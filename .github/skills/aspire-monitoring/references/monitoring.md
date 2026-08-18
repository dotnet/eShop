# Monitoring

Use this when the task is about inspecting app state, logs, traces, endpoints, or sharable diagnostics.

## Scenario: I Need To Know What Is Running And Where The Endpoints Are

Use these commands when the first job is to inspect current resource state, find URLs, or hand machine-readable app state to another tool.

```bash
aspire describe
aspire resources
aspire describe --apphost <path>
aspire describe --apphost <path> --format Json
```

Keep these points in mind:

- Use `aspire describe` first when you need the current state of the running app before deciding what to do next.
- Use `--apphost <path>` when the workspace has multiple AppHosts or discovery is ambiguous.
- Prefer `--format Json` when another tool or script needs to consume the result, such as a Playwright handoff or endpoint extraction.

## Scenario: Something Is Wrong, But Investigate Before Editing Code

Use these commands when the task is to diagnose behavior in the live app before making code changes.

```bash
aspire otel logs [resource] --format Json
aspire otel traces [resource] --format Json
aspire otel spans [resource] --format Json
aspire otel logs --trace-id <id> --format Json
aspire otel logs [resource] --search "connection timeout"
aspire otel traces [resource] --search "/api/products"
aspire otel logs [resource] --search "severity:error"
aspire otel spans [resource] --search "@http.method:GET duration:>100"
aspire logs [resource]
aspire logs [resource] --search "error"
```

Keep these points in mind:

- Prefer structured telemetry before raw console logs when possible.
- Use `aspire logs` as a secondary console-output view after checking structured telemetry.
- Use the trace-filtered log command when you already have a trace id and want the related log slice.
- Use `--search` to filter results by a case-insensitive text match across all fields (messages, attribute keys/values, trace/span IDs, resource names, severity, scope names). This is the fastest way to narrow output when you know what you're looking for.
- Prefer `--format Json` when another tool or script needs to consume the result, such as a Playwright handoff or endpoint extraction.
- `[resource]` is optional. Include it to filter results to a single resource; omit it to see all resources.
- `--search` can be combined with other options like `--format Json`, `--trace-id`, `--limit`, and resource filtering.

## Filtering

The `--search` option filters output by matching text against log content and trace/span content.

- Multiple words are AND'd — all fragments must match.
- Use `"quoted phrases"` for multi-word fragments: `--search "\"connection timeout\""`.
- Qualifiers support quoted values: `--search "message:\"connection failed\""`.

### Console Logs (`aspire logs --search`)

Matches against log line text content and resource name. Only free-text is supported (no structured qualifiers).

```bash
aspire logs redis --search "timeout"
aspire logs --follow --search "\"connection error\""
```

### Structured Telemetry (`aspire otel logs/traces/spans --search`)

Supports free-text and structured qualifiers in a single query.

**Free-text** matches against message, resource name, scope, trace/span IDs, severity/status, and all attribute keys/values.

**Structured qualifiers** filter on specific fields with `key:value` syntax:

- Log keys: `severity`, `resource`, `scope`, `message`, `trace-id`, `span-id`, `event`
- Span/Trace keys: `name`, `resource`, `scope`, `status`, `kind`, `trace-id`, `span-id`, `duration`
- Custom attributes: `@http.method:GET`, `@db.system:redis`
- Negation: `-severity:debug`, `-@db.system:redis`
- Comparison: `duration:>100`, `duration:>=50`

```bash
aspire otel logs --search "severity:error \"connection failed\""
aspire otel spans --search "@http.method:GET duration:>100 status:error"
aspire otel logs --search "resource:api -severity:debug"
```

## Scenario: I Need A Sharable Diagnostics Bundle

Use this command when you need a portable handoff artifact for deeper analysis or for another person to inspect offline.

```bash
aspire export [resource]
```

Keep these points in mind:

- Use `aspire export` when you need a sharable bundle of telemetry and resource state.
- `[resource]` is optional. Include it to filter the export to a single resource; omit it to export all resources.
- The output is a zip archive (default name `aspire-export-<timestamp>.zip`) containing up to four directories:
  - `resources/` — one JSON file per resource with resource details (name, type, state, endpoints, environment variables, etc.).
  - `consolelogs/` — one plain-text file per resource with raw console output lines.
  - `structuredlogs/` — one JSON file per resource with structured log entries in OTLP format.
  - `traces/` — one JSON file per resource with distributed traces and spans in OTLP format.
- When extracting the export for analysis, look at `resources/` first for an overview, then drill into `consolelogs/`, `structuredlogs/` and `traces/` for detailed diagnostics.

## Dashboard Links

Commands like `aspire describe`, `aspire otel logs`, `aspire otel traces`, and `aspire otel spans` may include dashboard URLs in their JSON output. Only use URLs that are explicitly returned by these commands — do not construct dashboard URLs yourself.

When a dashboard link is returned alongside a resource or telemetry entry, make the resource name, trace ID, or span ID a clickable markdown link using the returned URL.

## Displaying Resources

When showing resource state to the user, display the state text with a circle emoji prefix:

- 🟢 Running, healthy
- 🟡 Starting, waiting
- 🔴 Failed, error, unhealthy
- ⚪ Stopped, exited

Link resource names to their dashboard page when the dashboard URL is known.

## Displaying Telemetry

When showing structured logs, prefix each entry with an emoji matching the log level:

- 🔴 Error / Critical
- 🟡 Warning
- 🔵 Information
- ⚪ Debug / Trace

Link resource names to their dashboard resource page. When trace IDs are present, display the first 7 characters and link that value to the full trace detail page.

When showing traces or spans, use 🟢 for success/unset status and 🔴 for error status. Display only the first 7 characters of trace and span IDs, and link those values to their dashboard detail pages.

## Production Monitoring Strategy (Azure)

Aspire auto-configures Application Insights when `AddAzureApplicationInsights()` is used in the AppHost. Deployed Azure apps export OpenTelemetry to App Insights automatically:

- Request traces and dependency tracking
- Exception logging
- Performance metrics
- Live Metrics stream
- Application Map (service topology)

No additional configuration is needed for Azure once the AppHost and deployment target wire the connection strings during deployment.

> **Docker Compose / Kubernetes**: Auto-configured App Insights does not apply. These targets require platform-native observability (Prometheus, Grafana, ELK, etc.) unless the app is explicitly configured to export OTEL to an external collector.

## Deployed App Monitoring — Route by Target

| Target | Tool | Commands |
|--------|------|----------|
| Azure Container Apps / App Service | azure-diagnostics | `az containerapp logs show`, `az webapp log tail`, App Insights |
| Azure resource health (Front Door, NSP, private endpoint, App Insights) | azure-diagnostics | AppLens, `az monitor app-insights query` |
| AKS workload (pods, workloads) | kubectl + Container Insights | `kubectl logs <pod>`, `kubectl describe pod <pod>`, Azure Monitor Container Insights |
| Docker / Compose | Docker CLI | `docker logs <container>`, `docker compose logs <service>` |

## Standalone Dashboard (`aspire dashboard run`)

`aspire dashboard run` launches the Aspire Dashboard without an AppHost, so any OTLP-emitting application can stream telemetry into it.

```bash
aspire dashboard run
# Dashboard:  http://localhost:18888/login?t=<TOKEN>
# OTLP/gRPC:  http://localhost:4317
# OTLP/HTTP:  http://localhost:4318
```

> **Foreground / blocking.** `aspire dashboard run` does not return until stopped. Agents must treat it as a long-running background process, capture the dashboard URL and token from initial output, and leave it running. Do not invoke it as a one-shot synchronous command, and do not wait for it to "finish".

### Connect the Aspire CLI to a standalone dashboard

`aspire otel logs` and `aspire otel traces` accept `--dashboard-url`. The simplest form passes the full login URL printed by `aspire dashboard run`; the CLI normalizes it automatically:

```bash
aspire otel logs --dashboard-url "http://localhost:18888/login?t=TOKEN" --follow
aspire otel traces --dashboard-url "http://localhost:18888/login?t=TOKEN"
```

For dashboards configured with API-key authentication, pass `--api-key` alongside the base `--dashboard-url`:

```bash
aspire otel logs --dashboard-url https://my-dashboard.example.com --api-key "$DASHBOARD_API_KEY" --follow
```

## Browser Telemetry

Frontend resources opted into `Aspire.Hosting.Browsers` via `WithBrowserLogs()` surface browser console logs, network requests, and screenshots in the dashboard alongside server-side telemetry.

| Need | Action |
|------|--------|
| Inspect browser telemetry that is already wired | Open the dashboard; browser logs / network / screenshots appear next to server telemetry for the resource |
| Confirm a frontend has it enabled | Check the AppHost for `.WithBrowserLogs()` on the resource |
| Add `WithBrowserLogs()` to a resource | Route to `aspireify`; this is AppHost authoring, not monitoring |

## Why Aspire CLI Can't Do Remote Diagnostics

The Aspire CLI talks to a *running AppHost* through a local backchannel socket at `~/.aspire/backchannels/`. This is by design — there is no remote backchannel. For deployed apps, route to platform-specific tools such as azure-diagnostics, kubectl, or Docker.

**Exception**: if a Dashboard is reachable (deployed alongside the app, or running standalone), `aspire otel logs` and `aspire otel traces` can query it via `--dashboard-url` and optional `--api-key`. This does not apply to `aspire logs` or `aspire describe`.
