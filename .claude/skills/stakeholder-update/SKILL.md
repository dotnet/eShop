---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, reads Sprint state and work item progress automatically. Falls back to a conversational briefing. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Drafts a clear, concise stakeholder update — translating Sprint progress, risks, and delivery forecasts into business language that non-technical stakeholders can act on. Use when a Product Owner or Scrum Master says things like "write a stakeholder update", "draft a status update", "send an update to the business", "prepare a progress report", "what do I tell the steering committee", "draft the weekly update", or "communicate sprint progress". Do not use for Sprint Review facilitation — use sprint-review for that. Do not use for release notes — use po-release-notes for that.
license: MIT
metadata:
    ceremony: Sprint
    github-path: skills/stakeholder-update
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: cb9009f0cd5d80dd563ff40754cb20ba33c17642
    perspective: Product Owner / Scrum Master
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: stakeholder-update
---
# Stakeholder Update

## Purpose

Stakeholders don't read ADO boards. They need to know three things: are we on track, is there anything they should care about, and what is coming next. This skill reads the current Sprint state and drafts that message — translated out of Scrum jargon and into business language — so the PO or SM can send it in minutes, not hours.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → `$PM_TOOL = ado`
2. Check for active `mcp__jira__*` tools → `$PM_TOOL = jira`
3. If neither → `$PM_TOOL = manual`

---

## Step 1 — Understand the context

Ask: *"Who is this update for, and what format do they prefer — a brief Slack/Teams message, an email, or a more formal status report?"*

| Audience | Format | Length |
|---|---|---|
| Executive / steering committee | Formal status report or email | 1 page max — headline status, risks, decisions needed |
| Business stakeholders | Email or Teams message | 3–5 bullet points — progress, risks, next steps |
| Internal team / PO | Brief Slack message | 2–3 sentences — what's done, what's at risk |

Also ask: *"Is there anything specific you want me to highlight or leave out?"*

Store as `$AUDIENCE` and `$FORMAT`.

---

## Step 2 — Read the current Sprint state

- **ADO:** Before calling any iteration or team tool, resolve project and team in this exact order:
  1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
  2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
  3. Call `work_list_team_iterations` with `project: $PROJECT` and `team: $TEAM` to identify the active iteration. Store as `$ITERATION`.
  4. Only then call `wit_get_work_items_for_iteration` and `work_get_team_settings` using the resolved project, team, and iteration. Never call these tools with null values for project or team.
- **Jira:** use the active sprint reporting tools to read issue states and progress.
- **Manual:** ask *"Give me a quick brief — what has been completed, what is in progress, what is at risk, and how many days remain in the Sprint?"*

Calculate:
- Sprint progress: days elapsed vs. total Sprint days
- Story point progress: points Done vs. total committed
- Items at risk: any work item In Progress with less than 2 days to Sprint end and not nearly complete

---

## Step 3 — Assess Sprint health

Before writing, form a clear picture of the Sprint's trajectory:

| Signal | What it means |
|---|---|
| Progress matches days elapsed (±10%) | On track |
| Progress lags days elapsed by >20% | At risk — volume of work may not complete |
| Sprint Goal items are all Done or In Progress | Goal is achievable |
| Sprint Goal items have not started | Sprint Goal is at risk — this needs flagging |
| Unplanned work was added mid-Sprint | Scope change — may affect commitment |
| Blockers are unresolved near Sprint end | Items will carry over |

If the Sprint Goal is at risk, this is the single most important thing to communicate.

---

## Step 4 — Draft the update

Use the appropriate template for the requested format.

### Template A — Brief message (Slack / Teams)

```
Sprint [N] update — [date]

✅ Done: [1–3 bullet points of completed outcomes in user language]
🔄 In progress: [what the team is currently working on]
⚠ Risk: [one sentence on any risk — or "No blockers" if clear]

Sprint ends [date].
```

### Template B — Email to business stakeholders

```
Subject: Sprint [N] Progress Update — [Project Name]

Hi [name / team],

Here is a brief update on Sprint [N] progress as of [date].

**Overall status: [🟢 On Track / 🟡 At Risk / 🔴 Off Track]**

**This Sprint's goal:** [Sprint Goal in plain language]

**Progress so far:**
- [Completed outcome 1 — user-facing language]
- [Completed outcome 2]
- [What is currently in progress and when it is expected to complete]

**Risks or issues:**
[One to three lines — only genuine risks. If there are none, say "No significant risks at this time."]

**Coming up next:**
[One or two sentences on what the next Sprint or the remainder of this Sprint will focus on]

Questions or concerns? Reply to this email or reach out directly.

[Name]
```

### Template C — Formal status report (steering committee / programme level)

```
Project: [name]
Sprint: [N]   Period: [start date] – [end date]
Report date: [today]
Status: [🟢 On Track / 🟡 At Risk / 🔴 Off Track]

─── Sprint Goal ─────────────────────────────────────────

[Sprint Goal in one sentence]
Achievement forecast: [Likely / At Risk / Not Achievable] — [one sentence reason]

─── Delivery progress ───────────────────────────────────

Story points completed: [N] of [N] committed ([X]%)
Sprint days elapsed:    [N] of [N] ([X]%)

Completed this Sprint:
  - [Outcome 1]
  - [Outcome 2]

In progress:
  - [Item] — expected completion [date]

─── Risks and issues ────────────────────────────────────

[Table or bullets: Risk / Likelihood / Impact / Owner / Mitigation]
[Or: "No risks to escalate at this time."]

─── Decisions needed ────────────────────────────────────

[Any decision the stakeholder needs to make, with deadline. Or: "None at this time."]

─── Next Sprint preview ─────────────────────────────────

The team will focus on [theme/goal] next Sprint, starting [date].
```

---

## Step 5 — Review and send

Present the draft and ask: *"Does this capture the right message? Any adjustments before I finalise?"*

Apply corrections. Once confirmed, offer to:
- Post to a Teams/Slack channel (if an integration is available)
- Create a Wiki page in ADO (`mcp__azure-devops__wiki_create_or_update_page`) for a running status log
- Copy the final text to paste into email

---

## Guardrails

- Never spin negative news into positive framing — if the Sprint Goal is at risk, say so clearly. Stakeholders need accurate information to make decisions.
- Never use Scrum jargon in stakeholder-facing updates: "velocity", "story points", "sprint backlog", "impediment", "backlog refinement" are internal terms. Translate them into plain language.
- Never include internal ADO item IDs or technical implementation details in a business-facing update — these are meaningless and reduce trust.
- If the PO asks to omit a genuine risk from the stakeholder update, note: *"I'd recommend including this — stakeholders who are surprised later tend to lose trust faster than stakeholders who are informed early."* Then respect their decision.
- Keep all formats short. A stakeholder update that is three pages long will not be read. One page is the maximum; half a page is better.
