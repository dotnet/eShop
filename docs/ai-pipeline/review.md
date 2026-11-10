# Review stage

Part of the [AI development pipeline](README.md). Once the implementation agent runs `node scripts/pipeline/pipeline.mjs done`, the review stage runs an architecture review of its work each time the agent's turn ends, and loops with the agent until the code is approved.

```mermaid
flowchart LR
  I[Implementation agent] -->|turn ends, after pipeline done| R[architecture-reviewer<br/>fresh process, read-only]
  R -->|CHANGES_REQUESTED<br/>Stop hook blocks with findings| I
  R -->|APPROVED| A[review.approvedHash set<br/>later stages run]
  R -.->|5 rounds without approval| E[Escalation for a human]
```

- The reviewer sees only the story and the implementation diff against the merge base (tests, plans and scenarios excluded, see `implExclude` in `scripts/pipeline/config.json`), plus the repository conventions (`AGENTS.md`, `CONTRIBUTING.md`). It never sees the implementation agent's reasoning.
- Changes requested block the stop and send the findings to the agent as its next instruction. Each correction triggers a new round with a fresh reviewer, up to `review.maxRounds` (5). An approval with blocking findings counts as changes requested.
- If the agent stops again without changing the code, the hook blocks once more, then lets the turn end with a message so it cannot loop forever.
- On approval it records the approved implementation hash in `review.approvedHash` in the story state. The `qe` stage starts automation only for that exact hash, so code changed after an approval is reviewed again first.
- `pipeline start <KEY>` resets the review rounds.

Every round is appended to `.agent-metrics/arch-reviews.jsonl` (story, round, verdict, and each finding's severity, category and file).

## Commands

```bash
node scripts/review/review.mjs run [PROMO-3]       # one review round now, printed
node scripts/review/review.mjs status [PROMO-3]
node scripts/review/review.mjs metrics [--json] [--since 2026-01-01]
```

Settings: `scripts/review/config.json` (`review.maxRounds`, `review.timeoutSec`, `agent.models.reviewer`).
