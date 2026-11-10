# QE stage

Part of the [AI development pipeline](README.md). Agents turn each user story into a verified integration test plan, and, once the implementation of a story is architecture-approved by the [review stage](review.md), into verified automated tests. Every artifact an agent produces is checked by a separate verifier agent that starts with a clean context and loops until it scores 10/10. Every denial is logged, and recurring denials become lessons that the agents read on their next run.

The stage CLI is `scripts/qe/qe.mjs`; settings are in `scripts/qe/config.json`.

## Workflow 1: story to test plan

```mermaid
flowchart LR
  S[Story added or changed<br/>product-research/stories] -->|post-commit / post-merge<br/>background| P[qe-plan agent<br/>writes or updates the plan]
  P --> C{Structural<br/>precheck}
  C -->|fails| P
  C -->|passes| V[qe-plan-verifier<br/>fresh process, read-only]
  V -->|score below 10, gaps logged| P
  V -->|10/10| OK[Plan stamped verified<br/>story-sha + plan-sha]
  P -.->|5 rounds without 10/10| E[Escalation for a human]
```

- Output: `tests/plans/<story-file-name>/_index.md` plus one `TC-<KEY>-NN-<slug>.md` per case, covering every happy and sad path (see [tests/plans/README.md](../../tests/plans/README.md)).
- The runner owns the index front matter. `story-sha` records the story text the plan was written for. When the story changes, the plan becomes stale and the agent gets the story diff and updates the affected cases in place, keeping the ids of the others stable.
- The verifier sees only the story and the plan. It never sees the planner's reasoning.

## Workflow 2: implementation to automated tests

```mermaid
flowchart LR
  R[review stage approves<br/>the implementation] -->|same Stop hook| A[qe-automation agent<br/>background: scenario + tests]
  A --> T{Deterministic checks<br/>ids, traits, scenario, tests pass}
  T -->|fails| A
  T -->|passes| V[qe-automation-verifier<br/>fresh process, read-only]
  V -->|test gaps| A
  V -->|case gaps, root_stage plan| P[Workflow 1 plan loop]
  P --> A
  V -->|10/10| OK[Tests stamped verified]
```

1. The implementation agent runs `node scripts/pipeline/pipeline.mjs start <KEY>` and, when the line of work is complete, `node scripts/pipeline/pipeline.mjs done`.
2. When its turn ends, the dispatcher runs the review stage first. The QE stage runs only once the review has approved the current implementation (`review.approvedHash` matches), then starts the automation loop in the background and lets the agent finish. If the implementation changes again, the next approval re-runs automation in update mode, so the tests follow the corrected code.
3. The automation loop writes `testing/scenarios/<key>/scenario.yaml` and tests whose names carry their case ids, runs them, and loops with the automation verifier until it scores 10/10. If the tests show that the implementation contradicts the plan, the run stops with a `product-bug` escalation instead of weakening the tests.
4. Every recorded Playwright test also captures labeled keyframes (screenshots at each step, demo checkpoint and the final or failure state; see the `feature-demo-video` skill), listed in `artifacts/e2e-videos/manifest.json`. They are generated in the automation loop's deterministic checks (`precheckAutomation` in `scripts/qe/lib/workflows.mjs`), which run the story's Playwright tests with `E2E_KEEP_VIDEOS=1` because passing tests otherwise keep no video or keyframes. When that run fails, the feedback to the automation agent includes each failed test's error and keyframe paths. When the tests pass, the verifier gets every test's keyframes and checks that the screen matches each case (`visual-mismatch`). Only a manifest written by that run is used.

## Where it hooks in

