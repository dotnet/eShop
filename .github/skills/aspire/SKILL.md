---
name: aspire
description: >-
  **WORKFLOW SKILL** - Top-level router for Aspire 13.4 distributed apps. Detects the
  AppHost, enforces safety guardrails, and routes to the right sub-skill.
  USE FOR: Aspire AppHost detected, aspire CLI, distributed app, cloud-native .NET,
  aspire start, aspire stop, aspire resource, aspire deploy, aspire destroy, aspire publish,
  aspire init, aspire new, aspire add, aspire integration list/search, aspire wait,
  aspire describe, aspire ps, aspire dashboard run, aspire doctor, aspire update,
  aspire logs, aspire otel, --include-hidden, aspireify, WithBrowserLogs, custom
  dashboard/resource commands, .aspire/modules recovery, Playwright URL discovery.
  DO NOT USE FOR: non-Aspire .NET projects (use dotnet directly), Azure provisioning
  without Aspire (use azure-prepare), container-only repos with no AppHost, ordinary
  build/test tasks.
  INVOKES: aspire-init, aspireify, aspire-orchestration, aspire-deployment, aspire-monitoring.
  FOR SINGLE OPERATIONS: Route directly to the matching sub-skill.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

# Aspire

Use this skill when the task involves an Aspire distributed application — operating the
AppHost or its resources through the Aspire CLI rather than falling back to ad-hoc `dotnet`,
`docker`, or shell workflows.

## Detection

Activate when ANY signal is present. Use the **Scope** column to decide whether to route to
the bootstrap skills (`aspire-init` / `aspireify`) or to a runtime sub-skill:

| Signal | How to Detect | Confidence | Scope |
|--------|---------------|------------|-------|
| C# AppHost | `.csproj` containing `Aspire.AppHost.Sdk` | ✅ Definitive | AppHost present → orchestration / deployment / monitoring |
| File-based C# AppHost | `apphost.cs` with `#:sdk Aspire.AppHost.Sdk` | ✅ Definitive | AppHost present → orchestration / deployment / monitoring |
| TypeScript AppHost | `apphost.ts` file in project | ✅ Definitive | AppHost present → orchestration / deployment / monitoring |
| Aspire config without AppHost | `aspire.config.json` present **and no AppHost** above | High | Bootstrap → `aspireify` (skeleton dropped, needs wiring) |
| Aspire config with AppHost | `aspire.config.json` present **and** AppHost above | High | AppHost present → orchestration / deployment / monitoring |
| Aspire settings | `.aspire/` directory present | High | AppHost present (usually) |
| Generated TS modules | `.aspire/modules/` directory present | High | AppHost present (TS) |
| Service defaults | `Aspire.ServiceDefaults` in project references | Medium | AppHost present |
| **No AppHost, no `aspire.config.json`** | None of the above and user asks to add Aspire | n/a | Bootstrap → `aspire-init` (skeleton drop) |

## Default Workflow

0. **Bootstrap branch** — if **no AppHost exists** in the repo, route to
   [`aspire-init`](../aspire-init/SKILL.md) for the skeleton drop. If an AppHost stub exists
   but is **unwired** (no resources declared), route to [`aspireify`](../aspireify/SKILL.md).
   Only continue with the steps below once a wired AppHost is present.
1. Confirm workspace is Aspire — identify the AppHost
2. `aspire start` (or `aspire start --isolated` in worktrees or whenever shared local state is risky)
3. `aspire wait <resource>` before interacting with any resource
4. Inspect state with `aspire describe`, `aspire otel logs`, `aspire logs`, `aspire otel traces`, and `aspire export` before making code changes
5. Before adding integrations, use `aspire integration search <query>` when the package is unknown, then `aspire add <package>` when ready to mutate the AppHost
6. When code changes, decide whether the AppHost model changed or only one resource changed. Re-run `aspire start` after AppHost changes; otherwise prefer resource commands, runtime watch/HMR, dashboard actions, or IDE-managed debugging as appropriate.

## Key Rules

- **Always** `aspire start`, **never** `dotnet run` on AppHosts
- **Always** `aspire wait <resource>`, **never** manual HTTP polling
- Use `aspire resource <resource-name> <command>` for resource operations such as `stop`, `start`, or `rebuild` when available
- Do not stop or restart the whole AppHost just because one resource changed
- Use `features.defaultWatchEnabled` only for Aspire default watch; do not treat it as per-resource rebuild, restart, or hot reload
- Prefer a resource's own framework/runtime hot reload, HMR, or watch workflow when it already handles the change
- **Always** `aspire docs search <topic>` before editing unfamiliar AppHost APIs
- **Always** `aspire docs api search <query> --language csharp|typescript` for API reference before editing AppHost code
- **Always** `--non-interactive` for agent execution
- Use `aspire integration list --format Json` and `aspire integration search <query> --format Json` for read-only integration discovery
- **Never** install the obsolete Aspire workload
- **Never** edit `.aspire/modules/` directly in TypeScript AppHosts

## Routing

| Task | Route To |
|------|----------|
| Start, stop, wait, restart, rebuild | → [aspire-orchestration](../aspire-orchestration/SKILL.md) |
| Create a new Aspire project from a template (`aspire new`) | → [aspire-init](../aspire-init/SKILL.md) (in-plugin) |
| Add Aspire to an existing repo (`aspire init`, drop skeleton) | → [aspire-init](../aspire-init/SKILL.md) (in-plugin) |
| Wire AppHost / scaffold resource graph / add integrations after `aspire init` | → [aspireify](../aspireify/SKILL.md) (in-plugin) |
| Deploy, publish, destroy, pipeline steps | → [aspire-deployment](../aspire-deployment/SKILL.md) |
| Logs, traces, metrics, dashboard, browser logs | → [aspire-monitoring](../aspire-monitoring/SKILL.md) |
| Deployed app monitoring (Azure) | → `azure-diagnostics` skill (azure-skills plugin) |

