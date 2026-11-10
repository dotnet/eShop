# Lessons: qe-plan

Rules learned from plan-verifier denials of test plans. They are appended to the qe-plan agent's instructions on every run.

This file is managed by `node scripts/qe/qe.mjs lessons` (see `docs/qe-pipeline.md`). Each rule is a `## L<n>: <instruction>` heading followed by a tracking marker comment holding its category, subtype and date added. The pipeline uses the marker to measure whether the rule reduced denials and retires or reverts rules that did not help. Changes arrive as pull requests.

No lessons yet.