The [dispatcher](README.md#lifecycle-events) calls the QE stage on these events:

| Event | What the QE stage does |
| --- | --- |
| `story-start` | reports the story's test plan state |
| `session-start` | test plan and automation state of the active story, open product bugs, stale plans |
| `stop` | after review approval: starts automation in the background |
| `post-commit`, `post-merge` | plan loop in the background for stories added or changed |
| `pre-push` | `qe check`: plans and tests for touched stories are verified and current |
| pull request | `qe-check.yml`, called from `ai-pipeline.yml` |
| weekly | `qe-lessons.yml`: `calibrate`, `metrics`, `lessons` (opens a PR) |

Opt out of automatic planning on commit with `git config qe.autoPlan false`; opt in to lessons runs after merges with `git config qe.autoLessons true`. Bypass the pre-push gate once with `QE_SKIP=1 git push`; CI still enforces it. `QE_REQUIRED_SCORE` overrides the 10/10 bar (for experiments only).

## Verifier loops

- Each round starts a new verifier process whose prompt holds only what it needs: the story and plan for plans; the case files, the tests and the scenario for automation. It cannot see the producer's reasoning or earlier verdicts.
- Cheap deterministic checks run before any verifier call (front matter, sections, happy and sad cases, index lists every id; every case has a test, no orphan ids, `[Trait("Story", ...)]`, scenario validates, tests pass).
- A verdict passes only at the required score (10) with no gaps; a lower score without gaps counts as an unexplained gap. Gaps go back to the producer as its next task.
- Automation gaps the verifier traces to the case itself (`root_stage: plan`) are sent to the plan loop and attributed to the plan agent in the metrics.
- After 5 rounds without 10/10, the loop stops and writes `.agent-state/escalations/<KEY>-<stage>.md`, which the next agent session is told about.

## Observability

All results are appended to `.agent-metrics/*.jsonl` (architecture reviews are in `arch-reviews.jsonl`, see [review.md](review.md)), which are committed with the plans and tests they describe (`merge=union` keeps every record across branches):

| File | One record per |
| --- | --- |
| `verifier-runs.jsonl` | verifier round: stage, story, round, score, passed, producer and verifier prompt versions |
| `verifier-denials.jsonl` | gap: category, subtype, case, detail, location, `root_stage`, `attributed_agent` |
| `precheck-failures.jsonl` | failed deterministic check |
| `calibration.jsonl` | audit of a past 10/10 verdict (`agreed`, or each gap the verifier missed) |

`node scripts/qe/qe.mjs metrics` (or `--json`; `pipeline metrics` combines every stage) reports denial rates, first-round pass rates, rounds to 10/10, denials by category and agent, the trend for each prompt version (a version is the hash of the agent file plus its lessons), cross-stage denials and calibration misses.

## Auto-improvement

`node scripts/qe/qe.mjs lessons` reads the metrics and, for each of `qe-plan`, `qe-automation`, `qe-plan-verifier`, `qe-automation-verifier` and `implementer` (whose denials are the blocking findings in `arch-reviews.jsonl`, see [review](review.md#how-the-implementer-improves)):

- **adds** a rule when a denial category recurs in 3 or more stories and no rule covers it;
- **strengthens** a rule when its category keeps recurring in 3 or more stories after the rule was added;
- **retires** a rule when its category has not recurred for 10 stories;
- **reverts** a rule when the denial rate for its category did not drop after it was added.

The `qe-lessons` agent writes the rules into `.github/agents/lessons/<agent>.md` (at most 15 each), and the command opens a pull request, so every change to agent behavior is reviewed like code. Each rule carries a marker with its category and date, which is what makes the retire and revert measurements possible. The lessons file is appended to the agent's prompt on every run, and it is part of the prompt version, so the metrics show whether a change helped.

Verifiers improve through calibration: `qe calibrate` re-audits recent 10/10 verdicts with a stricter auditor (optionally a different model, `agent.models.audit`), and gaps the verifier missed become lessons for that verifier.

Tune thresholds, round limits, models and categories in `scripts/qe/config.json`.

## Commands

```bash
node scripts/qe/qe.mjs status [PROMO-3]
node scripts/qe/qe.mjs plan PROMO-3 | --stale | --all [--force] [--background]
node scripts/qe/qe.mjs automate PROMO-3 [--skip-tests] [--background]
node scripts/qe/qe.mjs check [--all] [--changed-from origin/main]
node scripts/qe/qe.mjs metrics [--json] [--since 2026-01-01]
node scripts/qe/qe.mjs calibrate [--stage plan|automation] [--sample 3]
node scripts/qe/qe.mjs lessons [--dry-run] [--local]
npm run test:pipeline                          # all pipeline tests (fake agent, no LLM)
```

Background runs log to `.agent-state/logs/`.
