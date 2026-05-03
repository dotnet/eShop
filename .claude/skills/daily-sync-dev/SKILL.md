---
compatibility: Requires a project management MCP (Azure DevOps or Jira) for full functionality. Falls back to manual mode when no MCP is connected. Works with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Prepares a developer's Daily Scrum update by pulling assigned Sprint Backlog items from the connected project management tool, checking recent git activity, and composing a concise Sprint-Goal-focused statement. Use when a developer says things like "prepare my standup", "daily sync update", "what did I work on", "help me with standup", or "what's my daily update". Don't use for sprint planning, backlog refinement, retrospectives, or any other Scrum ceremony. Don't use when the user is asking general questions about Scrum or the Daily Scrum format.
license: MIT
metadata:
    ceremony: Daily Scrum
    github-path: skills/daily-sync-dev
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 0385bbeadab3cd45266de322b34f33a17668f1bb
    perspective: Developer
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 2.0.0
name: daily-sync-dev
---
# Daily Scrum — Developer Update

## Scrum Guide grounding

The **Daily Scrum** is a 15-minute event for Developers. Its purpose is to inspect progress toward the Sprint Goal and produce an actionable plan for the next day.

Key rules this skill enforces:
- It is not a status report to management or the Scrum Master.
- The 2020 Scrum Guide removed the mandatory three questions — any structure that serves the team is valid.
- Focus is on the **Sprint Goal**, not individual task counts.

---

## Tool detection

Identify which project management tool is available before proceeding:

1. Check for active `mcp__azure-devops__*` tools → set `$PM_TOOL` to `ado`
2. Otherwise check for active `mcp__jira__*` tools → set `$PM_TOOL` to `jira`
3. If both are available → ask: *"I see both ADO and Jira connected — which should I use?"*
4. If neither is available → set `$PM_TOOL` to `manual`

---

## Step 1 — Identify the developer

Ask: *"What's your name or username as it appears in your PM tool?"*

Store as `$DEVELOPER`. If skipped, proceed with whatever items are returned and note that attribution may be incomplete.

---

## Step 2 — Fetch the Sprint Goal

Retrieve the Sprint Goal from the current sprint or iteration.

- **ADO:** Query the current iteration node for its goal field.
- **Jira:** Query the active sprint's `goal` field.
- **Manual:** Ask the developer directly.

Store as `$SPRINT_GOAL`. If unavailable, proceed and note that a missing Sprint Goal is a team health issue worth raising.

---

## Step 3 — Fetch Sprint Backlog items

Use the available MCP tools to retrieve work items assigned to `$DEVELOPER` in the current sprint:

- **ADO:** Before calling any iteration tool, resolve project and team in this exact order:
  1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
  2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
  3. Call `work_list_team_iterations` with `project: $PROJECT` and `team: $TEAM` to identify the current iteration. Store as `$ITERATION`.
  4. Only then call `wit_my_work_items` or `wit_get_work_items_for_iteration` with the resolved project, team, and iteration. Never call iteration tools with null values for project or team.
- **Jira:** use the equivalent sprint issue listing tool
- **Manual:** ask the developer to paste their items directly into the chat

Group the results into:

| Bucket | Criteria |
|---|---|
| **Done since last sync** | State moved to Done / Resolved / Closed in the last 24 h |
| **In Progress** | Active / In Progress state |
| **Planned** | To Do / New — sprint-committed but not started |

---

## Step 4 — Fetch recent git activity *(optional)*

Run the following if a git repository is present:
```bash
git log --oneline --since="yesterday 00:00" --author="$DEVELOPER"
```
Use commit messages to enrich the "Done" bucket. Skip silently if unavailable or empty.

---

## Step 5 — Ask about blockers

Ask one focused question:
*"Is anything slowing you down or blocking progress toward the Sprint Goal?"*

Accept free-form input. Do not troubleshoot the blocker — note it for the update.

---

## Step 6 — Compose the update

Compose the update using the Sprint Goal as the organizing lens:

```
Daily Scrum — [DEVELOPER NAME] — [DATE]

Sprint Goal: [SPRINT_GOAL]

Progress toward Sprint Goal
- [#ID Title]: [what changed or was accomplished]

Plan for today
- [#ID Title]: [specific next action — not "continue working on X"]

Blockers / Impediments
- [Description] — OR — None.
```

Drafting rules:
1. Reference work item IDs in the tool's native format (e.g., `#1234`, `PROJ-456`)
2. Each "Plan for today" bullet must name a concrete next action, not a vague continuation
3. Items unrelated to the Sprint Goal are listed separately with a scope-drift note
4. Maximum 10 bullets total — group minor tasks
5. No estimates, percentages, or hours
6. Tone is peer-to-peer, not a report to a manager

---

## Step 7 — Review and finalize

Present the draft and ask:
*"Does this reflect what you want to share with the team? Say 'looks good' to finalize, or tell me what to adjust."*

Apply corrections and re-present. Once confirmed, output the final update in a clean code block ready to copy-paste.

---

## Guardrails

- Never invent work item details. If the PM tool returns nothing, say so and ask for context.
- Never position the output as a management status report.
- Never update work item state without explicit developer confirmation.
- Never include credentials, tokens, or PII in the update.
- Adapt the format if the team uses a different standup structure — the Sprint Goal focus is what matters, not the template.
- When a developer starts troubleshooting a blocker, note it and redirect: *"Let's capture that and dig in after the standup."*
