---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, reads completed work items from the current or most recent Sprint automatically. Falls back to a pasted list of items. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Generates audience-appropriate release notes from completed Sprint work items — translating technical changes into plain-language user outcomes for stakeholders, customers, or internal teams. Use when a Product Owner says things like "write release notes", "what shipped this Sprint", "generate a changelog", "what did we deliver", "draft the release email", or "summarise what we released". Do not use for sprint review facilitation — use sprint-review for that. Do not use for velocity analysis.
license: MIT
metadata:
    ceremony: Sprint Review
    github-path: skills/po-release-notes
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 61fac60f1daa5a042b81266a68ec9aa725880f59
    perspective: Product Owner
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: po-release-notes
---
# Release Notes Generator

## Purpose

Release notes are not a commit log. Stakeholders and customers do not care which service was refactored — they care what they can do now that they couldn't do before. This skill reads the Sprint's completed work items and translates them into outcomes, grouped by theme, in language the audience actually understands.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → `$PM_TOOL = ado`
2. Check for active `mcp__jira__*` tools → `$PM_TOOL = jira`
3. If neither → `$PM_TOOL = manual`

---

## Step 1 — Identify the Sprint and audience

Ask: *"Which Sprint are we writing release notes for? And who is the audience — customers, internal stakeholders, or both?"*

| Audience | Tone | Detail level |
|---|---|---|
| **Customers / end users** | Plain language, benefit-focused | Feature outcomes only; no internal jargon |
| **Internal stakeholders** | Business language, outcome + context | Feature outcomes plus metrics/context |
| **Technical stakeholders** | More detail allowed | May include architecture changes, migrations |

Store as `$SPRINT` and `$AUDIENCE`.

---

## Step 2 — Fetch completed work items

- **ADO:** Before calling any iteration tool, resolve project and team in this exact order:
  1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
  2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
  3. Call `work_list_team_iterations` with `project: $PROJECT` and `team: $TEAM` to confirm the iteration path for `$SPRINT`.
  4. Only then call `wit_get_work_items_for_iteration` filtered to Done/Closed state, or use `wit_query_by_wiql` for the iteration. Read title, description, acceptance criteria, and type (Story, Bug, Task, Feature). Never call iteration tools with null values for project or team.
- **Jira:** use sprint reporting tools to list resolved issues. Read summary, description, issue type, and fix version.
- **Manual:** ask *"Paste the list of completed items — title, type, and a one-line description is enough."*

Exclude purely technical work items (infrastructure tasks, pipeline fixes, dependency bumps) unless they have user-visible impact. When in doubt, ask the PO.

---

## Step 3 — Group and categorise

Before writing, cluster the completed items into meaningful themes. Good themes are user-facing capability areas, not technical layers.

Good groupings:
- "Reporting" — items related to dashboards, exports, data views
- "Notifications" — items related to alerts, emails, push
- "Performance & reliability" — items that improved speed or fixed errors users experienced

Avoid groupings like "Backend", "Database", "Refactor" — these are technical, not audience-facing.

If the Sprint only contains one or two items, a flat list is fine — don't force artificial groupings.

---

## Step 4 — Write the release notes

Use this structure, adapting length to the number of items:

```markdown
# Release Notes — [Sprint Name or Version] — [Date]

## What's new

### [Theme 1]

**[Feature or capability name]**
[One to two sentences: what the user can now do, and why it matters.
No jargon. No technical implementation detail. Start from the user's perspective.]

**[Feature or capability name]**
[One to two sentences.]

### [Theme 2]
...

## Bug fixes

- **[Short description of what was broken]** — [What the user experienced before and what it does now.]
- ...

## Known issues
[Optional. List any items that shipped with known limitations, or leave this section out.]
```

### Writing rules

- **Lead with the outcome, not the feature name.** "You can now export reports as PDF" beats "PDF export is now available."
- **One sentence per item is enough** unless context genuinely requires more.
- **Never describe the implementation** — "We refactored the reporting service" tells the user nothing. "Reports now load in under two seconds" tells them everything.
- **Bug fix entries** should say what the user experienced before, not what the code did: "Fixed: the dashboard was blank after login for accounts created before March" not "Fixed: null pointer in session initialiser."

---

## Step 5 — Adapt per audience

If multiple audiences were requested, generate a separate version for each:

**Customer version** — shortest. Benefit-only. Strip all internal context, metrics, and process notes. Written as if it will appear in a product changelog or release email.

**Stakeholder version** — add Sprint context: what the team focused on, any scope changes, what carries to next Sprint. May include a brief "what's next" preview.

**Technical version** — may include migration steps, API changes, or configuration updates if relevant.

---

## Step 6 — Review and finalise

Present the draft(s) and ask: *"Does this capture what shipped accurately? Any items to add, remove, or reword before I finalise?"*

Apply corrections. Once confirmed, output the final notes ready to:
- Paste into an ADO Wiki page (`mcp__azure-devops__wiki_create_or_update_page` if available)
- Copy to a Confluence/Notion page
- Drop into a Slack release announcement
- Send as a Sprint Review email

---

## Guardrails

- Never describe a bug fix as a new feature.
- Never include work that is In Progress or not Done by the Sprint end — partial work belongs in the next release.
- If acceptance criteria reveal a story was only partially implemented, flag it: *"Story [N] has unmet ACs — confirm it should appear in release notes as complete."*
- Keep the customer version free of internal terminology: Sprint names, team names, ADO IDs, and ticket numbers are internal artefacts unless the audience is internal.
