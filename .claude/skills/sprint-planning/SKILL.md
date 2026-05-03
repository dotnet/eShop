---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, fetches the backlog and creates sprint assignments automatically. Falls back to manual mode. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Facilitates Sprint Planning by helping the Scrum Team craft a Sprint Goal, select Product Backlog items, and build an initial Sprint Backlog. Use when anyone says things like "let's plan the sprint", "sprint planning", "what should we work on this sprint", "help us pick stories for the sprint", or "we need a sprint goal". Don't use for backlog refinement, story writing, Daily Scrum, Sprint Review, or Retrospective. Don't use when the team is asking general questions about how Sprint Planning works.
license: MIT
metadata:
    ceremony: Sprint Planning
    github-path: skills/sprint-planning
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: f6f729871f0dfeac33924b2fc92a3642c69e45c4
    perspective: Scrum Team
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: sprint-planning
---
# Sprint Planning

## Scrum Guide grounding

Sprint Planning kicks off the Sprint. The whole Scrum Team attends — Developers, the Product Owner, and the Scrum Master. It is time-boxed to eight hours for a one-month Sprint (shorter for shorter Sprints).

The event answers three questions:

| Topic | Question |
|---|---|
| **Why** | What is the Sprint Goal — the single objective that makes this Sprint worthwhile? |
| **What** | Which Product Backlog items can the Developers commit to completing? |
| **How** | How will the selected work get done? |

The Sprint Goal is the output that matters most. It gives the team coherence and the flexibility to negotiate scope during the Sprint.

---

## Tool detection

Identify which project management tool is available before proceeding:

1. Check for active `mcp__azure-devops__*` tools → set `$PM_TOOL` to `ado`
2. Otherwise check for active `mcp__jira__*` tools → set `$PM_TOOL` to `jira`
3. If both → ask: *"I see both ADO and Jira connected — which should I use?"*
4. If neither → set `$PM_TOOL` to `manual`

---

## Step 1 — Establish context

Ask for any missing context before pulling data:

- *"What is the Sprint length?"* (default: 2 weeks)
- *"What is the team's capacity this Sprint?"* (total developer-days, accounting for time off)
- *"Is there a Product Goal or theme the Sprint should contribute to?"*

Store as `$SPRINT_LENGTH`, `$CAPACITY`, `$PRODUCT_GOAL`.

---

## Step 2 — Fetch the top of the Product Backlog

Retrieve the ordered, refined items at the top of the backlog:

- **ADO:** Before calling any backlog or team tool, resolve project and team in this exact order:
  1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
  2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
  3. Only then call `wit_list_backlogs` with `project: $PROJECT` and `team: $TEAM` to confirm available backlog levels, then `wit_list_backlog_work_items` to retrieve ordered items. Never call these tools with null values for either parameter.
- **Jira:** use the equivalent backlog listing tool, ordered by rank
- **Manual:** ask the Product Owner to paste or describe the top 10–15 items

Present the items as a numbered list with ID, title, and current state. Flag any item that appears unrefined (no acceptance criteria, no size estimate) with a ⚠️.

---

## Step 3 — Craft the Sprint Goal

A good Sprint Goal is:
- A single, coherent objective — not a list of features
- Outcome-oriented, not a task list
- Achievable within the Sprint
- Meaningful to stakeholders

Suggest a draft Sprint Goal based on the top backlog items and the Product Goal. Examples of good framing:
- *"Enable customers to complete checkout without a phone call to support."*
- *"Reduce report generation time so finance can close the books faster."*

Ask: *"Does this capture what this Sprint is for? Adjust it until it feels right."*

Store the confirmed goal as `$SPRINT_GOAL`.

---

## Step 4 — Select the Sprint Backlog

Guide the team to select items that:
1. Directly serve the Sprint Goal
2. Fit within `$CAPACITY`
3. Meet the Definition of Ready (refined, sized, unblocked)

Present a proposed selection. For each item, note:
- Whether it contributes to the Sprint Goal (✅) or is additional scope (📌)
- Any refinement gaps that should be addressed before committing

If the team wants to add items beyond what capacity allows, name the trade-off explicitly: *"Adding #1234 would fill remaining capacity — is that the right call?"*

---

## Step 5 — Confirm and create the Sprint

Once the team confirms the Sprint Goal and backlog selection:

Present a summary:

```
Sprint Goal: [SPRINT_GOAL]

Committed items ([N] total):
- [#ID] [Title] — [contributes to goal / additional scope]
- ...

Items deferred to next Sprint:
- [#ID] [Title]
```

Ask: *"Does this reflect the team's plan? Say 'looks good' to finalize, or tell me what to adjust."*

After confirmation:

- **ADO:** use `work_create_iterations` or `work_assign_iterations` to create/assign the sprint; use `wit_update_work_item` to move items into the iteration
- **Jira:** use the sprint creation and issue-assignment tools
- **Manual:** output the summary in a clean block for the team to act on

---

## Guardrails

- Never set the Sprint Goal on behalf of the Product Owner without confirmation.
- Never move items into a sprint without explicit team confirmation.
- Never estimate story points or size — that belongs to the Developers.
- If a proposed Sprint Goal is actually a list of features, coach gently: *"A Sprint Goal is usually a single outcome. Would [X] work as the unifying purpose?"*
- If capacity is unknown, proceed but flag that commitment is approximate.
- Never schedule work beyond the Sprint length — call out over-commitment explicitly.
