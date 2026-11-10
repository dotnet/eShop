# Agent instructions

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- graphify-out/ is built locally (gitignored except the semantic cache). If graphify-out/graph.json is missing, run `scripts/graphify-build.sh` first (no API cost, a few seconds; replays the shared semantic cache), then follow the rules below.
- When graphify-out/graph.json exists, run `graphify query "<question>"` before your first grep, search or source-file read for a codebase question, and put this rule in every subagent prompt that explores code. Read raw files only for lines the graph or an earlier read already located. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- A stale graph is not a reason to skip graphify; refresh it with `graphify update .`. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- graphify-out/cache/semantic/ is committed and shared, so everyone gets the LLM-extracted doc/image nodes without an API key. After adding or changing docs or images (.md, .pdf, diagrams), run `scripts/graphify-build.sh --extract` and commit only the new files under graphify-out/cache/semantic/. `--extract` needs the `claude` CLI; if it is not installed (`command -v claude` fails), run `scripts/graphify-build.sh` instead (no LLM, code graph only for the changed docs), do not install or sign in to `claude` yourself, and say in your summary that the semantic cache still needs `scripts/graphify-build.sh --extract` from someone with the CLI. Never commit anything else in graphify-out/.

## Feature videos (definition of done)

Every e2e test is recorded on video (`video` in `playwright.config.ts`). By default only failed tests keep theirs (`artifacts/e2e-videos/<project>/<spec>--<test>.failed.webm`, plus keyframes for agents); set `E2E_KEEP_VIDEOS=1` to keep passing tests' videos (`<spec>--<test>.webm`) too. When you implement or change anything a user can see or do in the browser, you are not done until a passing e2e test covers the flow and you have its video. Follow the `feature-demo-video` skill (`.github/skills/feature-demo-video/SKILL.md`):

1. Add or extend an e2e test for the flow. Prefer a teststack spec (`e2e/teststack/<feature>.spec.ts`, template `e2e/teststack/add-to-cart.spec.ts`) and narrate it with the `demo` fixture from `e2e/demo.ts`. Specs import `test` from `e2e/fixtures.ts` (or `e2e/demo.ts`), never `@playwright/test`, so contexts opened with `browser.newContext()` are recorded too.
2. Run it: `testing/teststack up`, then `E2E_KEEP_VIDEOS=1 npx playwright test -c playwright.teststack.config.ts <feature>` (PowerShell: `$env:E2E_KEEP_VIDEOS=1` first). It must pass.
3. Embed the resulting `artifacts/e2e-videos/**/*.webm` (absolute path) in your final summary, and describe what it shows in the PR description. Never commit the videos.

Skip this only for backend-only, docs-only or no-behavior-change work, and say so in your summary.

## AI development pipeline

Stories go through the agent pipeline described in `docs/ai-pipeline/README.md` (architecture review, then QE test plans and automated tests).

- When you start implementing a story, run `node scripts/pipeline/pipeline.mjs start <KEY>` (for example `PROMO-3`).
- When your line of work for that story is complete, run `node scripts/pipeline/pipeline.mjs done` before you finish. The Stop hook then runs an architecture review. If it blocks with findings, fix them and finish again; tests are written automatically after approval.
- Do not edit `tests/plans/`, `.agent-metrics/` or `.github/agents/lessons/` by hand; the pipeline owns them. Commit them together with the work they describe.
