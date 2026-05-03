---
compatibility: Requires an Azure DevOps MCP connection (mcp__azure-devops__* or mcp__ado__* tools). Reads from the ADO Retrospectives extension board, wiki pages, or retro-tagged work items depending on how the team tracks retrospectives. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Reads an Azure DevOps retrospective — from the ADO Retrospectives extension board, a wiki page, or retro-tagged work items — and produces a structured summary of what the team said, identifies patterns and recurring themes, audits whether action items are properly tracked as work items with owners, and generates concrete suggestions. Use when anyone says things like "summarize our retro", "audit the retrospective in ADO", "what were the action items from the retro", "review the team's retrospective board", "check our retro follow-up", or "did we capture everything from the retro". Don't use for facilitating a retrospective in real time, Jira projects, or general Scrum questions.
license: MIT
metadata:
    ceremony: Sprint Retrospective
    github-path: skills/audit-sprint-retrospective-ado
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: b9841924960534bcf04ba260ae0a9a5ba818eb29
    perspective: Scrum Master / Team
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: audit-sprint-retrospective-ado
---
# ADO Retrospective Audit

## Purpose

A retrospective that produces no followed-through action items is just venting. This skill reads the retro data from ADO, turns it into a clear structured summary, checks that every action item is properly captured and owned, and surfaces patterns the team can act on — so the Retrospective actually improves the way the team works.

---

## Prerequisites

Requires an active Azure DevOps MCP connection. If `mcp__azure-devops__*` or `mcp__ado__*` tools are not available, stop and inform the user.

---

## Step 1 — Locate the retrospective data

Teams store retrospectives in ADO in different places. Ask:

*"Where does your team record retrospectives — the ADO Retrospectives board extension, a wiki page, or work items tagged as retro action items? Or should I search all three?"*

Then retrieve accordingly:

**ADO Retrospectives extension board**
The extension stores retro content as work items under a hidden area. Search for them using `wit_query_by_wiql` or `search_workitem` with terms like "retrospective", the sprint name, or the board name. Look for items with types or tags like `Retrospective`, `FeedbackItem`, or `ActionItem`.

**Wiki**
Use `wiki_list_pages` to find pages named after the sprint or containing "retrospective", "retro", or "sprint review". Then use `wiki_get_page_content` to retrieve the full content.

**Work items (retro-tagged)**
Use `search_workitem` or `wit_query_by_wiql` to find items tagged `retrospective`, `retro-action`, or similar. Also search for items whose title contains "action item from retro" or whose description links to a retrospective.

**If all three return nothing:** ask the user to paste the retro content directly or share a link to the wiki page.

Store everything found as `$RETRO_DATA`. Note the source(s) for the summary.

---

## Step 2 — Parse and structure the content

Extract and categorise all content from `$RETRO_DATA` into the standard retrospective buckets. Adapt to whatever format the team used:

| Bucket | Common labels used by teams |
|---|---|
| **Went well** | Went well · Liked · Glad · Keep · Continue · Strengths · 👍 |
| **To improve** | Didn't go well · Improve · Stop · Change · Concerns · Needs work · 👎 |
| **Action items** | Action items · To do · Commitments · Next steps · Follow-up · ✅ |
| **Questions / Kudos** | Questions · Shoutouts · Appreciations (capture but treat as secondary) |

For each item capture:
- The text as written by the team
- The author (if recorded)
- Any vote count or upvote score (if the board captured reactions)
- Any linked work item ID (if the retro tool created a ticket)

---

## Step 3 — Produce the retro summary

```
Retrospective Summary — [SPRINT NAME] — [DATE]
Source: [ADO Retrospectives board / Wiki page: [name] / Work items]

━━━  What went well  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
+ [Item] (votes: N, author: Name)
+ [Item]

━━━  What to improve  ━━━━━━━━━━━━━━━━━━━━━━━━━━━
- [Item] (votes: N, author: Name)
- [Item]

━━━  Action items  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
□ [Action item] — Owner: [Name or ⚠️ Unassigned] — Linked item: [#ID or ❌ None]
□ [Action item] — Owner: [Name or ⚠️ Unassigned] — Linked item: [#ID or ❌ None]

━━━  Other  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
? [Question or kudos]
```

If vote counts are available, sort "what to improve" by votes descending — the team already told you what matters most.

