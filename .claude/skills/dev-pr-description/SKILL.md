---
compatibility: Reads git diff and log via shell. When ADO or Jira MCP is connected, fetches the linked story automatically. Falls back to a pasted story or branch name. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Generates a pull request description by reading the git diff and the linked user story, mapping each changed file to an acceptance criterion so reviewers know why each change exists. Use when a developer says things like "write my PR description", "generate a PR", "help me describe this pull request", "create the PR body", "what should I put in my PR", or is about to open a pull request. Do not use for code review, story writing, or general git help unrelated to a pull request.
license: MIT
metadata:
    ceremony: Sprint
    github-path: skills/dev-pr-description
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: d375dc3f486381456a26b4f1a3b5df604ec60b73
    perspective: Developer
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: dev-pr-description
---
# PR Description Generator

## Purpose

A good PR description answers three questions every reviewer has before they read a single line of code: *What changed? Why does it exist? How do I verify it?* This skill reads the diff and the story together to answer all three — so the reviewer spends their time reviewing, not detective-work.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → `$PM_TOOL = ado`
2. Check for active `mcp__jira__*` tools → `$PM_TOOL = jira`
3. If neither → `$PM_TOOL = manual`

---

## Step 1 — Read the git context

Run the following to understand what changed:

```bash
# Changed files and a summary of what moved
git diff --stat HEAD~1..HEAD 2>/dev/null || git diff --stat origin/main..HEAD

# Commit messages on this branch
git log --oneline origin/main..HEAD 2>/dev/null || git log --oneline -10

# Full diff (for understanding the nature of changes)
git diff origin/main..HEAD 2>/dev/null || git diff HEAD~1..HEAD
```

If git is not available or the branch has no commits yet, ask the developer to describe the changes.

---

## Step 2 — Identify the linked story

Look for the work item ID in this order:

1. **Branch name** — extract a pattern like `feature/#1234-`, `feat/PROJ-456-`, `dev/1234/`, etc.
2. **Commit messages** — look for `#1234`, `PROJ-456`, `Refs:`, `Closes:`, or `Fixes:` references
3. **Ask** — *"Which story does this PR implement? Provide the ID or paste the content."*

Then fetch the story:
- **ADO:** use `wit_get_work_item` — read title, description, and acceptance criteria
- **Jira:** use the get-issue tool — read summary, description, and acceptance criteria
- **Manual:** accept pasted content

Store as `$STORY`.

---

## Step 3 — Map changes to acceptance criteria

Before writing the description, build an internal map:

For each acceptance criterion in `$STORY`:
- Which files in the diff satisfy it?
- Is it fully satisfied, partially satisfied, or not covered?

Flag any AC that is not covered by the diff — this is either a missing implementation or the AC belongs to a different PR. Surface it: *"AC [N] — '[text]' — does not appear to be covered by these changes. Is that intentional?"*

---

## Step 4 — Generate the PR description

Use this structure:

```markdown
## What this does
[One sentence in user-facing language — what the user can now do that they could not before.
Not a technical summary. Not "refactored the service layer."]

## Work item
Closes #[ID]
[or: Refs: PROJ-456]

## Acceptance criteria covered
- [x] [AC text — copy verbatim from the story]
- [x] [AC text]
- [ ] [AC text — if intentionally deferred, explain why in a Note below]

## Changed files
| File | What changed and why |
|---|---|
| `path/to/file.ext` | [One line — what this file does differently and which AC it serves] |
| `path/to/test-file.ext` | [Tests for AC N and AC M] |

## How to test
1. [Step a reviewer can execute — environment, test data, or command]
2. [Expected result]
3. [Edge case or error state to verify]

## Notes
[Breaking changes, migrations needed, dependent PRs, feature flags, known gaps, or anything
the reviewer needs before they start. Leave blank if none.]
```

---

## Step 5 — Review and finalise

Present the draft and ask: *"Does this capture the PR accurately? Anything to add or adjust before I finalise it?"*

Apply corrections. Once confirmed, output the final description in a clean code block ready to paste into GitHub, ADO, or Jira.

---

## Guardrails

- Never describe changes in technical terms only — always include the user-facing outcome in the "What this does" section.
- Never mark an AC as covered if the diff does not contain changes that satisfy it.
- Never include secrets, tokens, connection strings, or personal data in the description — scan the diff for these before generating.
- If the diff is very large (more than 20 files), suggest splitting the PR: *"This PR touches [N] files across [M] concerns. Consider splitting into smaller PRs — one per AC or one per layer — so reviewers can focus."*
- Keep "How to test" steps executable by someone who did not write the code.
