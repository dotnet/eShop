# Validation

End-to-end validation after editing the AppHost. Goal: confirm the wired graph
starts cleanly, every resource reaches a healthy state, and `aspire describe`
returns the expected shape. After a clean validation, `aspireify` self-deactivates
and hands off to `aspire-orchestration`.

## Flow

```bash
# 1. Start the AppHost in the background, JSON output for parsing
aspire start --non-interactive --format Json

# 2. Wait for each declared resource (use displayName from aspire ps)
aspire ps --format Json --include-hidden     # also lists hidden resources
aspire wait <displayName>                     # repeat per resource

# 3. Sanity-check the resource graph
aspire describe --format Json --include-hidden

# 4. Stop cleanly
aspire stop
```

## Why `--non-interactive`

Agents must always pass `--non-interactive` to `aspire start` (and other
commands) to suppress prompts and spinners. Without it the command can hang
waiting for input.

## Why `--format Json`

Structured output for parsing. Note: `aspire start` can interleave human-readable
lines with JSON in some scenarios ([#15843](https://github.com/microsoft/aspire/issues/15843)).
Strip non-JSON lines before parsing.

## Why `--include-hidden`

`aspire ps` and `aspire describe` filter hidden resources (proxies,
helper containers, migration jobs) by default. During validation use
`--include-hidden` so you don't miss anything that `aspire start` actually started.

## Why `aspire wait` not curl loops

`aspire wait <name>` blocks until the resource reports healthy via the AppHost's
own health model. Hand-rolled `curl` loops race with startup and don't see
container-internal health.

> **Resource-name gotcha** ([#15842](https://github.com/microsoft/aspire/issues/15842)):
> `aspire wait` rejects the `name` field — pass `displayName` from
> `aspire ps --format Json` instead.

## Recovery

| Symptom | Action |
|---------|--------|
| Build error in resource | Fix code; re-run `aspire start` |
| File-lock error during edit | Hand off to `aspire-orchestration` → `aspire stop` → retry edit |
| Port conflict on start | `aspire stop` (clears prior instance) → `aspire start` |
| Resource missing from `aspire ps` | Re-run with `--include-hidden`; if still missing, AppHost edit is wrong |
| `aspire wait` rejects name | Pull `displayName` from `aspire ps --format Json` |
| Mixed JSON output from `aspire start` | Strip non-JSON lines before parsing |
| Container-backed resource fails | Confirm Docker / Podman is running; re-run `aspire doctor` |
| TS AppHost change had no effect | You probably edited `.aspire/modules/`. Edit `apphost.ts` only |

## Hand-off Criteria

Aspireify deactivates when **all** of these hold:

1. `aspire start --non-interactive` exits 0 (or stays healthy in background mode)
2. `aspire wait` returns success for **every** declared, non-hidden resource
3. `aspire describe --format Json` shows the proposed graph (resources, references, endpoints)
4. `aspire stop` cleans up without errors

When done, announce:

```
✅ AppHost wired and validated:
   - Resources: pg, cache, api, worker, web
   - References: api → pg + cache; worker → pg; web → api
   - All resources reached healthy state.

Handing off to aspire-orchestration for day-to-day start/stop/wait.
Aspireify is done.
```

## Optional: Pre-Deploy Smoke Check

If the user plans to deploy next, suggest:

```bash
aspire publish --list-steps
aspire deploy --list-steps
```

Then route them to `aspire-deployment` for the actual `aspire deploy` /
`aspire publish` / `aspire destroy` workflow.
