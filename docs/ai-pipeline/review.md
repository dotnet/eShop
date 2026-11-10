# Review stage

Part of the [AI development pipeline](README.md). Once the implementation agent runs `node scripts/pipeline/pipeline.mjs done`, the review stage runs an architecture review of its work each time the agent's turn ends, and loops with the agent until the code is approved.

```mermaid
flowchart LR
  I[Implementation agent] -->|turn ends, after pipeline done<br/>code agents only| G[arch-review agent<br/>Architecture Policy Pack]
  G -->|BLOCK<br/>Stop hook blocks with rule findings| I
  G -->|PASS / WARN| R[architecture-reviewer<br/>fresh process, read-only]
  R -->|CHANGES_REQUESTED<br/>Stop hook blocks with findings| I
  R -->|APPROVED| A[review.approvedHash set<br/>later stages run]
  R -.->|5 rounds without approval| E[Escalation for a human]
```

- Each round has two steps:
  - The `arch-review` agent (`architecture-policy/agents/arch_review.py review`) evaluates the Architecture Policy Pack rules on the changed files, as requester `implementer`. A `BLOCK` ends the round with changes requested and its rule findings (fix, approved patterns, `explain_finding` command) without an LLM call. `WARN` findings ride along as non-blocking notes. Each later round is linked to the previous policy review with `previous_review_id`, so it reports resolved and persisting findings.
  - Otherwise the `architecture-reviewer` agent reviews the diff.

  Set `review.policy.enabled` to `false` to skip the policy step. If the pack or Python is unavailable, the step is skipped with a note and the LLM review still runs.
- Only code agents are reviewed: the main session's `Stop`, or a `SubagentStop` whose `agent_type` is listed in `review.codeAgents` (default `implementer`).
- The reviewer sees only the story and the implementation diff against the merge base (tests, plans and scenarios excluded, see `implExclude` in `scripts/pipeline/config.json`), plus the repository conventions (`AGENTS.md`, `CONTRIBUTING.md`). It never sees the implementation agent's reasoning.
- Changes requested block the stop and send the findings to the agent as its next instruction. Each correction triggers a new round with a fresh reviewer, up to `review.maxRounds` (5). An approval with blocking findings counts as changes requested.
- If the agent stops again without changing the code, the hook blocks once more, then lets the turn end with a message so it cannot loop forever.
- On approval it records the approved implementation hash in `review.approvedHash` in the story state. The `qe` stage starts automation only for that exact hash, so code changed after an approval is reviewed again first.
- `pipeline start <KEY>` resets the review rounds.

Every round is appended to `.agent-metrics/arch-reviews.jsonl` (story, round, verdict, and each finding's severity, category, file, line and issue). Rounds blocked by the policy pack carry `reviewer: arch-review-agent`, the `review_id` and each finding's `rule`.

## How the implementer improves

Review denials feed back into the code agents at two speeds:

- **Across stories (shared, reviewed):** `node scripts/qe/qe.mjs lessons` (weekly in `qe-lessons.yml`) treats every blocking finding in `arch-reviews.jsonl` as an `implementer` denial, grouped by category and, for policy-pack findings, by rule id. A mistake that recurs in 3 or more stories becomes a rule in `.github/agents/lessons/implementer.md`, proposed in a pull request; rules are strengthened, retired or reverted with the same measurements as the QE agents ([qe](qe.md#auto-improvement)). The `implementer` agent reads that file before it starts, and the session-start hook adds it to the main session's context while a story is active.
- **Within a story (local):** when the policy pack flags a finding as a repeat mistake (the requester was denied for that rule before), the block message also includes `get_lessons` guidance from the agent's local failure history (`review.policy.lessonsCommand`; set it to `null` to turn it off).

## Commands

```bash
node scripts/review/review.mjs run [PROMO-3]       # one review round now, printed
node scripts/review/review.mjs status [PROMO-3]
node scripts/review/review.mjs metrics [--json] [--since 2026-01-01]
```

Settings: `scripts/review/config.json` (`review.maxRounds`, `review.timeoutSec`, `review.codeAgents`, `review.policy` including `lessonsCommand` and `maxLessons`, `agent.models.reviewer`). Lesson thresholds are shared with QE in `scripts/qe/config.json` (`lessons`).