## Sub-Skills

### aspire-init
First-run flow only. Owns the skeleton drop for repos that do **not** yet have an AppHost —
picks `aspire new <template>` (greenfield) or `aspire init` (existing repo), runs the CLI,
and hands off to `aspireify` for the actual wiring. Self-deactivates once the skeleton is in
place. Do **not** use it on a repo that already contains an AppHost.

### aspireify
Agentic AppHost wiring after `aspire init` lands the skeleton. Scans the repo, proposes a
resource graph (Postgres / Redis / Rabbit / etc.), edits the AppHost (C#, file-based C#, or
TypeScript), wires `Aspire.ServiceDefaults` + OTel, validates with `aspire start`, then
self-deactivates. Owns current AppHost authoring patterns (`AddNextJsApp`, `AddViteApp`,
`WithBrowserLogs()`, generated `.aspire/modules/`, unified TS `withEnvironment`,
endpoint references, and config/secret migration).

### aspire-orchestration
Lifecycle management: start, stop, wait, resource commands, default watch/HMR guidance, and file-lock recovery.
Safety guardrails that prevent agent self-harm. Owns `aspire ps` / `aspire describe` /
`--include-hidden` inspection and CLI upgrades (`aspire update --self`). Does **not** edit
AppHost code — defers to `aspireify` for wiring.

### aspire-deployment
Multi-target deployment and tear-down: `aspire deploy`, `aspire publish`, `aspire destroy`,
`aspire do <step>`. Targets: Azure Container Apps, App Service, AKS, Kubernetes (Helm),
Docker Compose. Owns current deployment surfaces (Front Door, NSP, AKS hosting, Foundry
`AddPromptAgent`, JS `PublishAs*`, `--pipeline-log-level`) and 13.4 API naming.

### aspire-monitoring
Observability: `aspire logs`, `aspire otel`, `aspire describe`, `aspire export`,
`aspire dashboard run`. Routes between local Aspire CLI diagnostics, AKS workload tooling,
and deployed-Azure platform tools. Surfaces dashboard features (notification center,
Rebuild command, browser-logs telemetry).

## Project-Local Skill Override

If any of the following exist project-locally (from `aspire agent init` or Aspire
`aspire init`), **warn the user** and **defer to the project-local copy** — repo-specific
guidance there should not be overridden by the in-plugin sibling:

| Project-local file | Precedence |
|--------------------|-----------|
| `.agents/skills/aspire/SKILL.md` | This file (top-level router) defers to it for deeper C# / TS AppHost editing, Playwright handoff, investigation workflows. |
| `.agents/skills/aspireify/SKILL.md` | The in-plugin `aspireify` sibling defers to it for AppHost wiring. |
| `.agents/skills/aspire-init/SKILL.md` | The in-plugin `aspire-init` sibling defers to it for the skeleton/first-run flow. |

**Safety guardrails from this plugin always apply** even when project-local skills are
active.

## Prerequisites

| Requirement | Install |
|-------------|---------|
| .NET 10.0 SDK | https://dotnet.microsoft.com/download |
| Aspire CLI (curl/PowerShell) | `curl -sSL https://aspire.dev/install.sh \| bash` |
| Aspire CLI (NativeAOT global tool, .NET 10) | `dotnet tool install -g Aspire.Cli` |

Either install method works. The `dotnet tool install` path produces a NativeAOT binary
(instant startup, no JIT warmup) and is recommended when .NET 10 is already present.

## References

- [aspire-13-3-breaking-changes.md](references/aspire-13-3-breaking-changes.md) — Every 13.3
  breaking change to scrub from agent-generated code, scripts, and CI snippets (rename of
  `--log-level`, dashboard MCP removal, `NameOutput` → `NameOutputReference`,
  `AddAndPublishPromptAgent` removal, TS `withEnvironment*` deprecation, and the full
  13.2 → 13.3 migration checklist).
- [../aspire-orchestration/references/agent-workflows.md](../aspire-orchestration/references/agent-workflows.md) — Common agent workflows: worktrees, code changes, investigation, integrations, TypeScript generated APIs, secrets, deployment, and Playwright handoff.
- [../aspire-orchestration/references/app-commands.md](../aspire-orchestration/references/app-commands.md) — App lifecycle, bootstrap, update, restore, docs, and integration discovery commands.
- [../aspire-orchestration/references/resource-management.md](../aspire-orchestration/references/resource-management.md) — Resource wait and resource-command guidance.
- [../aspire-monitoring/references/monitoring.md](../aspire-monitoring/references/monitoring.md) — App state, logs, traces, search filtering, dashboard links, and export workflows.
- [../aspire-monitoring/references/playwright-handoff.md](../aspire-monitoring/references/playwright-handoff.md) — Playwright handoff after Aspire endpoint discovery.
- [../aspire-deployment/SKILL.md](../aspire-deployment/SKILL.md) — Deployment and pipeline-step workflows.
- [../aspireify/references/apphost-wiring.md](../aspireify/references/apphost-wiring.md) — C# and TypeScript AppHost API lookup and wiring patterns.
