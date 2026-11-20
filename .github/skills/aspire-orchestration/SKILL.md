---
name: aspire-orchestration
description: >-
  **WORKFLOW SKILL** — Manage Aspire AppHost lifecycle and recover from file locks,
  port conflicts, and orphaned processes. WHEN: "start my Aspire app", "aspire start",
  "aspire stop", "aspire wait", "restart the API service", "file lock error",
  "MSB3491", "CS2012", "port already in use", "upgrade Aspire CLI", "aspire update --self",
  "proxies missing in aspire ps", "--include-hidden", "aspire integration list",
  "aspire integration search", "default watch", "hot reload". INVOKES: aspire CLI
  (start, stop, wait, ps, resource, integration, add, init, doctor, update, restore).
  FOR SINGLE OPERATIONS: Run the aspire CLI command directly.
license: MIT
metadata:
  author: Microsoft
  version: "0.0.1"
---

# Aspire Orchestration

> **MANDATORY COMPLIANCE** — This skill prevents agent self-harm in Aspire projects.
> Violating these rules causes file locks, orphaned processes, and user frustration ([#15801](https://github.com/microsoft/aspire/issues/15801)).

## Prerequisites

| Requirement | Install |
|-------------|---------|
| .NET 10.0 SDK | https://dotnet.microsoft.com/download |
| Aspire CLI (curl/PowerShell) | `curl -sSL https://aspire.dev/install.sh \| bash` |
| Aspire CLI (NativeAOT global tool, .NET 10) | `dotnet tool install -g Aspire.Cli` |

Either install method works. The `dotnet tool install` path produces a NativeAOT binary
(instant startup, no JIT warmup) and is the recommended option when .NET 10 is already present.

## Detection

Activate when ANY signal is present:

| Signal | How to Detect | Confidence |
|--------|---------------|------------|
| C# AppHost | `.csproj` containing `Aspire.AppHost.Sdk` | ✅ Definitive |
| File-based C# AppHost | `apphost.cs` or `.cs` file with `#:sdk Aspire.AppHost.Sdk` | ✅ Definitive |
| TypeScript AppHost | `apphost.ts` file in project | ✅ Definitive |
| Aspire config | `aspire.config.json` in project root | High |
| Aspire settings | `.aspire/` directory present | High |
| Generated TS modules | `.aspire/modules/` directory present | High |
| Service defaults | `Aspire.ServiceDefaults` in project references | Medium |

See [detection.md](references/detection.md) for detailed fingerprinting.

## Safety Guardrails

| Situation | ✅ ALWAYS Do | ❌ NEVER Do |
|-----------|-------------|------------|
| Start an Aspire app | `aspire start` | `dotnet run` on AppHost |
| Wait for resource ready | `aspire wait <resource>` | `curl` / HTTP polling loops |
| Code changed in a resource | Prefer resource commands, runtime watch/HMR, dashboard actions, or IDE-managed debugging | `dotnet build` against locked files |
| Task complete | `aspire stop` | Leave processes running |
| Check resource status | `aspire describe` / `aspire ps` | Manual process inspection |
| Working in git worktree | `aspire start --isolated` | `aspire start` without isolation |
| Running from AI agent | Add `--non-interactive` to all commands | Assuming interactive terminal |
| Editing unfamiliar API | `aspire docs search <topic>` then `aspire docs api search <query>` for API reference | Guessing API shape |
| C# AppHost API inspection | Use `dotnet-inspect` skill (if available) for local symbols | Guessing overloads or builder chains |
| Adding custom dashboard/resource commands | `aspire docs search "custom resource commands"` first | Inventing `WithCommand` patterns without docs |
| Installing Aspire support | Use `aspire add` or `aspire init` | ~~`dotnet workload install aspire`~~ (obsolete) |

See [safety-guardrails.md](references/safety-guardrails.md) for detailed rules and recovery patterns.

## Default Workflow

1. Confirm workspace is Aspire — identify the AppHost
2. `aspire start` (or `aspire start --isolated` in worktrees)
3. `aspire wait <resource>` before interacting with any resource
4. `aspire describe` to inspect state, then work
5. If AppHost code changed, rerun `aspire start`; if only one resource changed, prefer the resource's commands/watch/HMR/debug workflow
6. `aspire stop` when cleanup is explicitly requested or needed to release locks/ports

## Quick Reference

| Task | Command |
|------|---------|
| Start app (agents) | `aspire start` (background, preferred) |
| Start app (human) | `aspire run` (foreground, dashboard) |
| Stop app | `aspire stop` |
| Wait for resource | `aspire wait <resource>` |
| Check status | `aspire ps` or `aspire describe` |
| Show hidden resources (proxies, helpers, migrations) | `aspire ps --include-hidden` / `aspire describe --include-hidden` |
| Resource operation | `aspire resource <resource-name> <command>` such as `stop`, `start`, or `rebuild` when exposed |
| Create new project | `aspire new aspire-starter` |
| Add Aspire to existing | `aspire init` (then hand off to `aspireify` skill for wiring) |
| Add integration | `aspire add <package>` |
| Discover integrations | `aspire integration list --format Json` / `aspire integration search <query> --format Json` |
| Upgrade the CLI itself | `aspire update --self` |
| Update project package refs | `aspire update` (modifies project files — get user approval) |
| Restore generated files | `aspire restore` |
| Environment maintenance | `aspire cache clear`, `aspire certs trust`, `aspire certs clean` |
| Diagnose environment | `aspire doctor` |
| Machine-readable output | `--format Json` (supported: `ps`, `describe`, `start`) |
| Look up API reference | `aspire docs api search <query> --language csharp\|typescript` |
| Browse API entries | `aspire docs api list <scope>` |
| Get API detail | `aspire docs api get <id>` |

## Error Handling

| Symptom | Cause | Action |
|---------|-------|--------|
| **File lock errors during build (`MSB3491`, `CS2012`)** | **Aspire is running and holds locks on `bin/`, `obj/`, and assemblies.** | **Run `aspire stop` first**, then rebuild or `aspire start`. Do NOT conclude the project has a permanent build failure. |
| "Port already in use" | Previous instance running | `aspire stop`, then `aspire start` |
| Resource not found | App not started or name wrong | `aspire ps` to check |
| Build errors in resource | Code error, not Aspire issue | Fix code, then use resource commands/watch/HMR/debug workflow or rerun `aspire start` if AppHost code changed |
| Environment issues | Missing SDK or tools | `aspire doctor` to diagnose |
| JSON parse failure from `aspire start` | Mixed human/JSON output ([#15843](https://github.com/microsoft/aspire/issues/15843)) | Strip non-JSON lines before parsing |
| `aspire wait` rejects name | Use `displayName` not `name` ([#15842](https://github.com/microsoft/aspire/issues/15842)) | Use `displayName` from `aspire ps --format Json` |
| `aspire ps` hangs | AppHost on breakpoint ([#15576](https://github.com/microsoft/aspire/issues/15576)) | Use timeout, check AppHost process |
| `aspire agent init` fails | Non-interactive terminal ([#16264](https://github.com/microsoft/aspire/issues/16264)) | Run from standard terminal |
| Docker daemon unavailable | Container-backed resources fail to start | Start Docker Desktop, then `aspire start` |
| Multiple AppHosts detected | Wrong AppHost targeted | Use `--apphost <path>` to specify explicitly |

### 🔒 File-Lock Recovery (MSB3491 / CS2012) — Always `aspire stop` First

When a build fails with `error MSB3491: Could not write to output file ...` or
`error CS2012: Cannot open ... for writing`, the project itself is healthy —
**Aspire is running and holding file locks** on the resource's output assemblies.
The recovery is always the same:

```bash
# ✅ Correct recovery sequence
aspire stop              # release the locks
# ... then either rebuild / restart one resource if the resource exposes commands ...
aspire resource <name> rebuild   # example: C# project resource with rebuild command
# ... or restart the whole AppHost ...
aspire start             # if AppHost code changed or Aspire was already stopped
```

| ❌ NEVER do | ✅ ALWAYS do |
|------------|-------------|
| Tell the user the project has a permanent build failure | Recognize the lock as Aspire holding outputs and run `aspire stop` |
| `dotnet build` again with locks held | `aspire stop` first, then `dotnet build` (or prefer resource commands/watch/HMR/debug workflow) |
| Delete `bin/` / `obj/` to "fix" the lock | `aspire stop` — deletion may succeed but the next build relocks |
| `pkill dotnet` or `kill <PID>` to free locks | `aspire stop` — clean shutdown via the CLI, no orphans |
| Tell the user to "reboot" or "restart your machine" | `aspire stop` — single command, instant fix |

The same rule applies to any "file in use", "cannot access the file", or
"another process is using" error during a build of an Aspire-managed resource.

## Handoff Rules

| Scenario | Route To |
|----------|----------|
| AppHost wiring after `aspire init` (scan repo, add resources, ServiceDefaults/OTel) | → `aspireify` skill ([`../aspireify/SKILL.md`](../aspireify/SKILL.md)) or project-local `.agents/skills/aspireify/SKILL.md` |
| Browser logs (`Aspire.Hosting.Browsers` / `WithBrowserLogs()`) and dashboard authoring | → `aspireify` skill (code edits) and `aspire-monitoring` (discovery) |
| Custom resource commands (`WithCommand`, `ExecuteCommandResult`, `HttpCommandResultMode`) | → `aspireify` skill |
| Lifecycle hooks (`SubscribeBeforeStart`, `SubscribeAfterResourcesCreated`, BeforeStart pipeline phase) | → `aspireify` skill |
| Endpoint authoring (`WithEndpoint` updates, `ExcludeReferenceEndpoint` flag) | → `aspireify` skill |
| Deploy, publish, pipeline steps, `aspire destroy` | → `aspire-deployment` skill |
| Logs, traces, metrics, dashboard, `aspire dashboard run` | → `aspire-monitoring` skill |
| Deployed app diagnostics | → `azure-diagnostics` skill (azure-skills) |

## Runtime Settings And Environment

| Variable | Default | Purpose |
|----------|---------|---------|
| `ASPIRE_ENABLE_CONTAINER_TUNNEL` | `true` | Container tunnel provides uniform host connectivity across Docker Desktop, Docker Engine, and Podman. Set to `false` to opt out. |
| `ASPIRE_ENVIRONMENT` | unset | Selects the environment-specific config profile — controls which `appsettings.{environment}.json` is loaded and which environment is reported in dashboard telemetry. |
| `ASPIRE_DCP_USE_DEVELOPER_CERTIFICATE` | `true` | The Aspire trusted developer certificate is used by DCP on Windows. Set to `false` to opt out. |
| `features.defaultWatchEnabled` | false unless configured | Enables Aspire default watch for supported C# and TypeScript AppHosts. Do not treat this as per-resource rebuild, restart, or hot reload for resource source changes. |

## TypeScript AppHost Note

Detection covers TS AppHosts (`apphost.ts`), but **all TS AppHost authoring is delegated to `aspireify`**.
Current rules to apply when handing off:

| Rule | Why |
|------|-----|
| Prefer unified `withEnvironment(name, value)` over deprecated per-kind helpers (`withEnvironmentEndpoint`, `withEnvironmentParameter`, `withEnvironmentConnectionString`, `withEnvironmentExpression`, `withEnvironmentFromOutput`, `withEnvironmentFromKeyVaultSecret`) | Per-kind helpers are deprecated — single API now handles all value types |
| Never edit `.aspire/modules/` directly | Generated; use `aspire add <package>` to regenerate and `aspire restore` to recover missing files |
| Use `aspire docs api search <query> --language typescript` for API lookup | TS surface differs from C# |

## Skill Routing — In-Plugin Sibling Skills

After `aspire init` drops a skeleton AppHost + `aspire.config.json`, route AppHost wiring
(scan repo → propose resource graph → edit AppHost → wire `Aspire.ServiceDefaults` / OTel →
validate via `aspire start`) to the in-plugin **aspireify** skill: [`../aspireify/SKILL.md`](../aspireify/SKILL.md).
For first-run flows that only need the skeleton drop, see the in-plugin **aspire-init** skill:
[`../aspire-init/SKILL.md`](../aspire-init/SKILL.md). This orchestration skill stays focused
on lifecycle (start/stop/wait/restart) and never edits AppHost code itself.

## Project-Local Skill Precedence

If `.agents/skills/aspire/SKILL.md` exists (from `aspire agent init`), defer to it for:
C# AppHost editing, TS AppHost editing, Playwright handoff, investigation workflows.
Safety guardrails from this plugin ALWAYS apply.

If `.agents/skills/aspireify/SKILL.md` exists project-locally (installed by `aspire init` in
current Aspire), **warn the user** that a project-local aspireify skill is present and **defer to it**
for AppHost wiring instead of the in-plugin sibling. Same precedence rule as the project-local
`aspire` skill above: project-local wins, plugin guardrails still apply.

## References

- [safety-guardrails.md](references/safety-guardrails.md) — Detailed rules and recovery patterns
- [detection.md](references/detection.md) — Project fingerprinting
- [app-commands.md](references/app-commands.md) — App lifecycle and bootstrap commands
- [resource-management.md](references/resource-management.md) — Resource wait, restart, and operations
- [agent-workflows.md](references/agent-workflows.md) — Common agent investigation, integration, TypeScript, and handoff workflows
