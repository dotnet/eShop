---
compatibility: Requires an Azure DevOps MCP connection (mcp__azure-devops__* or mcp__ado__* tools). No Jira support — use audit-sprint-jira for Jira projects. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Audits a completed or in-progress Azure DevOps sprint against Scrum and ADO best practices — checks Sprint Goal presence, work item hygiene, state transitions, field completeness, capacity tracking, and Definition of Done compliance. Surfaces findings with severity levels and offers to post a summary to the ADO wiki or add comments to specific work items. Use when anyone says things like "audit our sprint in ADO", "review the sprint in Azure DevOps", "check our ADO sprint health", "are our work items following best practices", or "sprint health check". Don't use for Jira, Sprint Planning, Daily Scrum, or Retrospective. Requires an Azure DevOps MCP connection.
license: MIT
metadata:
    ceremony: Sprint Review
    github-path: skills/audit-sprint-ado
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 368cf9011d368b558d1975f862abd2ecde11045c
    perspective: Scrum Master / Team
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: audit-sprint-ado
---
# ADO Sprint Audit

## Purpose

ADO gives teams enormous flexibility — which means it's easy to drift into habits that obscure progress, hide impediments, or make velocity data meaningless. This audit checks both Scrum health (is the team doing Scrum well?) and ADO hygiene (is the data in the board trustworthy?).

---

## Prerequisites

This skill requires an active Azure DevOps MCP connection. If `mcp__azure-devops__*` or `mcp__ado__*` tools are not available, let the user know and stop.

---

## Step 1 — Identify the sprint

Ask: *"Which sprint should I audit — the current one, the one just completed, or a specific iteration path?"*

**Before calling any iteration tool, resolve project and team in this exact order:**

1. Call `core_list_projects` to get the list of projects. If there is only one project, use it automatically. If there are multiple, ask the user which project to audit. Store as `$PROJECT`.
2. Call `core_list_project_teams` with `$PROJECT` to get the list of teams. If there is only one team, use it automatically. If there are multiple, ask the user which team to audit. Store as `$TEAM`.
3. Only after both `$PROJECT` and `$TEAM` are resolved as non-null strings, call `work_list_team_iterations` with `project: $PROJECT` and `team: $TEAM` to list iterations. Store the selected iteration path as `$ITERATION`.

Never call `work_list_team_iterations` without explicit non-null values for both `project` and `team`.

---

## Step 2 — Fetch sprint data

Retrieve all relevant data for `$ITERATION`:

- All work items in the iteration via `wit_get_work_items_for_iteration`
- Team capacity via `work_get_team_capacity` or `work_get_iteration_capacities`
- Team settings (Definition of Done, working days) via `work_get_team_settings`

Group items by type: PBI / User Story, Task, Bug, Test Case, other.

---

## Step 3 — Run the audit

Grade each check as:
- ✅ **Pass** — meets the standard
- ⚠️ **Warning** — worth improving
- ❌ **Fail** — missing or broken; impacts team effectiveness

### Area 1 — Sprint Goal (Scrum)

| Check | Pass condition |
|---|---|
| Sprint Goal defined | Iteration has a non-empty goal field |
| Sprint Goal is outcome-oriented | Goal reads as a value statement, not a task list |
| All committed items relate to the Sprint Goal | No large clusters of work unrelated to the goal |

A missing Sprint Goal is a significant Scrum health issue — the team has no way to assess whether the Sprint was successful beyond counting items.

### Area 2 — Work Item Hygiene (ADO)

| Check | Pass condition |
|---|---|
| All items have an assigned owner | No unassigned active or resolved items |
| All items are linked to the correct iteration | No stray items from other iterations |
| PBIs / User Stories have a parent (Feature or Epic) | Orphaned backlog items are harder to trace to strategy |
| Bugs are linked to the PBI they affect | Unlinked bugs obscure the root cause |
| No items in "Removed" state without a comment | Silent removals hide scope changes |
| Item titles are descriptive | No "New PBI", "Task 1", or copy-paste duplicates |

### Area 3 — State Transitions (ADO)

ADO state transitions tell the story of how work flowed. Irregular transitions are a signal — not always wrong, but worth understanding.

| Check | Pass condition |
|---|---|
| No items jumped from New → Done without Active | Direct state jumps should have a comment explaining why |
| No items lingered in Active for the full sprint without progress | Flag items with no state change and no task activity |
| Resolved items have been reviewed and closed or reopened | Resolved ≠ Done — items should not stay in Resolved at sprint end |
| Tasks are Closed when their parent PBI is Done | Open child tasks on a Done PBI creates inconsistent data |

Use `wit_list_work_item_revisions` on flagged items to inspect state history if needed.

### Area 4 — Acceptance Criteria and Definition of Done (Scrum + ADO)

| Check | Pass condition |
|---|---|
| All PBIs have acceptance criteria | The `Microsoft.VSTS.Common.AcceptanceCriteria` field is not empty |
| Done/Closed items have ACs that can be verified | ACs are conditions, not vague descriptions |
| No Done items have open child tasks | All tasks under a closed PBI should be closed |
| DoD is applied consistently | If a team DoD exists, Done items should visibly meet it |

### Area 5 — Capacity and Velocity (ADO)

| Check | Pass condition |
|---|---|
| Team capacity was set for the sprint | Capacity entries exist for each team member |
| Remaining work on closed tasks is zero | Non-zero remaining work on closed tasks skews burn-down |
| Sprint commitment matched capacity | Flag sprints where planned effort significantly exceeded capacity |
| Velocity is calculable | Enough PBIs have story points / effort for velocity to be meaningful |

---

## Step 4 — Present the audit report

```
ADO Sprint Audit — [ITERATION PATH]
Audited: [DATE]

Sprint Goal
  ✅ / ⚠️ / ❌  [Check]: [finding]

Work Item Hygiene
  ✅ / ⚠️ / ❌  [Check]: [finding]
  (list each item ID where relevant)

State Transitions
  ✅ / ⚠️ / ❌  [Check]: [finding]

Acceptance Criteria & DoD
  ✅ / ⚠️ / ❌  [Check]: [finding]

Capacity & Velocity
  ✅ / ⚠️ / ❌  [Check]: [finding]

Summary
  [N] passed · [N] warnings · [N] failed

Top recommendations
1. [Most impactful fix — name specific item IDs where possible]
2. ...
```

---

## Step 5 — Offer follow-up actions

Ask the user which, if any, follow-up actions to take:

1. **Add comments to specific work items** — for items with AC or DoD findings, offer to post a comment via `wit_add_work_item_comment`
2. **Post audit to ADO wiki** — offer to create or update a wiki page with the full report via `wiki_create_or_update_page`
3. **No action** — output the report as a clean block for the team to act on manually

Never take any of these actions without explicit confirmation.

---

## Guardrails

- Never update work item state, fields, or assignments without explicit user confirmation.
- Never frame findings as individual failures — focus on systemic patterns and process improvements.
- If the sprint data is incomplete (e.g. capacity not set, Sprint Goal missing), report the gap rather than skipping the check.
- Flag persistent patterns across multiple sprints if the user mentions them — a single audit snapshot is less useful than a trend.
- Capacity and velocity data is team-internal — never suggest sharing it outside the team without the user's intent being clear.