---

## Step 4 — Audit the action items

This is the most important part. Retrospective action items are only valuable if they are tracked, owned, and actually worked on.

For each action item, check:

| Check | Pass condition | Severity |
|---|---|---|
| Captured as a work item | Action item has a linked ADO work item (PBI, Task, or Bug) | ❌ Fail |
| Assigned to an owner | Work item has a non-empty `Assigned To` | ❌ Fail |
| Assigned to a sprint | Work item is in the next or current iteration, not the backlog | ⚠️ Warning |
| Specific and actionable | Action item describes a concrete change, not a vague intention ("improve communication") | ⚠️ Warning |
| Has a Definition of Done | It is clear when the action item will be considered complete | ⚠️ Warning |
| Not a duplicate of a prior retro action item | Check recent iterations for similar unresolved items | ⚠️ Warning |

**Check for carry-overs from prior retros:**
Use `wit_query_by_wiql` or `search_workitem` to find work items tagged as retro actions from previous sprints that are still open. A recurring unresolved action item is a signal of a systemic blocker — not just a forgotten task.

---

## Step 5 — Identify patterns and generate suggestions

Look across the full retro content for signals:

**Volume signals**
- Many "went well" items and few "to improve" items → team morale is high; keep the current practices
- More "to improve" than "went well" → team under strain; worth discussing in next retro opener
- Very few items overall → participation was low; consider async input collection next time

**Theme clusters**
Group the "to improve" items into themes. Common clusters:
- **Process** — sprint ceremonies, planning quality, backlog hygiene
- **Technical** — build stability, tech debt, testing
- **Communication** — between team members, with stakeholders, across teams
- **Tooling** — ADO setup, CI/CD, environments
- **Clarity** — story quality, requirements, Sprint Goal

Name the top 1–2 themes clearly: *"The top theme this Sprint was process — 4 of 7 improvement items relate to how work flows from refinement into the sprint."*

**Recurring patterns**
If the same theme appears across multiple retros, surface it: *"'Unclear acceptance criteria' has appeared in 3 consecutive retros without a resolved action item. This suggests the improvement isn't being treated as a sprint commitment — it may need to become a standing agenda item in refinement."*

---

## Step 6 — Present the full audit

```
Retrospective Audit — [SPRINT NAME] — [DATE]

[Retro Summary — from Step 3]

Action Item Audit
  ✅ / ⚠️ / ❌  [#ID or description] — [finding]
  ...

  [N] action items found
  [N] linked to a work item   [N] unlinked
  [N] have an owner           [N] unassigned
  [N] assigned to a sprint    [N] in backlog only

Carry-overs from previous retros
  ⚠️  [Item description] — open since [Sprint name] — [#ID if linked]

Patterns & Themes
  Top theme: [theme] — [N] items
  Secondary theme: [theme] — [N] items
  [Recurring pattern note if applicable]

Suggestions
  1. [Specific, concrete — name item IDs or team members where relevant]
  2. [Second suggestion]
  3. [Third suggestion — max 5 total]
```

---

## Step 7 — Offer follow-up actions

Ask: *"Would you like me to:"*

1. **Create missing work items** — for any action item not yet linked to an ADO ticket, create it via `wit_create_work_item` (type: Task or PBI) with the action item text as the title and the retro sprint as context. Ask for the owner and target iteration before creating.
2. **Post the audit summary to the ADO wiki** — create or update a wiki page for this sprint's retro via `wiki_create_or_update_page`
3. **Add a comment to existing action item work items** — flag the ones missing owners or sprint assignment via `wit_add_work_item_comment`
4. **No action** — output the summary as a clean block to copy-paste

Never take any action without explicit confirmation.

---

## Guardrails

- Never invent retro items. If the data is sparse, report it as sparse.
- Never assign owners to action items without asking — the team decides who owns what.
- Never mark a retro as complete if action items have no owners or no linked work items.
- Frame recurring unresolved items as systemic signals, not individual failures.
- If the retro data source is a wiki page, quote items verbatim — do not paraphrase the team's words in the summary section.
- If no retro data is found at all, say so clearly and ask whether the team held a retro this sprint: *"I couldn't find any retrospective data for this sprint in ADO. Did the team hold a retro? If so, where was it recorded?"*
