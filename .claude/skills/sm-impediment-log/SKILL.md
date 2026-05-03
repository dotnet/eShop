---
compatibility: Works with or without a project management MCP. When ADO MCP is connected, creates and queries impediment work items directly. Falls back to a structured plain-text log. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Helps a Scrum Master capture, track, and resolve impediments raised during Daily Scrums or Sprint events — logging them as ADO work items, assigning owners, setting target resolution dates, and surfacing overdue or recurring blockers. Use when a Scrum Master says things like "log an impediment", "track this blocker", "what are our open impediments", "any blockers from standup", "review our impediment backlog", "this needs escalating", or "what's blocking the team". Do not use for sprint planning, retrospectives, or general task tracking.
license: MIT
metadata:
    ceremony: Daily Scrum
    github-path: skills/sm-impediment-log
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 7805f1f4b460a809bf5c02c277691855fa2e19cd
    perspective: Scrum Master
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: sm-impediment-log
---
# Impediment Log

## Purpose

An impediment is anything outside the team's control that slows or blocks Sprint progress. The Scrum Master's job is to make them visible, get them assigned, and get them resolved — fast. An unlogged impediment is invisible to stakeholders. An impediment with no owner and no target date is a blocker in waiting. This skill makes logging fast and tracking honest.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → `$PM_TOOL = ado`
2. If neither ADO nor Jira → `$PM_TOOL = manual`

---

## Mode detection

The user is either:
- **Logging a new impediment** — phrases like "log this", "we have a blocker", "add an impediment"
- **Reviewing open impediments** — phrases like "what's open", "show impediments", "any overdue blockers"
- **Resolving/updating an impediment** — phrases like "that's resolved", "update the blocker", "close this impediment"

Handle each mode below.

---

## Mode A — Log a new impediment

### Collect the details

Gather (or ask for) the following:

| Field | Description | Required |
|---|---|---|
| Title | One-line summary — what is blocked | Yes |
| Blocker description | What is the impediment, where it came from, why it exists | Yes |
| Affected story/team member | Which Sprint item or person is blocked | Yes |
| Impact | What happens if this is not resolved — does the Sprint goal slip? | Yes |
| Owner | Who is responsible for resolving it (often the SM, sometimes PO or external) | Yes |
| Target resolution date | When must this be resolved to avoid Sprint impact | Yes |
| Escalation required | Does this need to go above the team (management, external vendor, another team)? | Yes |

### Create the work item

**ADO:** use `wit_create_work_item` with type `Impediment` (if the process template supports it) or `Issue`. Set:
- Title: `[BLOCKER] [title]`
- Description: blocker description + impact
- Tags: `impediment`, plus `escalation-needed` if escalation is required
- Priority: 1 if Sprint goal is at risk, 2 otherwise
- Assigned To: owner
- Target Date (custom field or iteration target)

Link to the affected story using `wit_work_items_link` if an ID is available.

**Manual:** output a structured log entry:

```
Impediment logged — [DATE]

Title:    [title]
Affects:  [story or person]
Impact:   [what slips if unresolved]
Owner:    [name]
Resolve by: [date]
Escalate: [Yes / No]

Status: Open
```

---

## Mode B — Review open impediments

**ADO:** use `wit_query_by_wiql` to find all items tagged `impediment` (or type `Impediment`/`Issue`) that are not Resolved/Closed.

Present as a triage table:

```
Open Impediments — [TEAM] — [DATE]

┌──────┬─────────────────────────────┬──────────┬──────────────┬──────────┐
│ ID   │ Title                       │ Owner    │ Resolve by   │ Status   │
├──────┼─────────────────────────────┼──────────┼──────────────┼──────────┤
│ #123 │ [title]                     │ [name]   │ [date]       │ Open     │
│ #124 │ [title]                     │ [name]   │ [date] ⚠ DUE │ Open     │
└──────┴─────────────────────────────┴──────────┴──────────────┴──────────┘

⚠  Overdue (past target date): [N]
⏳  Due within 2 days:          [N]
🔺  Escalation pending:         [N]
```

Flag:
- Any impediment past its target resolution date
- Any impediment that has been open for more than the Sprint length (may have been forgotten or abandoned)
- Any impediment marked for escalation with no escalation action recorded

---

## Mode C — Resolve or update an impediment

Ask: *"What is the ID (or title) of the impediment, and what is the resolution?"*

**ADO:** use `wit_update_work_item` to:
- Set state to Resolved/Closed
- Add a comment via `wit_add_work_item_comment`: *"Resolved [date]: [resolution summary]. Impediment was open for [N] days."*

**Manual:** output:

```
Impediment closed — [DATE]

Title:      [title]
Resolution: [what was done]
Open for:   [N] days
Owner:      [name]
```

---

## Step — Recurring impediment detection

After reviewing open impediments, check for patterns across the current and previous Sprint:

- Has the same type of blocker appeared more than once? (e.g., waiting on design sign-off, external API down, access not provisioned)
- Is the same team or person consistently the source of escalations?

If a pattern exists, surface it: *"This is the third Sprint with an impediment waiting on [dependency]. This is worth raising in the next Retrospective as a systemic issue, not just an individual blocker."*

---

## Guardrails

- An impediment is outside the team's direct control — a developer who is stuck on a technical problem is not an impediment (it is a development challenge). An external dependency that has not responded in three days is an impediment.
- Never close an impediment without a resolution note — "closed" with no context is not useful in a retro or audit.
- If escalation is required, the SM should act immediately — do not log escalation and wait. Surface it to the right person.
- The impediment log is not a task board. Impediments should be resolved fast and closed — they should not become long-running work items. If an impediment cannot be resolved within a Sprint, it may be a structural issue requiring a different kind of action.
