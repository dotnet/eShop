---
name: aspire-monitoring
description: >-
  **ANALYSIS SKILL** - Observe Aspire apps: logs, traces, metrics, resource state,
  telemetry export, browser telemetry, and the standalone dashboard. Routes between local
  Aspire CLI, AKS workload diagnostics, and deployed Azure resource health.
  USE FOR: aspire logs, aspire otel logs, aspire otel traces, aspire otel spans, aspire
  describe, aspire ps, aspire export, aspire dashboard run, --include-hidden, browser logs
  in dashboard, WithBrowserLogs, App Insights query, AKS pod logs, container app logs.
  DO NOT USE FOR: start/stop/wait (use aspire-orchestration), deploy/publish/destroy (use
  aspire-deployment), AppHost code edits like WithBrowserLogs() (use aspireify), Azure
  provisioning (use azure-prepare).
  INVOKES: aspire CLI, azure-diagnostics (deployed Azure), kubectl + Container Insights.
  FOR SINGLE OPERATIONS: Run the aspire CLI command directly for quick log or describe lookups.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

# Aspire Monitoring

> Aspire CLI provides full observability **locally**. For deployed apps, route to platform-specific tools.

## Diagnostics Bridge — Where To Look

| Need | Environment | Tool | Command / Route |
|------|------------|------|-----------------|
| Console logs | Local dev | Aspire CLI | `aspire logs <resource>` |
| Structured logs | Local dev | Aspire CLI | `aspire otel logs [resource]` |
| Distributed traces | Local dev | Aspire CLI | `aspire otel traces [resource]` |
| Span detail | Local dev | Aspire CLI | `aspire otel spans [resource]` |
| Resource state | Local dev | Aspire CLI | `aspire describe` (add `--include-hidden` if a resource is missing) |
| Telemetry export | Local dev | Aspire CLI | `aspire export [resource]` |
| Standalone dashboard | Any (no AppHost) | Aspire CLI | `aspire dashboard run` (foreground/blocking — see below) |
| Browser console / network / screenshots | Local dev (frontend) | Aspire dashboard | Surfaced via `Aspire.Hosting.Browsers` + `WithBrowserLogs()` |
| AppHost / deployment definition | Authoring | aspire-deployment skill | → `aspire-deployment` skill |
| AKS workload (pod logs, pod state) | Deployed AKS | kubectl + Container Insights | `kubectl logs <pod>`, `kubectl describe pod <pod>`, Container Insights in Azure Monitor |
| Azure resource health (App Insights, Front Door, NSP, private endpoint) | Deployed Azure | azure-diagnostics | → `azure-diagnostics` skill |
| App Service / Container Apps logs | Deployed Azure | azure-diagnostics | → `azure-diagnostics` skill |
| Logs/state | Deployed Docker / Compose | Docker CLI | `docker logs <container>`, `docker compose logs <service>` |

**Decision tree:**

1. Is this about **AppHost code or deployment definition**? → `aspire-deployment` skill.
2. Is the app **running locally** via `aspire start`? → Aspire CLI.
3. Is it deployed to **AKS**? → kubectl + Container Insights for workload; `azure-diagnostics` for the cluster's Azure resources.
4. Is it deployed to **other Azure** (App Service, Container Apps)? → `azure-diagnostics`.
5. Is it deployed to **Docker / Compose**? → `docker` / `docker compose` CLI.

See [diagnostics-bridge.md](references/diagnostics-bridge.md) for detailed routing.

## Investigation Workflow

When something is wrong, investigate before editing code:

1. `aspire describe` — check resource state and endpoints
2. `aspire otel logs <resource>` — structured logs first
3. `aspire logs <resource>` — console output as secondary view
4. `aspire otel traces <resource>` — cross-service activity
5. `aspire export` — zipped telemetry snapshot for deeper analysis

## Local Commands Reference

