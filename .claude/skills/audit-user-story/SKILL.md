---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, fetches the story by ID and can post the audit as a comment. Falls back to reviewing a story pasted into the chat. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Audits an existing user story against Scrum best practices — checks INVEST criteria, story format, acceptance criteria quality, and Definition of Ready compliance. Raises findings with clear severity levels and offers to post the audit as a comment on the work item for the Product Owner or PM to act on. Use when anyone says things like "audit this story", "review this user story", "is this story ready", "check this PBI", "does this story meet best practices", or pastes a story and asks for feedback. Don't use for creating new stories, sprint planning, or general Scrum questions. Works with ADO, Jira, or a pasted story.
license: MIT
metadata:
    ceremony: Backlog Refinement
    github-path: skills/audit-user-story
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 2ebfe3780cce35f6417d71b9e8e7c6a822d97b82
    perspective: Developer / Scrum Master / Product Owner
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: audit-user-story
---
# User Story Audit

## Purpose

A story that is unclear, too large, or untestable wastes the whole team's time — in refinement, in planning, and in delivery. This skill runs a structured audit so issues are caught before the story enters a Sprint, not during one.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → set `$PM_TOOL` to `ado`
2. Otherwise check for active `mcp__jira__*` tools → set `$PM_TOOL` to `jira`
3. If both → ask: *"I see both ADO and Jira connected — which should I use?"*
4. If neither → set `$PM_TOOL` to `manual`

---

## Step 1 — Fetch the story

- **ADO / Jira:** Ask: *"What's the work item ID or issue key you'd like me to audit?"* Then retrieve the full item including title, description, acceptance criteria, state, and any linked items.
- **Manual:** Ask the user to paste the story content directly.

Store the content as `$STORY`. If the item cannot be found, say so and ask the user to paste it instead.

---

## Step 2 — Run the audit

Evaluate `$STORY` across four areas. Grade each check as:
- ✅ **Pass** — meets the standard
- ⚠️ **Warning** — present but weak; worth improving
- ❌ **Fail** — missing or clearly broken; blocks readiness

### Area 1 — Story Format

| Check | Pass condition |
|---|---|
| Title is action-oriented | Starts with a verb + noun (e.g. "View monthly summary") |
| Story statement present | Has "As a / I want / So that" or equivalent |
| Persona is specific | Not "user", "the system", or "admin" |
| "So that" states value | Names an outcome or benefit, not a restatement of the want |
| No implementation detail in the story | Story describes *what*, not *how* |

### Area 2 — Acceptance Criteria

| Check | Pass condition |
|---|---|
| ACs present | At least 3 criteria exist |
| ACs are conditions, not UI steps | Each AC describes an observable outcome, not a sequence of clicks |
| ACs are independently verifiable | Each AC can be tested without depending on another |
| Edge cases covered | Error states, empty states, or boundary conditions addressed where relevant |
| No ambiguous language | Avoid "should", "might", "fast", "easy" — prefer measurable specifics |

### Area 3 — INVEST

| Criterion | What to look for |
|---|---|
| **Independent** | Can this be delivered without another unfinished story? |
| **Negotiable** | Is the story a conversation starter, not a rigid spec? |
| **Valuable** | Does it deliver a real outcome to a user or the business? |
| **Estimable** | Enough detail for developers to size it? |
| **Small** | Completable within one Sprint by the team? |
| **Testable** | Can the ACs be verified objectively? |

Flag only the INVEST criteria that are genuinely at risk — not all six for every story.

### Area 4 — Definition of Ready

| Check | Pass condition |
|---|---|
| Persona named and understood | Team knows who this is for |
| Business value clear | PO can explain why this is prioritized now |
| Dependencies identified | Blockers or upstream dependencies are named |
| Size estimate present | Story has been sized (optional but flag absence) |
| No open assumptions | Any assumptions are captured and resolved |

---

## Step 3 — Present findings

Format the audit output as follows:

```
User Story Audit — [#ID / Issue Key] [Title]
Audited: [DATE]

Story Format
  ✅ / ⚠️ / ❌  [Check]: [brief finding]

Acceptance Criteria
  ✅ / ⚠️ / ❌  [Check]: [brief finding]

INVEST
  ✅ / ⚠️ / ❌  [Criterion]: [brief finding]

Definition of Ready
  ✅ / ⚠️ / ❌  [Check]: [brief finding]

Overall readiness: Ready / Needs work / Not ready

Top recommendations
1. [Most important fix — be specific, offer a rewrite if helpful]
2. [Second fix]
3. [Third fix]
```

Keep findings concise — a one-line note per check is enough unless a rewrite is warranted. Offer a specific rewrite for the persona, "So that" clause, or individual ACs when the existing text is weak.

---

## Step 4 — Offer to comment

Ask: *"Should I add these findings as a comment on the work item for the Product Owner to review?"*

If yes, post a comment via the PM tool using `wit_add_work_item_comment` (ADO) or the equivalent Jira comment tool. Frame the comment as constructive peer feedback, not a rejection:

> **Story Audit Findings** — [DATE]
>
> This story was reviewed against Scrum best practices. Here are the findings:
>
> [Paste the formatted findings block]
>
> These are suggestions to help the team get the most value from this story — happy to discuss any of them in refinement.

If `$PM_TOOL` is `manual`, output the comment text in a clean block for the user to paste manually.

---

## Guardrails

- Never reject a story outright — frame every finding as an improvement opportunity.
- Never suggest story points or estimates — sizing belongs to the Developers.
- Never rewrite the full story without being asked — offer rewrites for specific weak sections only.
- If the story is clearly an epic (covers multiple independent workflows), say so clearly: *"This reads like an epic — the most valuable single story might be [X]. Want me to help split it?"*
- Keep the tone peer-to-peer. The audit is a team quality tool, not a gate kept by a single role.
