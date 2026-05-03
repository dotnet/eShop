---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, fetches completed items and sprint data automatically. Falls back to manual mode. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Facilitates the Sprint Review by presenting the increment, inspecting progress toward the Product Goal, and capturing Product Backlog adaptations. Use when anyone says things like "sprint review", "demo prep", "what did we complete this sprint", "review the increment", "what's the Product Backlog looking like after this sprint", or "stakeholder demo". Don't use for the Retrospective, Daily Scrum, Sprint Planning, or general questions about how Sprint Reviews work. Don't use when the team just wants a status report.
license: MIT
metadata:
    ceremony: Sprint Review
    github-path: skills/sprint-review
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: adf01f3befab282ae6415e2beddcd9684d09e5e5
    perspective: Scrum Team
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: sprint-review
---
# Sprint Review

## Scrum Guide grounding

The Sprint Review is a working session, not a presentation. The Scrum Team and stakeholders inspect what was accomplished and how the environment has changed. The output is an adapted Product Backlog that reflects the current best opportunity going forward.

Key rules:
- Time-boxed to four hours for a one-month Sprint (proportionally shorter for shorter Sprints).
- The increment is inspected — not just described. Stakeholders ask questions and give input.
- The Product Backlog may be adjusted based on what was learned.
- Velocity and burn-down metrics are inputs, not the focus.

---

## Tool detection

Identify which project management tool is available:

1. Check for active `mcp__azure-devops__*` tools → set `$PM_TOOL` to `ado`
2. Otherwise check for active `mcp__jira__*` tools → set `$PM_TOOL` to `jira`
3. If both → ask: *"I see both ADO and Jira connected — which should I use?"*
4. If neither → set `$PM_TOOL` to `manual`

---

## Step 1 — Fetch Sprint data

Retrieve the completed Sprint's items:

- **ADO:** Before calling any iteration tool, resolve project and team in this exact order:
  1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
  2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
  3. Call `work_list_team_iterations` with `project: $PROJECT` and `team: $TEAM` to identify the most recently completed iteration. Store as `$ITERATION`.
  4. Only then call `wit_get_work_items_for_iteration` with the resolved project, team, and iteration path; filter for Done/Closed state. Never call iteration tools with null values for project or team.
- **Jira:** use the sprint report or issue listing tool for the closed sprint
- **Manual:** ask the team to share what was completed, what wasn't, and the Sprint Goal

Identify:
- `$SPRINT_GOAL` — the goal set at Sprint Planning
- `$DONE_ITEMS` — work items that meet the Definition of Done
- `$NOT_DONE_ITEMS` — work items that were planned but not completed
- `$PRODUCT_GOAL` — the longer-horizon objective the team is working toward (if known)

---

## Step 2 — Assess the Sprint Goal

State clearly whether the Sprint Goal was met, partially met, or not met — and why.

- **Met:** The increment delivered the intended outcome.
- **Partially met:** Some committed scope was not done, but the core outcome was achieved.
- **Not met:** The team did not deliver the outcome. Name the cause without assigning blame.

A missing Sprint Goal is a team health signal worth naming: *"The team didn't have a Sprint Goal this Sprint — that makes it hard to assess whether the Sprint was successful. Worth raising in the Retrospective."*

---

## Step 3 — Present the increment

Structure the review of `$DONE_ITEMS`:

For each completed item, prepare a one-line summary suitable for a stakeholder audience:
- What it does (user-facing language, not technical)
- Which part of the Product Goal it advances

Example:
```
#1234 — Export transactions as CSV
  Customers can now download their full transaction history for any date range.
  Contributes to: "Make it easy for customers to manage their own finances."
```

Flag items in `$NOT_DONE_ITEMS` briefly — not as failures, but as context for the backlog discussion.

---

## Step 4 — Capture stakeholder input

Prompt the team to note the key inputs received during the review:

*"What feedback or new information came from stakeholders that should influence the backlog?"*

Accept free-form input. Group into:
- **New opportunities** — things to add or prioritize
- **Changes in direction** — items to deprioritize or remove
- **Risks or concerns** — items that need a closer look

---

## Step 5 — Adapt the Product Backlog

Based on what was done, what wasn't, and stakeholder input, produce a set of recommended backlog actions:

| Action | Item | Reason |
|---|---|---|
| Reprioritize | [#ID Title] | [why it moves up/down] |
| Add | [new item description] | [stakeholder request or new insight] |
| Remove | [#ID Title] | [no longer relevant] |
| Carry over | [#ID Title] | [not done, still valuable] |

Ask: *"Do these backlog changes reflect the team's intent? Confirm to apply, or tell me what to adjust."*

After confirmation, apply changes via the PM tool or output them for manual action.

---

## Step 6 — Output the review summary

```
Sprint Review — [SPRINT NAME / NUMBER] — [DATE]

Sprint Goal: [SPRINT_GOAL] — [Met / Partially Met / Not Met]

Increment (Done)
- [#ID] [Title]: [one-line stakeholder description]

Not completed this Sprint
- [#ID] [Title]: [brief note — carry over / deprioritized / blocked]

Stakeholder input
- [Key feedback or decision]

Product Backlog adaptations
- [Action]: [#ID / description]

Next Sprint outlook
- [Any relevant context: upcoming events, team changes, Product Goal milestone]
```

---

## Guardrails

- Never present undone items as done. The Definition of Done is the team's standard.
- Never make backlog changes without explicit Product Owner confirmation.
- Never frame incomplete work as a team failure — focus on learning and adaptation.
- If no stakeholders attended, note it: the Sprint Review without external input reduces its value.
- Keep the tone collaborative and forward-looking — this is an inspection event, not a performance review.