| Command | Purpose | Example |
|---------|---------|---------|
| `aspire logs <resource>` | Console stdout/stderr | `aspire logs apiservice` |
| `aspire logs --follow` | Stream logs in real-time | `aspire logs apiservice --follow` |
| `aspire otel logs` | Structured OpenTelemetry logs | `aspire otel logs` |
| `aspire otel traces` | Distributed trace data | `aspire otel traces` |
| `aspire otel spans` | Individual span detail | `aspire otel spans` |
| `aspire otel logs --trace-id <id>` | Logs correlated to trace (⚠️ verify flag) | `aspire otel logs --trace-id abc123` |
| `aspire otel logs --dashboard-url` | Query a standalone dashboard (login URL or base URL + `--api-key`) | `aspire otel logs --dashboard-url "http://localhost:18888/login?t=TOKEN" --follow` |
| `aspire otel traces --dashboard-url` | Query a standalone dashboard | `aspire otel traces --dashboard-url "http://localhost:18888/login?t=TOKEN"` |
| `aspire describe` | Resource state, endpoints, health | `aspire describe --format Json` |
| `aspire describe --include-hidden` | Include proxies, helper containers, migrations | `aspire describe --include-hidden --format Json` |
| `aspire ps --format Json` | Resource list with state (filtered) | `aspire ps --format Json` |
| `aspire ps --include-hidden --format Json` | Resource list with hidden resources | `aspire ps --include-hidden --format Json` |
| `aspire export` | Portable telemetry bundle | `aspire export` |
| `aspire dashboard run` | Standalone dashboard (foreground/blocking) | `aspire dashboard run` |

### Hidden resources are filtered by default

`aspire ps`, `aspire describe`, and other CLI commands filter out resources marked hidden in the AppHost (proxies, helper containers, migrations). The default output is correct for normal app inspection. Add `--include-hidden` when:

- Debugging proxies, sidecar/helper containers, or migration jobs.
- An expected resource is "missing" from `aspire ps` / `aspire describe`.
- Triaging connectivity or wiring issues that may involve infrastructure resources.

### Tips for Agents

```bash
# ✅ Use --format Json for machine parsing (supported: ps, describe, start)
aspire describe --format Json

# ✅ When a resource you expect is missing, retry with --include-hidden
aspire ps --include-hidden --format Json | jq '.[] | {name, displayName, state, hidden}'

# ✅ Get endpoints from describe, not guessing ports
ENDPOINT=$(aspire describe apiservice --format Json | jq -r '.endpoints[0].url')

# ✅ Use --apphost <path> when multiple AppHosts exist
aspire describe --apphost ./src/MyApp.AppHost/
```

## Known Diagnostics Issues

