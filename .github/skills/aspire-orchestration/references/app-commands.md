# App Commands

Use this when the task is about app-level lifecycle, bootstrap, or AppHost-wide maintenance.

## Start The App Safely In The Background

```bash
aspire start
aspire start --isolated
aspire stop
```

- Use `aspire start` for normal background AppHost execution.
- In git worktrees or when another local instance may already be running, use `aspire start --isolated`.
- To restart after AppHost changes, rerun the same start command.
- Use `aspire stop` when cleanup is explicitly requested, ports/locks need to be released, or you are finished with a started instance that the user did not ask to keep running.
- Avoid `aspire run` in agent workflows — it blocks the terminal.

### `aspire run` vs `aspire start`

| Command | Mode | Use Case |
|---------|------|----------|
| `aspire run` | Foreground (interactive) | Human developer at terminal |
| `aspire start` | Background (detached) | **AI agents — always prefer** |
| `aspire run --detach` | Background | Alternative to `aspire start` |

## Create A New Aspire App Or Add Aspire To An Existing App

```bash
aspire new
aspire init
aspire init --language typescript
```

- Use `aspire new` when creating a brand-new Aspire app from scratch.
- Use `aspire init` when adding Aspire to an existing application.

## After `aspire init` — Hand Off to `aspireify`

`aspire init` drops a minimal AppHost skeleton + AppHost configuration into the
repo and installs the **`aspireify`** agent skill alongside it. `aspire init` itself does not
wire resources, projects, or integrations. Hand off the wiring step to:

1. The in-plugin sibling skill: [`../../aspireify/SKILL.md`](../../aspireify/SKILL.md), or
2. The project-local `.agents/skills/aspireify/SKILL.md` if `aspire init` installed it
   (project-local wins — defer to it and warn the user).

The aspireify workflow:

1. **Scan** the repository — discover .NET projects, Node.js apps, Python/Go services, docker-compose files
2. **Present findings** — confirm with user which services to include
3. **Wire the AppHost** — add resources using 3-tier API preference:
   - Tier 1: First-party `Aspire.Hosting.*` (e.g., `AddPostgres`, `AddRedis`, `AddViteApp`, `AddNextJsApp`)
   - Tier 2: Community Toolkit `CommunityToolkit.Aspire.Hosting.*` (e.g., `AddGolangApp`)
   - Tier 3: Raw fallbacks (`AddExecutable`, `AddDockerfile`, `AddContainer`)
4. **Configure dependencies** — ServiceDefaults for .NET, OTel for non-.NET
5. **Validate** — `aspire start` until all resources are healthy

### Key Init Rules

- **Never install the obsolete Aspire workload** (`dotnet workload install aspire`)
- **Never change the repo's .NET SDK version** (don't modify root `global.json`)
- **Never change existing project target frameworks** (older TFMs work with newer AppHost)
- **Always use `aspire docs search` before writing AppHost code** — don't guess APIs
- **Never hardcode URLs** — use endpoint references (`WithReference`, `WithEnvironment` with expressions)
- **Never overwrite existing files** — augment and merge
- **Adapt the AppHost to the app**, not the other way around

### Init Config: `aspire.config.json`

Read `aspire.config.json` at repo root for init context:

| Field | Values | Meaning |
|-------|--------|---------|
| `appHost.language` | `"typescript/nodejs"` or `"csharp"` | AppHost syntax to use |
| `appHost.path` | Path to AppHost file/dir | Where to edit |

C# has two sub-modes:
- **Single-file**: `appHost.path` → `apphost.cs` (uses `#:sdk` directive)
- **Full project**: `appHost.path` → directory with `.csproj` + `Program.cs`

## Find The Right AppHost Or Refresh AppHost-Wide Support

```bash
aspire ps
aspire integration list --format Json
aspire integration search <query> --format Json
aspire add <package>
aspire update --self        # upgrades the Aspire CLI itself (NativeAOT global tool or curl install)
aspire update               # updates project package references / aspire.config.json
aspire restore
```

- Use `aspire ps` first to discover which AppHost is already running.
- Use `aspire integration list --format Json` and `aspire integration search <query> --format Json` for read-only integration discovery.
- Use `aspire add <package>` to add integrations and regenerate AppHost APIs when you are ready to mutate the AppHost.
- Use **`aspire update --self`** to upgrade the Aspire CLI itself — the safe, no-side-effect upgrade path agents can run unattended.
- Use **`aspire update` (no `--self`)** to refresh AppHost package references and bump pinned versions in `aspire.config.json`. This **modifies project files** — get user approval before running unattended (CI / agent flows).
- Use `aspire restore` after pulls, cleans, or missing generated files.
- Use `--apphost <path>` when the workspace has multiple AppHosts. The CLI's global config validates configured AppHost paths to catch typos early.

## Key Rules

- **Never install the obsolete Aspire workload** (`dotnet workload install aspire`). Use `aspire add`, `aspire init`, or `aspire new` instead.
- **Never edit `.aspire/modules/` directly** in TypeScript AppHosts. Use `aspire add <package>` to regenerate APIs, `aspire restore` if files are missing.
- For unfamiliar C# AppHost APIs, use `aspire docs search` as primary reference. If the `dotnet-inspect` skill is available, use it to inspect local symbols and overloads — but keep docs as the source of truth.
- For custom dashboard or resource commands (`WithCommand`), always run `aspire docs search "custom resource commands"` before implementing.

## Look Up API Reference Before Editing AppHost Code

```bash
aspire docs search <query>
aspire docs get <slug>
aspire docs api search <query> --language csharp
aspire docs api search <query> --language typescript
aspire docs api list <scope>
aspire docs api get <id>
```

- Use `aspire docs search` and `aspire docs get` for workflow guidance and documented patterns.
- Use `aspire docs api search` when you need the C# or TypeScript API reference entry for a resource builder, extension method, or member.
- Use `aspire docs api list <scope>` to browse children under a language, package, module, type, or symbol.
- Always specify `--language csharp` or `--language typescript` to get the correct API surface.
