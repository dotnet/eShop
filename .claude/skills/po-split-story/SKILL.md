---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, fetches the story by ID and can create the split stories directly in the backlog. Falls back to a pasted story. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Applies proven story splitting patterns to break an oversized user story or epic into sprint-sized, independently deliverable stories. Use when a Product Owner or developer says things like "this story is too big", "we need to split this", "this will not fit in a sprint", "break this epic down", "help me slice this story", or when a story fails the Small criterion in INVEST. Do not use for writing new stories from scratch — use po-create-user-story instead. Do not use for sprint planning or backlog prioritization.
license: MIT
metadata:
    ceremony: Backlog Refinement
    github-path: skills/po-split-story
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: e7bee2dbee4a3345620576794d8bea9ba82a4f8e
    perspective: Product Owner / Developer
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: po-split-story
---
# Story Splitting

## Purpose

A story that cannot fit in one Sprint is a planning liability — it blocks commitment, obscures progress, and almost always hides scope nobody agreed to. This skill applies the nine proven splitting patterns to turn one large story into multiple small ones, each independently valuable and completable in a single Sprint.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → `$PM_TOOL = ado`
2. Check for active `mcp__jira__*` tools → `$PM_TOOL = jira`
3. If both → ask which tool to use
4. If neither → `$PM_TOOL = manual`

---

## Step 1 — Fetch the story

Ask: *"Which story needs splitting? Provide the ID or paste the content."*

- **ADO:** use `wit_get_work_item` — read title, description, acceptance criteria, story points, and parent
- **Jira:** use the get-issue tool — read summary, description, acceptance criteria, story points, and epic link
- **Manual:** accept pasted content

If the item is an Epic, treat the whole Epic as the input. Store as `$STORY`.

---

## Step 2 — Diagnose why it is too large

Before splitting, understand *what* makes it large. This determines which pattern fits best.

Ask yourself — and the PO if needed:
- Does it cover **multiple user types**? → split by persona
- Does it span **multiple workflow steps** end-to-end? → split by workflow
- Does it handle **many data variations or edge cases**? → split by data
- Does it cover **both the happy path and error handling**? → split happy/unhappy
- Does it need to work across **multiple channels** (web, mobile, API)? → split by interface
- Does it include **both basic and high-performance** versions? → split by performance
- Does it bundle **create, read, update, delete**? → split by operation (CRUD)
- Does it mix **a core feature with validation, logging, or notifications**? → split cross-cutting concerns
- Does it depend on **a system integration that is not ready**? → defer the integration, stub it first

---

## Step 3 — Apply the best-fit pattern

### Pattern 1 — Workflow steps
Split the story at each step in the user's journey. Each step becomes its own story.

```
Original: As a manager, I want to approve expense claims end-to-end.

Split:
→ Submit an expense claim for approval
→ Review pending expense claims as a manager
→ Approve or reject an individual claim
→ Receive a decision notification as a claimant
```

### Pattern 2 — Business rule variations
Keep the happy path in one story. Each significant business rule variation becomes its own story.

```
Original: Apply discount rules to an order at checkout.

Split:
→ Apply a fixed percentage discount code at checkout
→ Apply a tiered loyalty discount based on account history
→ Apply free-shipping thresholds to qualifying orders
```

### Pattern 3 — Happy path / unhappy path
Deliver the working case first. Error handling, edge cases, and validation follow as separate stories.

```
Original: Export transaction history with full validation.

Split:
→ Export transaction history as CSV for a valid date range [happy path]
→ Show an error when the date range exceeds 12 months [unhappy path]
→ Handle export timeouts gracefully for large datasets [edge case]
```

### Pattern 4 — Data variations
Same operation, different data types or states.

```
Original: Manage all document types in the file library.

Split:
→ Upload and preview PDF documents
→ Upload and preview image files
→ Upload and preview spreadsheets
```

### Pattern 5 — Interface / channel
Deliver one channel first. Other channels follow as separate stories.

```
Original: Receive low-balance alerts across all channels.

Split:
→ Receive low-balance alerts via email
→ Receive low-balance alerts via push notification
→ Receive low-balance alerts via SMS
```

### Pattern 6 — Operations (CRUD)
Split by create, read, update, delete. Read is usually the most valuable starting point.

```
Original: Manage team members.

Split:
→ View the list of current team members [Read]
→ Add a new team member [Create]
→ Update a team member's role [Update]
→ Remove a team member [Delete]
```

### Pattern 7 — Performance
Deliver basic functionality first. Optimised performance is a separate story.

```
Original: Generate the annual report with real-time data.

Split:
→ Generate the annual report from cached data [basic]
→ Generate the annual report with real-time refresh [optimised]
```

### Pattern 8 — Deferred system integration
Replace the integration with a manual or stub step. Automate it as a follow-up story.

```
Original: Sync approved claims to the accounting system automatically.

Split:
→ Export approved claims as a CSV for manual upload to accounting [stub]
→ Sync approved claims to the accounting system via API [automation]
```

### Pattern 9 — Cross-cutting concerns
Deliver core functionality first. Add notifications, audit logs, and validation as follow-up stories.

```
Original: Create a project with full validation, notifications, and audit log.

Split:
→ Create a project with basic required fields
→ Validate project name uniqueness across the organisation
→ Notify project stakeholders when a project is created
→ Log project creation events to the audit trail
```

---

## Step 4 — Present the split

For each resulting story, produce a complete draft:

```
Split Story [N] of [total]

Title: [action-oriented title]
As a [specific persona],
I want [concrete action],
so that [measurable benefit].

Acceptance Criteria:
- [ ] [Observable condition]
- [ ] [Observable condition]

Size signal: [Small — fits one Sprint / Medium — verify with team]
Dependency: [None / Depends on Story N being done first]
```

Then present the full set and ask: *"Does this split make sense? Any stories to merge, rename, or reorder before I create them?"*

---

## Step 5 — Confirm and create

After confirmation:

- **ADO:** create each story as a Product Backlog Item using `wit_create_work_item`, linked to the original parent Feature or Epic
- **Jira:** create each story as a Story linked to the original Epic
- Optionally mark the original oversized story as Resolved/Closed with a comment: *"Split into [#IDs]."*

Never create without explicit confirmation.

---

## Guardrails

- Every split story must be independently valuable — if story N is useless without story N+1, it is not a valid split.
- Never split by technical layer (frontend story, backend story, database story) — that is task decomposition, not story splitting. A story must deliver user value end-to-end.
- If a story cannot be split without losing value, say so: *"This story is atomic — the value only exists when all parts are present. Consider whether the whole story fits in the Sprint with the current team capacity."*
- Keep the original story's parent link on all split stories — they should all trace back to the same Feature or Epic.
