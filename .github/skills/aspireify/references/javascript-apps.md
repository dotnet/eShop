# JavaScript and TypeScript app patterns

Use this reference when wiring JavaScript/TypeScript services into the AppHost or configuring TypeScript AppHost dependencies (Step 5 and Step 6).

## Choosing the right JavaScript resource type

The `Aspire.Hosting.JavaScript` package provides three resource types. Pick the right one:

| Signal | Use | Example |
|--------|-----|---------|
| Vite app (has `vite.config.*`) | `AddViteApp(name, dir)` | Frontend SPA, Vite + React/Vue/Svelte |
| App runs via package.json script only | `AddJavaScriptApp(name, dir, { runScriptName })` | CRA app, Next.js, monorepo root scripts |
| App has a specific Node entry file (`.js`/`.ts`) and uses a dev script like `ts-node-dev` | `AddNodeApp(name, dir, "entry.js")` + `.WithRunScript("start:dev")` | Express/Fastify API, Socket.IO server |

**Key distinctions:**
- `AddNodeApp` is for apps that run a **specific file** with Node (e.g., an Express server at `src/index.ts`). Use `.WithRunScript("start:dev")` to override the dev-time command (e.g., `ts-node-dev`).
- `AddJavaScriptApp` runs a **package.json script** — simpler, good when the script handles everything.
- `AddViteApp` is `AddJavaScriptApp` with Vite-specific defaults (auto-HTTPS config augmentation, `dev` as default script).

## JavaScript dev scripts

Use `.WithRunScript()` to control which package.json script runs during development:

```typescript
// Express API with TypeScript: uses ts-node-dev for hot reload in dev
const api = await builder
    .addNodeApp("api", "./api", "src/index.ts")
    .withRunScript("start:dev")                      // runs "yarn start:dev" (ts-node-dev)
    .withYarn()
    .withHttpEndpoint({ env: "PORT" });

// Vite frontend: default "dev" script is fine, just add yarn
const web = await builder
    .addViteApp("web", "./frontend")
    .withYarn();
```

## Framework-specific port binding

Not all frameworks read ports from env vars the same way:

| Framework | Port mechanism | AppHost pattern |
|-----------|---------------|-----------------|
| Express/Fastify | `process.env.PORT` | `.withHttpEndpoint({ env: "PORT" })` |
| Vite | `--port` CLI arg or `server.port` in config | `.withHttpEndpoint({ env: "PORT" })` — Aspire's Vite integration handles this automatically |
| Next.js | `PORT` env or `--port` | `.withHttpEndpoint({ env: "PORT" })` |
| CRA | `PORT` env | `.withHttpEndpoint({ env: "PORT" })` |

When the framework supports reading the port from an env var or Aspire already handles it, **prefer that over pinning a fixed port**. Managed ports make repeated local runs more reliable and work better when multiple services or multiple Aspire apps are running.

**Suppress auto-browser-open:** Many dev servers (Vite, CRA, Next.js) auto-open a browser on start. Add `.withEnvironment("BROWSER", "none")` to prevent this in Aspire-managed apps. Vite also respects `server.open: false` in its config.

## Yarn/pnpm workspace monorepos

In monorepos that use **yarn workspaces** or **pnpm workspaces**, all workspace packages share a single root-level `node_modules/` directory (hoisted or symlinked). This creates two specific problems with `.withYarn()` / `.withPnpm()`:

1. **Concurrent install conflicts (Windows)**: `.withYarn()` runs `yarn install` before each resource starts. When multiple resources start concurrently, each triggers a root-level `yarn install` that tries to write to the shared `node_modules/`. On Windows, this causes `EPERM: operation not permitted` errors when one resource's running process (e.g., `esbuild.exe`) holds a file lock while another `yarn install` tries to overwrite it.

2. **Redundant installs**: In a properly set up workspace, `yarn` at the root installs everything for all workspaces. Running `yarn install` per-resource is redundant and slow.

**The fix: don't use `.withYarn()` on individual workspace resources.** Instead, ensure dependencies are installed once at the root before starting:

```typescript
// ❌ WRONG for workspace monorepos — concurrent installs cause file locking errors
const app = await builder.addViteApp("app", "./packages/frontend")
    .withYarn();  // triggers yarn install at startup → EPERM on Windows

const api = await builder.addNodeApp("api", "./packages/api", "src/index.ts")
    .withYarn();  // second concurrent yarn install → file lock conflict

// ✅ CORRECT for workspace monorepos — deps already installed at root
const app = await builder.addViteApp("app", "./packages/frontend");

const api = await builder.addNodeApp("api", "./packages/api", "src/index.ts")
    .withRunScript("start:dev");
```

Tell the user: *"This is a yarn workspace monorepo — I'll skip `.withYarn()` on individual resources since dependencies are shared at the root. Make sure to run `yarn` at the root before `aspire start`."*

**This only applies to workspace monorepos with shared `node_modules`.** For standalone apps or apps with independent `node_modules` directories, `.withYarn()` / `.withPnpm()` is correct and should be used — it ensures deps are installed before the resource starts.

## TypeScript AppHost dependency configuration (Step 6)

### package.json

If one exists at the root, augment it (do not overwrite). Add/merge these scripts that delegate to the Aspire CLI:

```json
{
  "type": "module",
  "scripts": {
    "dev": "aspire run",
    "build": "tsc",
    "watch": "tsc --watch"
  }
}
```

If no root `package.json` exists, create a minimal one matching the canonical Aspire template:

```json
{
  "name": "<repo-name>",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "aspire run",
    "build": "tsc",
    "watch": "tsc --watch"
  },
  "engines": {
    "node": "^20.19.0 || ^22.13.0 || >=24"
  }
}
```

**Important**: Scripts should point to `aspire run`/`aspire start` — the Aspire CLI handles TypeScript compilation internally. Do not use `npx tsc && node apphost.js` patterns.

Never overwrite existing `scripts`, `dependencies`, or `devDependencies` — merge only. Do not manually add Aspire SDK packages — `aspire restore` handles those.

Run `aspire restore` to generate the `.aspire/modules/` directory with TypeScript SDK bindings, then install dependencies with the repo's package manager (`npm install`, `pnpm install`, or `yarn`).

### tsconfig.json

Augment if it exists:

- Ensure `".aspire/modules/**/*.ts"` and `"apphost.ts"` are in `include`
- Ensure `"module"` is `"nodenext"` or `"node16"` (ESM required)
- Ensure `"moduleResolution"` matches

If no `tsconfig.json` exists and `aspire restore` didn't create one, create a minimal one:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "nodenext",
    "moduleResolution": "nodenext",
    "esModuleInterop": true,
    "strict": true,
    "outDir": "./dist",
    "rootDir": "."
  },
  "include": ["apphost.ts", ".aspire/modules/**/*.ts"]
}
```

### ESLint

Only augment if config already exists. If it uses `parserOptions.project` or `parserOptions.projectService`, ensure the AppHost tsconfig is discoverable. Do not create ESLint configuration from scratch.
