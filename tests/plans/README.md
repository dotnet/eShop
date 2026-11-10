# Integration test plans

One directory per story in `product-research/stories`, named after the story file. Plans are written and updated by the `qe-plan` agent and approved by the `qe-plan-verifier` agent; see [docs/ai-pipeline/qe.md](../../docs/ai-pipeline/qe.md).

```text
tests/plans/PROMO-3-apply-promotions-to-basket/
  _index.md                         # scope, case table, open questions; front matter owned by the pipeline
  TC-PROMO-3-01-<slug>.md           # one test case per file
  TC-PROMO-3-02-<slug>.md
```

Each case file has front matter (`id`, `title`, `type: happy|sad`, `sad-path` for sad cases, `layer: functional|e2e`, `covers`) and `Preconditions`, `Steps` and `Expected result` sections. Automated tests carry the case id in their name, which is how coverage is traced.

Do not edit plans by hand without re-running `node scripts/qe/qe.mjs plan <KEY> --force`: the verification stamp covers the plan content, and `qe check` (pre-push and CI) fails on an edited, unverified plan.
