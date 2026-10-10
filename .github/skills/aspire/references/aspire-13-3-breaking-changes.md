# Aspire 13.3 Breaking Changes — Agent Reference

Single, agent-facing scrub list of every 13.3 breaking change that may affect agent-generated
code, scripts, CI snippets, or skill routing. Source:
[Aspire 13.3 release notes](https://aspire.dev/whats-new/aspire-13-3/).

> Use this page when reviewing AppHost code, CI YAML, or shell snippets for 13.3 compatibility.
> Agents must scrub for these patterns before recommending or generating code.

## Quick scrub table

| Change | Migration |
|--------|-----------|
| `--log-level` → `--pipeline-log-level` on `aspire publish` / `aspire deploy` | Update CI/CD scripts to use `--pipeline-log-level <level>`. |
| Dashboard MCP server **removed** along with `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL` | Use AppHost-level MCP via `aspire agent init`. See [Dashboard MCP migration](#dashboard-mcp-migration). |
| `NameOutput` → `NameOutputReference` (Azure Network resources) | Replace every `*.NameOutput` with `*.NameOutputReference`. |
| `OtlpEndpointEnvironmentVariableName` property removed | Remove the property; OTLP endpoint env var is managed automatically. |
| `AksSkuTier` enum removed | Delete the reference. AKS control-plane defaults to **Free** SKU. |
| `AddAndPublishPromptAgent` API removed | Use `AddPromptAgent` — returns a working `AzurePromptAgentResource`. |
| Kubernetes Ingress / Gateway routing types moved namespaces | Update `using` directives where these types are referenced directly. |
| `package.json` `engines.node` no longer drives Node image selection | Pin Node version explicitly via `WithDockerfile` or your Dockerfile base image. |
| `dotnet new aspire-py-starter` removed | Use `aspire new aspire-py-starter` (template moved to Aspire CLI; .NET SDK no longer required). |
| TypeScript per-kind `withEnvironment*` helpers `@deprecated` | Use unified `withEnvironment(name, value)`. See [TS withEnvironment migration](#typescript-withenvironment-migration). |
| CLI telemetry `--format json` schema aligned with MCP tool format | Update parsers consuming `--format json` from telemetry commands. |
| `ASPIREEXTENSION001` JS diagnostic ID renamed to `ASPIREJAVASCRIPT001` | Update `#pragma warning disable` and any code-search rules. |
| Docker Swarm `UpdateConfig` property types changed | Update generated/hand-written Swarm overrides. |
| `aspire init` no longer wires up the AppHost | Follow with `aspireify` (in-plugin sibling skill or project-local copy). |
| In-dashboard GitHub Copilot UI removed | Replaced by `aspire agent init`-driven agentic flow (works with Copilot, Claude, Cursor, any MCP/skill agent). |

## TypeScript `withEnvironment` migration

Per-kind helpers are still generated for backward compatibility but marked `@deprecated` and
slated for removal. Replace every call site with the unified API.

| Old method (deprecated)                              | Replacement                          |
|------------------------------------------------------|--------------------------------------|
| `withEnvironmentExpression(name, expr)`              | `withEnvironment(name, expr)`        |
| `withEnvironmentEndpoint(name, endpoint)`            | `withEnvironment(name, endpoint)`    |
| `withEnvironmentParameter(name, param)`              | `withEnvironment(name, param)`       |
| `withEnvironmentConnectionString(name, resource)`    | `withEnvironment(name, resource)`    |
| `withEnvironmentFromOutput(name, output)`            | `withEnvironment(name, output)`      |
| `withEnvironmentFromKeyVaultSecret(name, secret)`    | `withEnvironment(name, secret)`      |

The unified `withEnvironment(name, value)` accepts any of: a plain `string`,
`ReferenceExpression`, `EndpointReference`, parameter builder, connection string resource
builder, or `IExpressionValue` — one method handles every value kind.

## Dashboard MCP migration

The Aspire dashboard no longer hosts an MCP server, and the
`ASPIRE_DASHBOARD_MCP_ENDPOINT_URL` environment variable has been removed. AI coding agents
now connect to your Aspire app through an **AppHost-level MCP server** plus skills wired up by
`aspire agent init`.

```bash
# Replace any prior dashboard-MCP setup with:
aspire agent init
```

Detection covers GitHub Copilot, Claude, Cursor, and any other agent that supports skills or
MCP. The previous in-dashboard GitHub Copilot UI has been removed in favor of this flow.

If a script or environment file still sets `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`, delete it.

## `aspire init` no longer wires the AppHost

In 13.3, `aspire init` drops a skeleton (`aspire.config.json` + AppHost stub) and installs the
`aspireify` agent skill, but does **not** wire resources, projects, or integrations on its own.

Workflow:

1. `aspire init --language csharp|typescript --non-interactive`
2. Hand off to the `aspireify` skill (in-plugin sibling or project-local
   `.agents/skills/aspireify/SKILL.md`) to scan the repo, propose a resource graph, edit the
   AppHost, wire `Aspire.ServiceDefaults` + OTel, and validate via `aspire start`.

Skills shipped in this plugin:

- `aspire-init` — owns the skeleton drop and template choice.
- `aspireify` — owns the wiring step after the skeleton lands.

## Migration from Aspire 13.2 to 13.3

Mirror of the upstream checklist plus the items above. Run through each step before
recommending Aspire-related changes against an existing repo.

1. **Update the CLI** — `aspire update --self`.
2. **Update your projects** — `aspire update` from the repo root (modifies project package
   references; get user approval before running in CI).
3. **Audit `--log-level` usage** in CI/CD pipelines and rename to `--pipeline-log-level`.
4. **Search for breaking-change identifiers** in AppHost code:
   - `NameOutput` → `NameOutputReference`
   - `AddAndPublishPromptAgent` → `AddPromptAgent`
   - `AksSkuTier` → delete (defaults to Free)
   - `OtlpEndpointEnvironmentVariableName` → delete
   - `ASPIREEXTENSION001` → `ASPIREJAVASCRIPT001`
5. **Replace `dotnet new aspire-py-starter`** with `aspire new aspire-py-starter`.
6. **Rerun `aspire agent init`** if you previously relied on the dashboard MCP server or its
   `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL` env var.
7. **Re-pin Node versions** in Dockerfiles if you were relying on `package.json`
   `engines.node` for base-image selection.
8. **Rewire AppHost via `aspireify`** if the repo went through `aspire init` on 13.3 — the
   skeleton is unwired by design.
9. **Replace deprecated TS `withEnvironment*` helpers** with the unified
   `withEnvironment(name, value)` (see [migration table](#typescript-withenvironment-migration)).
10. **Update Kubernetes Ingress / Gateway `using` directives** if your AppHost references the
    moved types directly.
11. **Update consumers of `--format json`** from CLI telemetry commands to the new
    MCP-aligned schema.
12. **Update Docker Swarm overrides** for the new `UpdateConfig` property types.