| Issue | Symptom | Workaround |
|-------|---------|-----------|
| TS AppHost DNS failure ([#15782](https://github.com/microsoft/aspire/issues/15782)) | `aspire otel` "No such host" for `*.dev.localhost` | Use `--dashboard-url localhost:PORT` |
| `--isolated` mode telemetry ([#16107](https://github.com/microsoft/aspire/issues/16107)) | OTEL port not randomized in isolated mode | Avoid `--isolated` if telemetry is needed |
| Resource missing from `aspire ps` / `aspire describe` | Hidden-by-default resources such as proxies, helpers, or migrations | Re-run with `--include-hidden` |

> **Resolved in 13.3**: The standalone-dashboard workaround for [#16236](https://github.com/microsoft/aspire/issues/16236) is obsolete — use `aspire dashboard run` (see below).

## Standalone Dashboard (`aspire dashboard run`)

`aspire dashboard run` launches the Aspire Dashboard without an AppHost, so any OTLP-emitting application (Aspire or not) can stream telemetry into it.

```bash
aspire dashboard run
# Dashboard:  http://localhost:18888/login?t=<TOKEN>
# OTLP/gRPC:  http://localhost:4317
# OTLP/HTTP:  http://localhost:4318
```

> ⚠️ **Foreground / blocking.** `aspire dashboard run` does **not** return until you stop it (Ctrl-C). Agents must treat it as a long-running background process — start it with the bash tool's `mode="async"`, capture the dashboard URL and token from initial output, and leave it running. Do **not** invoke it as a one-shot synchronous command, and do **not** wait for it to "finish".

### Connect the Aspire CLI to a standalone dashboard

`aspire otel logs` and `aspire otel traces` accept `--dashboard-url`. The simplest form passes the full login URL printed by `aspire dashboard run` — the CLI normalizes it automatically:

```bash
# Stream structured logs from a standalone dashboard (login URL form)
aspire otel logs --dashboard-url "http://localhost:18888/login?t=TOKEN" --follow

# Search recent traces
aspire otel traces --dashboard-url "http://localhost:18888/login?t=TOKEN"
```

For dashboards configured with API-key authentication (e.g., the standalone container image with a separate API key), pass `--api-key` alongside the base `--dashboard-url`:

```bash
aspire otel logs --dashboard-url https://my-dashboard.example.com --api-key "$DASHBOARD_API_KEY" --follow
```

The container-image standalone dashboard still works for environments where the CLI isn't available.

## Browser Telemetry (`Aspire.Hosting.Browsers`)

The `Aspire.Hosting.Browsers` integration captures **browser console logs, network requests, and screenshots** from frontend resources during local development and surfaces them in the dashboard alongside server-side telemetry. Frontend resources opt in via `WithBrowserLogs()`.

| Need | Action |
|------|--------|
| Inspect browser telemetry that is already wired | Open the dashboard; browser logs / network / screenshots appear next to server telemetry for the resource |
| Confirm a frontend has it enabled | Check the AppHost for `.WithBrowserLogs()` on the resource (e.g., `AddViteApp("frontend").WithBrowserLogs()`) |
| Add `WithBrowserLogs()` to a resource | → **`aspireify` skill** (AppHost authoring) — do not edit the AppHost from this skill |

When parsing telemetry programmatically, browser logs surface as additional OTLP log records associated with the frontend resource — `aspire otel logs <frontend-resource>` returns them alongside server logs.

## Dashboard UX Features

Agents inspecting a running dashboard should know:

- **Notification center** (bell icon, top-right) — surfaces results of resource commands and lifecycle events. Inline command responses appear here instead of being scraped from the logs panel.
- **Rebuild command** — available on container and project resources; rebuilds the image and restarts the resource without restarting the whole AppHost. Result lands in the notification center.
- **Structured command results** — custom resource commands return `ExecuteCommandResult` with a `Message` payload that the dashboard renders inline; HTTP commands set `HttpCommandResultMode.Auto | Json | Text | None` to control how the response body is shown.

> Authoring custom commands or `WithBrowserLogs()` calls is AppHost work — route to **`aspireify`**. This skill is for *observing* what those features surface in the dashboard.

## Why Aspire CLI Can't Do Remote Diagnostics

The Aspire CLI talks to a *running AppHost* through a local backchannel socket at `~/.aspire/backchannels/`. This is **by design** — there is no remote backchannel. For deployed apps, route to platform-specific tools (azure-diagnostics, kubectl, docker).

**Exception**: if a Dashboard is reachable (deployed alongside the app, or running standalone), `aspire otel logs` and `aspire otel traces` can query it via `--dashboard-url` (login URL form) and optional `--api-key` (see the Standalone Dashboard section above). This does **not** apply to `aspire logs` or `aspire describe`.

## Handoff Rules

| Scenario | Route To |
|----------|----------|
| Start/stop/wait/rebuild lifecycle | → `aspire-orchestration` skill |
| Deploy, publish, pipeline steps, AppHost compute environment binding | → `aspire-deployment` skill |
| AppHost code changes (`WithBrowserLogs()`, custom commands, `WithHttpCommand`) | → `aspireify` skill |
| Deployed Azure resource health (App Insights, Front Door, NSP, private endpoint, ACA, App Service) | → `azure-diagnostics` skill (azure-skills) |
| AKS workload diagnostics (pod logs, pod state, Container Insights) | → `kubectl` + Azure Monitor Container Insights |
| Docker / Compose container logs | → `docker logs` / `docker compose logs` |

## Project-Local Skill Routing

If `.agents/skills/aspire/SKILL.md` exists (from `aspire agent init`), see its
`references/monitoring.md` for deeper telemetry workflow guidance.

## References

- [diagnostics-bridge.md](references/diagnostics-bridge.md) — Local vs deployed routing detail
- [monitoring.md](references/monitoring.md) — Telemetry inspection and export patterns
- [playwright-handoff.md](references/playwright-handoff.md) — Find the correct Aspire frontend URL before browser testing
