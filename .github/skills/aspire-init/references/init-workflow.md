# `aspire init` Workflow

Reference for the `aspire init` flow on existing repositories. Source:
https://aspire.dev/reference/cli/commands/aspire-init/.

## What `aspire init` Does

`aspire init` initializes Aspire support in an existing repo or workspace. It scaffolds a
**minimal AppHost skeleton** plus an `aspire.config.json`, then optionally installs the
**`aspireify`** agent skill so the AI coding agent can complete the wiring.

It does **not**:

- Wire resources, projects, or integrations into the AppHost
- Modify existing project files
- Change the repo's .NET SDK version (`global.json` is left alone)
- Trust the developer certificate (run `aspire certs trust` separately if needed)

## Command and Options

```bash
aspire init [options]
```

| Option | Purpose |
|--------|---------|
| `--language` | `csharp` or `typescript`. Required in `--non-interactive` mode if both paths are available |
| `--channel` | `stable` (default), `staging`, `daily` |
| `--non-interactive` | **Required for agent execution.** Disables prompts and spinners |
| `--nologo` | Suppress startup banner / telemetry notice |
| `--banner` | Show the animated welcome banner |
| `-l, --log-level` | `Critical`, `Debug`, `Error`, `Information`, `None`, `Trace`, `Warning` |
| `--wait-for-debugger` | Pause until a debugger attaches |
| `-?, -h, --help` | Print help |

## What Gets Dropped

### C# Path (`--language csharp`)

- **`apphost.cs`** — single-file AppHost using `#:sdk Aspire.AppHost.Sdk` and `#:package`
  directives. No `.csproj` is created in the file-based mode.
- **`aspire.config.json`** at repo root.

### TypeScript Path (`--language typescript`)

- **`apphost.ts`** at repo root.
- **`.aspire/modules/`** generated folder (do not edit by hand — regenerate with `aspire add`).
- **`aspire.config.json`** at repo root.

### `aspireify` Skill

- A Markdown skill file is installed into the AI agent's skill directory — the same
  directory chosen by `aspire agent init` (e.g., `.agents/skills/aspireify/`,
  `.github/skills/aspireify/`, `.claude/skills/aspireify/`, or `.opencode/skill/aspireify/`).
- The skill instructs the agent to scan the repo, propose a resource graph, edit the
  AppHost, and validate via `aspire start`.

## `aspire.config.json` Layout

| Field | Values | Meaning |
|-------|--------|---------|
| `appHost.language` | `"csharp"` or `"typescript/nodejs"` | Which AppHost syntax to use |
| `appHost.path` | Path to AppHost file or directory | Where the AppHost lives |

C# has two sub-modes the agent may encounter:

- **Single-file** — `appHost.path` points at `apphost.cs` (uses `#:sdk` directive).
- **Full project** — `appHost.path` points at a directory containing a `.csproj` plus
  `Program.cs`. In solution-backed repos, full project mode lets the AppHost participate in IDE and solution workflows.

## End-to-End Sequence

1. **Pre-flight** — verify no AppHost already exists. If one does, stop and route to
   `aspireify` or `aspire-orchestration`.
2. **Run init**:
   ```bash
   aspire init --language <csharp|typescript> --non-interactive
   ```
3. **Confirm artifacts** — `apphost.cs` (or `apphost.ts` + `.aspire/modules/`) and
   `aspire.config.json` should be in the repo root.
4. **Confirm `aspireify` skill installed** — the agent's skill directory contains
   `aspireify/SKILL.md`. If missing, run `aspire agent init` to install it.
5. **Hand off to `aspireify`** for wiring:
   - Scan repo and discover existing projects, services, containers
   - Ask the user clarifying questions (which services to orchestrate, hardcoded ports,
     whether to map env vars or switch to Aspire service discovery)
   - Wire resources with `WithReference`, `WaitFor`, endpoints, volumes
   - Optionally configure OpenTelemetry
   - Validate with a smoke-test `aspire start`
6. **Validate** — once `aspireify` finishes wiring, run `aspire start` (handled by
   `aspire-orchestration`) and confirm resources reach a healthy state.

## Project-Local Skill Precedence

`aspire init` installs `aspireify` into the project's skill directory when an agent skill location is detected.
When a project-local `.agents/skills/aspireify/SKILL.md` (or equivalent location) is
present, **defer to it and warn the user** — the project-local copy may carry repo-specific
guidance.

The same precedence applies to a legacy `.agents/skills/aspire-init/SKILL.md` from older
`aspire init` runs: warn and defer.

## Failure Modes and Recovery

| Symptom | Cause | Recovery |
|---------|-------|----------|
| `aspire init` reports an AppHost already exists | Repo is already an Aspire app | Stop. Route to `aspireify` or `aspire-orchestration` |
| `aspire init` fails without `--language` in `--non-interactive` | CLI needs the language explicitly when prompts are disabled | Re-run with `--language csharp` or `--language typescript` |
| Skeleton dropped but no `aspireify` skill | Agent skill directory not detected during init | Run `aspire agent init` to install `aspireify`, then continue |
| `apphost.cs` references a missing `#:package` | Channel mismatch or transient feed issue | Re-run with `--channel stable` (or `daily` for pre-release) |
| `aspire start` after wiring fails immediately | Wiring incomplete or wrong AppHost path | Re-invoke `aspireify`; confirm `aspire.config.json` `appHost.path` is correct |

## Don't Do This

- **Don't run `aspire init` if any AppHost signal already exists** — it duplicates the
  skeleton and confuses subsequent tooling.
- **Don't edit `.aspire/modules/`** in TypeScript AppHosts. Use `aspire add` to regenerate APIs;
  use `aspire restore` if files are missing.
- **Don't install the obsolete Aspire workload** (`dotnet workload install aspire`). Use
  `aspire init`, `aspire new`, or `aspire add` instead.
- **Don't perform the resource wiring inside this skill.** Hand off to `aspireify`. This
  skill's job ends when the skeleton + `aspire.config.json` + `aspireify` skill are in
  place.
