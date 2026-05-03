---
compatibility: Requires a Jira MCP connection (mcp__jira__* tools). No ADO support — use audit-sprint-ado for Azure DevOps projects. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Audits a completed or in-progress Jira sprint against Scrum and Jira best practices — checks Sprint Goal presence, issue hygiene, workflow transitions, field completeness, story point coverage, and sub-task consistency. Surfaces findings with severity levels and offers to add comments to specific issues or export a summary report. Use when anyone says things like "audit our sprint in Jira", "review the Jira sprint", "check our Jira sprint health", "are our issues following best practices", or "sprint health check in Jira". Don't use for Azure DevOps — use audit-sprint-ado instead. Don't use for Sprint Planning, Daily Scrum, or Retrospective. Requires a Jira MCP connection.
license: MIT
metadata:
    ceremony: Sprint Review
    github-path: skills/audit-sprint-jira
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 60124b6fc61d4329d4bb48f10bfee8eec4806139
    perspective: Scrum Master / Team
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: audit-sprint-jira
---
# Jira Sprint Audit

## Purpose

Jira's flexibility is powerful and, unchecked, leads to boards where issues sit in the wrong state, story points are missing, epics are unlinked, and the Sprint Goal exists only in someone's memory. This audit checks both Scrum health and Jira hygiene so the team can trust their data and improve how they work.

---

## Prerequisites

This skill requires an active Jira MCP connection. If `mcp__jira__*` tools are not available, let the user know and stop.

---

## Step 1 — Identify the sprint

Ask: *"Which sprint should I audit — the current active one, the most recently closed one, or a specific sprint by name?"*

Use the available Jira sprint listing tool to confirm the sprint ID and name. Store as `$SPRINT`.

---

## Step 2 — Fetch sprint data

Retrieve all issues in `$SPRINT`:
- All issues assigned to the sprint, including their type, status, story points, assignee, epic link, sub-tasks, and description/AC fields
- Sprint details including the Sprint Goal (if set)

Group issues by type: Story, Task, Bug, Sub-task, Epic, other.

---

## Step 3 — Run the audit

Grade each check as:
- ✅ **Pass** — meets the standard
- ⚠️ **Warning** — worth improving
- ❌ **Fail** — missing or broken; impacts team effectiveness

### Area 1 — Sprint Goal (Scrum)

| Check | Pass condition |
|---|---|
| Sprint Goal defined | Sprint has a non-empty goal field |
| Sprint Goal is outcome-oriented | Goal is a value statement, not a task list or feature dump |
| Committed issues relate to the Sprint Goal | No large clusters of work that don't connect to the goal |

A missing Sprint Goal means the team cannot assess whether the Sprint succeeded — it reduces the Sprint to a time-boxed to-do list.

### Area 2 — Issue Hygiene (Jira)

| Check | Pass condition |
|---|---|
| All issues have an assignee | No unassigned In Progress or Done issues |
| Stories are linked to an Epic | Unlinked stories are harder to track against the Product Goal |
| Bugs are linked to the story they affect | Unlinked bugs obscure cause and context |
| Issue titles are descriptive | No "Story 1", "Bug", or untitled issues |
| No duplicate issues | Check for titles that are near-identical |
| Issue types used correctly | No Stories filed as Tasks or Bugs filed as Stories |

### Area 3 — Story Points and Sizing (Jira + Scrum)

Story points enable velocity tracking and honest sprint commitment. Missing or wildly inconsistent values degrade planning accuracy over time.

| Check | Pass condition |
|---|---|
| All Stories have story points | No unpointed Stories in the sprint |
| All Bugs have story points | Bugs consume capacity — they should be sized |
| Points are within a reasonable range | Flag outliers significantly above the team's typical velocity per issue (may indicate unrefined work) |
| Sprint total points are within historical capacity | Flag sprints where total commitment significantly exceeded typical velocity |

### Area 4 — Acceptance Criteria and Definition of Done (Scrum + Jira)

| Check | Pass condition |
|---|---|
| All Stories have acceptance criteria | AC field or clearly marked section in the description is not empty |
| Done issues have ACs that are verifiable | ACs are conditions, not vague summaries |
| All sub-tasks closed when parent is Done | Open sub-tasks on a Done issue creates inconsistent data |
| Resolution is set on closed issues | Issues marked Done should have an explicit resolution (e.g. "Done", "Fixed") |
| DoD applied consistently | Done issues should visibly meet the team's agreed Definition of Done |

### Area 5 — Workflow Transitions (Jira)

Jira workflow history reveals how work actually flowed. Unusual transitions are worth understanding — they may reflect real complexity or they may reflect a habit that obscures progress.

| Check | Pass condition |
|---|---|
| No issues moved directly from Backlog / To Do → Done | Direct jumps should have a comment explaining why |
| No issues stuck In Progress for the entire sprint without updates | Flag issues with no status change and no comments for > 3 days |
| Closed sprints have no issues left in an active state | All in-progress work should have been moved or resolved at sprint close |
| Sub-tasks reflect parent issue status | Sub-tasks still In Progress when parent is Done are a data inconsistency |

### Area 6 — Sprint Closure Hygiene (Jira)

| Check | Pass condition |
|---|---|
| Sprint was formally closed in Jira | Not abandoned or left active past end date |
| Incomplete issues were explicitly moved, not left in the closed sprint | Issues left behind at sprint close are invisible in future planning |
| Velocity is calculable | Completed story points can be totalled for the sprint |

---

## Step 4 — Present the audit report

```
Jira Sprint Audit — [SPRINT NAME]
Audited: [DATE]

Sprint Goal
  ✅ / ⚠️ / ❌  [Check]: [finding]

Issue Hygiene
  ✅ / ⚠️ / ❌  [Check]: [finding]
  (list specific issue keys where relevant)

Story Points & Sizing
  ✅ / ⚠️ / ❌  [Check]: [finding]

Acceptance Criteria & DoD
  ✅ / ⚠️ / ❌  [Check]: [finding]

Workflow Transitions
  ✅ / ⚠️ / ❌  [Check]: [finding]

Sprint Closure
  ✅ / ⚠️ / ❌  [Check]: [finding]

Summary
  [N] passed · [N] warnings · [N] failed

Top recommendations
1. [Most impactful fix — name specific issue keys where possible]
2. ...
```

---

## Step 5 — Offer follow-up actions

Ask the user which, if any, follow-up actions to take:

1. **Add comments to specific issues** — for issues with AC, sizing, or DoD findings, offer to post a comment using the Jira comment tool, addressed to the assignee or PM/PO
2. **Export the report** — output the full audit as a formatted block for the team to paste into Confluence, a Jira ticket, or a Retrospective board
3. **No action** — present the report only

Never take any of these actions without explicit confirmation.

---

## Guardrails

- Never transition issue status, update fields, or reassign issues without explicit user confirmation.
- Never frame findings as individual failures — focus on team patterns and process health.
- If sprint data is incomplete (e.g. Sprint Goal missing, sprint still active), report the gap clearly rather than skipping the check.
- Story point data is team-internal — do not suggest sharing velocity or capacity data outside the team without understanding the user's intent.
- If a pattern appears across multiple sprints (e.g. consistently missing ACs, recurring unlinked bugs), name it as a systemic issue worth raising in the Retrospective.
