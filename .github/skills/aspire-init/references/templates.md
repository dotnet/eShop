# `aspire new` Templates

Reference for choosing and invoking templates with `aspire new`. Source:
https://aspire.dev/reference/cli/commands/aspire-new/

## Template List

| Template | Description | AppHost Language |
|----------|-------------|------------------|
| `aspire-starter` | Starter App (ASP.NET Core / Blazor) | C# |
| `aspire-ts-cs-starter` | Starter App (ASP.NET Core / React) | C# |
| `aspire-py-starter` | Starter App (FastAPI / React) | **TypeScript** (drives Python) |
| `aspire-ts-starter` | Starter App (Express / React) | TypeScript |
| `aspire-empty` | Minimal scaffold, no resources pre-wired | C# |
| `aspire-ts-empty` | Minimal scaffold, no resources pre-wired | TypeScript |

> **Important — Python starter changed in 13.3.** `dotnet new aspire-py-starter` was removed.
> The current path is `aspire new aspire-py-starter`, and the AppHost is **TypeScript**, not
> C#. The TypeScript AppHost orchestrates the FastAPI service plus the React frontend.

## Common Options

| Option | Purpose |
|--------|---------|
| `-n, --name` | Name of the project to create |
| `-o, --output` | Output path. Defaults to `./<template-name>` (auto-suffixed if non-empty) |
| `-s, --source` | NuGet source for the project templates |
| `-v, --version` | Version of the project templates to use |
| `--channel` | Template channel: `stable` (default), `staging`, `daily` |
| `--non-interactive` | **Required for agent execution.** Disables prompts and spinners |
| `--nologo` | Suppress startup banner / telemetry notice |
| `--suppress-agent-init` | Skip the post-create prompt to configure AI agent environments |
| `-l, --log-level` | `Critical`, `Debug`, `Error`, `Information`, `None`, `Trace`, `Warning` |

## Template-Specific Options

### `aspire-py-starter`

| Option | Values | Default |
|--------|--------|---------|
| `--use-redis-cache` | `true` / `false` | Prompts interactively |

```bash
aspire new aspire-py-starter --use-redis-cache true --non-interactive
```

## Interactive Prompts (avoid by passing flags)

| Prompt | What It Decides | Skip With |
|--------|------------------|-----------|
| Project name | Output directory name | `--name <value>` |
| Template version | Which release to use | `--version <value>` |
| Output folder | Where files land | `--output <value>` |
| Use `*.dev.localhost` URLs? | HTTPS subdomain launch profile vs `http://localhost:<port>` | Pass `--non-interactive` to take the default (No, standard localhost) |

If a project picks `*.dev.localhost`, the user must trust the developer certificate first:

```bash
aspire certs trust
```

## Output Path Validation

- Invalid characters (e.g., null bytes) are rejected with an error.
- Existing **non-empty** directories are rejected — pick a new `--output` or empty the target.
- When `--output` is omitted, the CLI derives the directory from the template name and
  appends a numeric suffix (`./aspire-starter-2`, `./aspire-starter-3`, ...) if the default
  already exists.

## Examples

```bash
# Interactive — CLI prompts for name, output, version
aspire new aspire-starter

# Non-interactive C# empty scaffold pinned to a specific version
aspire new aspire-empty --version <version> --name aspireapp --output ./dev --non-interactive

# Pre-release templates from the daily channel
aspire new aspire-starter --channel daily

# TypeScript-AppHost-driven Python starter with Redis cache
aspire new aspire-py-starter --use-redis-cache true --name py-shop --output ./py-shop --non-interactive
```

## When `aspire new` Is Wrong

| Situation | Use Instead |
|-----------|-------------|
| Repo already contains services to model | `aspire init` (Workflow B in [SKILL.md](../SKILL.md)) |
| AppHost already exists in the repo | `aspireify` (re-wire) or `aspire-orchestration` (lifecycle) |
| Need to add an integration to an existing AppHost | `aspire add <package>` (handled by `aspire-orchestration`) |
