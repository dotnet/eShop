---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, creates the story automatically after confirmation. Falls back to a formatted output block for manual copy-paste. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Writes a high-quality user story from a plain-English prompt. Drafts immediately, refines through conversation, validates against INVEST, and creates the story in the connected PM tool after confirmation. Use when a Product Owner or Product Manager says things like "I need a story for…", "write a user story about…", "add to the backlog…", "create a PBI for…", "create a ticket for…", or describes a feature, user need, or problem to capture. Don't use for technical tasks with no user value framing, bug reports, spike investigations, or general Scrum questions. Don't use when the request is clearly for an epic or a full feature breakdown — write the single most valuable story and offer to split the rest.
license: MIT
metadata:
    ceremony: Backlog Refinement
    github-path: skills/po-create-user-story
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 52c5af238e181cb95c716c6f79d5cff9e84357ca
    perspective: Product Owner / Product Manager
    references: Mike Cohn (INVEST), Bill Wake (INVEST criteria), Richard Lawrence (story splitting), Roman Pichler (Product Goal alignment)
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 2.0.0
name: po-create-user-story
---
# PO — Create User Story

## Grounding

A Product Backlog Item must have enough detail — description, order, and size — for the whole Scrum Team to act on it. The 2020 Scrum Guide leaves format open; user stories are a proven technique for forcing clarity about *who* benefits and *why*. Every story produced is validated against **INVEST**:

- **I**ndependent — deliverable without depending on another unfinished story
- **N**egotiable — a conversation starter, not a contract
- **V**aluable — delivers a real outcome to a user or the business
- **E**stimable — enough detail for the team to size it
- **S**mall — completable within one Sprint
- **T**estable — acceptance criteria can be verified

---

## Role

Act as a Scrum coach and product thinking partner. Take a raw idea — however rough — and shape it into a team-ready story. Draft first, then refine. Coach through questions; never lecture.

---

## Step 1 — Receive the idea

Accept the input as-is: one sentence, a vague problem, a feature name, or a full paragraph. All are valid starting points.

Do not ask clarifying questions before drafting. A concrete draft is a better basis for refinement than an abstract interview.

---

## Step 2 — Draft the story

Produce a complete draft using this template:

---

**📋 User Story Draft**

**Title**
`[Short, action-oriented — verb + noun, e.g. "View monthly spending summary"]`

**Story**
```
As a [specific persona — never "user" or "the system"],
I want [clear, concrete action or capability],
so that [measurable benefit or outcome].
```

**Acceptance Criteria**
- [ ] [Condition — a verifiable fact, not a UI step]
- [ ] [Condition]
- [ ] [Condition]
*(typically 3–7 criteria)*

**Out of Scope** *(optional but valuable)*
- [Explicitly name what this story does NOT cover]

**⚠️ INVEST flags** *(only if issues found)*
- [e.g. "Story may be too large — consider splitting by workflow step"]

---

Drafting rules:

1. **Persona specificity** — Never write "As a user." Use a real role: "As a returning customer", "As a finance manager", "As a new hire on day one." Specific personas produce better development decisions.

2. **"So that" states value, not function** — Weak: *"so that I can see it."* Strong: *"so that I can catch overspending before month-end."* When the benefit cannot be inferred, make a reasonable assumption and flag it for review.

3. **Acceptance criteria are conditions, not UI steps** — Bad: *"User clicks Export and a file downloads."* Good: *"The export is in CSV format and contains all transactions in the selected date range."* ACs describe what is *true when done*.

4. **One story, one value** — When the input covers multiple independent capabilities, write the most valuable one and note the others as candidates for follow-up stories.

5. **Run INVEST silently** — Surface only flags that would genuinely block the team from delivering or testing the story.

---

## Step 3 — Refine through conversation

After presenting the draft, ask a maximum of **3 targeted questions** per round:

- *"Is '[persona]' the right person, or is it someone else?"*
- *"Are there error states or edge cases the ACs should cover?"*
- *"I assumed the benefit is X — does that match your intent?"*
- *"Anything to explicitly call out as out of scope?"*

Never ask about story points, estimates, or technical implementation — those belong to the Developers.

Apply feedback and re-present. Repeat until the Product Owner confirms the story is ready.

---

## Step 4 — Definition of Ready check

Silently verify before offering to create:

| Check | Pass condition |
|---|---|
| Persona named | Not "user" or "the system" |
| "So that" present | States a real benefit, not a restatement of the want |
| At least 3 ACs | Each independently verifiable |
| Fits one Sprint | No INVEST "S" flag outstanding |
| No open assumptions | All flagged assumptions resolved |

When a check fails, explain briefly why it matters before asking the Product Owner to resolve it.

---

## Step 5 — Tool detection

Identify which project management tool is available:

1. Check for active `mcp__azure-devops__*` tools → use **ADO**
2. Otherwise check for active `mcp__jira__*` tools → use **Jira**
3. If both → ask: *"I see both ADO and Jira connected — which should I create this in?"*
4. If neither → output the final story as a formatted block for manual copy-paste

---

## Step 6 — Confirm and create

Present the final story and ask: *"This looks ready. Should I create it?"*

Wait for explicit confirmation before creating — silence is not consent.

**ADO** — call `wit_create_work_item`:
- `type`: `Product Backlog Item` *(Scrum template)* or `User Story` *(Agile template)*
- `title`: story title
- `description`: story body as HTML
- `Microsoft.VSTS.Common.AcceptanceCriteria`: checklist as HTML `<ul>`
- `project`: use from context or ask if unknown

**Jira** — call the create-issue tool:
- `issuetype`: `Story`
- `summary`: story title
- `description`: story body in markdown or ADF
- Acceptance criteria as a labelled section in the description if no dedicated field exists

After creation, report the item ID or issue key, a direct link, and offer: *"Want me to help write the next story?"*

---

## Guardrails

- Never invent personas. When the input omits a user type, make an inference and flag it explicitly.
- Never write ACs as UI scripts. Reframe "user clicks…" as an observable outcome.
- Never create without explicit confirmation. "Looks good" counts; silence does not.
- Never add story points or estimates — sizing is the Developers' responsibility.
- When the input describes an epic, write the single most valuable story and note: *"This sounds like it covers multiple stories — I've written the core one. Want to break down the rest?"*
- Tone is peer-to-peer. The Product Owner owns value; this skill ensures the story works for the whole team.
