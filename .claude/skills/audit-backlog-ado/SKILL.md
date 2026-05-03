---
compatibility: Requires an active Azure DevOps MCP connection (mcp__azure-devops__* tools). Reads backlog items, iteration data, and team settings from ADO. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Audits the Azure DevOps product backlog for hygiene, story quality, and refinement readiness — identifying items that are too large, missing acceptance criteria, stale, unestimated, or out of priority order. Use when a Scrum Master or Product Owner says things like "audit the backlog", "is the backlog healthy", "review the backlog before refinement", "clean up the backlog", "check backlog hygiene", or "prepare the backlog for planning". Do not use for individual story audits — use audit-user-story for that. Do not use for sprint planning or velocity review.
license: MIT
metadata:
    ceremony: Backlog Refinement
    github-path: skills/audit-backlog-ado
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 32c5946aead876b134e07947a797a2eee9f45a19
    perspective: Scrum Master / Product Owner
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: audit-backlog-ado
---
# Backlog Audit — Azure DevOps

## Purpose

A neglected backlog is a planning liability. Items without acceptance criteria become guesswork in Sprint Planning. Items that haven't moved in months crowd out real priorities. Items sized at 40 points can't be committed to. This audit surfaces all of it before it causes a problem, so the team walks into refinement with a clear picture of what needs attention.

---

## Tool detection

This skill requires ADO MCP tools. If `mcp__azure-devops__*` tools are not active, tell the user: *"This skill requires an Azure DevOps MCP connection. See docs/mcp-setup.md for setup instructions."*

---

## Step 1 — Identify the backlog scope

Ask: *"Which project and team backlog should I audit? And how deep — just the top 20 items, the full backlog, or a specific number?"*

Before calling any backlog or team tool, resolve project and team in this exact order:
1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
3. Only then call `wit_list_backlogs` with `project: $PROJECT` and `team: $TEAM` to confirm available backlog levels (Portfolio, Requirements, Task). Never call backlog tools with null values for project or team.

Default: audit the **Requirements backlog** (Product Backlog Items and Bugs) — the layer the team commits from. If the team uses a different backlog level, adjust.

Use `wit_list_backlog_work_items` or `wit_query_by_wiql` to retrieve the items. Sort by backlog rank (stack rank). Read: title, state, story points, description, acceptance criteria (custom field or description), tags, and last modified date.

---

## Step 2 — Run the health checks

For each backlog item, evaluate the following criteria. Track counts and flag specifics.

### 2a — Readiness (Definition of Ready)

| Check | Pass condition | Flag if |
|---|---|---|
| Has a title | Clear, action-oriented | Title is "Story", "TBD", a jira key, or too vague to understand without context |
| Has a description or user story format | As a / I want / So that OR a clear description | Empty description |
| Has acceptance criteria | At least one testable AC | No acceptance criteria at all |
| ACs are testable | Each AC describes an observable condition | ACs are vague ("it should be good") or describe UI steps rather than outcomes |
| Has an estimate | Story points > 0 | Unestimated (null or 0) |

### 2b — Size

| Check | Flag if |
|---|---|
| Story fits one Sprint | Story points > team's typical Sprint velocity ÷ 2 (or > 13 points as a default threshold — ask if team uses a different threshold) |
| Story is not an epic disguised as a story | Title or description spans multiple independent capabilities |

### 2c — Staleness

| Check | Flag if |
|---|---|
| Item is actively relevant | Last modified > 90 days ago AND still in New/Active state |
| Item is not blocked indefinitely | Tagged "Blocked" or "On Hold" with no resolution date |
| Item is not a duplicate | Title is near-identical to another item in the backlog |

### 2d — Priority coherence

- Are the top 10 items the ones the PO would actually commit to next Sprint?
- Are there items in the top 10 that depend on items ranked below them? (dependency inversion risk)
- Are there items in Active state that are not in the current Sprint? (orphaned actives)

### 2e — Sprint assignment anomalies

- Items assigned to a past Sprint that were not completed — are they still relevant or should they be reprioritised?
- Items assigned to a future Sprint with no estimate or AC — they will fail Sprint Planning.

---

## Step 3 — Produce the audit report

```
Backlog Audit — [PROJECT] / [TEAM] — [DATE]

Items reviewed: [N]
Items flagged: [N] ([X]%)

━━━ Critical — blocks Sprint Planning ━━━━━━━━━━━━━━━━━

Missing acceptance criteria: [N items]
  - #[ID] [Title] — last modified [date]
  - ...

Unestimated items in top 20: [N items]
  - #[ID] [Title]
  - ...

━━━ Size issues ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Oversized items (> [threshold] points): [N]
  - #[ID] [Title] — [N] points — consider splitting

━━━ Staleness ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Items not touched in 90+ days: [N]
  - #[ID] [Title] — last modified [date] — still in [state]

Blocked / On Hold with no path forward: [N]
  - #[ID] [Title] — tagged [tag] since [date]

━━━ Priority concerns ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Dependency inversions detected: [N]
  - #[ID] [Title] ranked above #[ID] [Title] which it depends on

Orphaned actives (Active but not in a Sprint): [N]
  - #[ID] [Title]

━━━ Summary ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Refinement-ready (top [N] items): [N] of [N]
Recommended refinement focus: [list top 3–5 most critical items to fix]
```

---

## Step 4 — Offer actions

After presenting the report, offer:

1. *"Should I add comments to the flagged items explaining what's needed?"*
   - If yes, use `wit_add_work_item_comment` for each flagged item with a clear, actionable note: *"This item is missing acceptance criteria and cannot be committed to in Sprint Planning. Please add at least one testable AC before the next refinement session."*

2. *"Should I tag the oversized items so they're easy to filter in the refinement session?"*
   - If yes, use `wit_update_work_item` to add a tag like `needs-splitting`.

3. *"Should I generate a refinement agenda — ordered list of items to address in the next session?"*
   - If yes, produce a prioritised list: critical items first (missing ACs, unestimated), then oversized, then stale.

Never modify state, priority, or content of items without explicit confirmation.

---

## Guardrails

- Never change backlog priority (stack rank) without explicit PO confirmation — rank is a PO decision.
- Never mark items as Resolved or Closed — only flag for PO attention.
- A large story is not automatically bad — if the team consistently delivers 40-point stories because they decompose them into tasks during Sprint Planning, note it but don't flag it as critical.
- If the backlog has more than 100 items, focus the detailed audit on the top 40 (the realistic planning horizon). Note the total backlog size but don't flood the report with items the team won't touch for six months.
- Stale items in the bottom half of the backlog are normal — only flag stale items that are ranked in the top 30.
