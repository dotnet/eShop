---
name: feature-demo-video
description: "**REQUIRED WORKFLOW SKILL** - Every e2e test is recorded on video; after implementing a user-facing change, cover the flow with a passing (optionally narrated) e2e test and hand over its video so humans can verify and demo it. USE FOR: finishing any feature, bug fix, or UI change visible in the WebApp; \"record a demo\", \"prove it works\", \"demo video\", \"walkthrough video\", \"test videos\", \"e2e video\". DO NOT USE FOR: backend-only changes with no browser-visible effect, docs-only changes, refactors with no behavior change. INVOKES: testing/teststack, playwright-cli (locator discovery), Playwright test runner."
---

# Feature videos from e2e tests

All Playwright e2e tests record video (`use.video` in `playwright.config.ts`, inherited by `playwright.teststack.config.ts`). Videos show the clicked actions (top-right) and the current spec, test and `test.step` (top-left). After each run, `e2e/video-reporter.ts` copies them to:

```text
artifacts/e2e-videos/<project>/<spec>--<test>.webm          # passed
artifacts/e2e-videos/<project>/<spec>--<test>.failed.webm   # failed: use it to debug, never as proof
```

The folder is cleared at the start of each run and is git-ignored. Videos are also attached to the HTML report, and CI uploads them as the `e2e-videos` artifact.

## When this is required

Whenever your change affects anything a shopper or admin can see or do in a browser: new pages, buttons or forms, or changed behavior in catalog, cart, checkout, orders, login or the webhooks UI. Skip it (and say why in your summary) for backend-only, docs-only or pure refactor changes.

## Workflow

1. **Start the app in a known state.** Prefer the test stack, so the data in the video is deterministic:

   ```bash
   testing/teststack up --scenario <scenario>   # see testing/README.md; add a scenario if the flow needs new data
   ```

   On Podman, set `TESTSTACK_DOCKER=podman` (and `ASPIRE_CONTAINER_RUNTIME=podman`). On Windows, run the script from Git Bash. If sign-in hangs on Podman machine (Windows/macOS), the containers can't reach `host.docker.internal`. Fix it once per machine: `podman machine ssh "printf '[containers]\nhost_containers_internal_ip = \"192.168.127.254\"\n' > /etc/containers/containers.conf.d/90-host-gateway.conf; systemctl restart podman.socket"`, then `testing/teststack compose up -d --force-recreate`.

2. **Walk the flow once with `playwright-cli`** (see the `playwright-cli` skill) to discover stable locators. Prefer roles, labels and text over CSS.

3. **Cover the flow with an e2e test.** For a new feature, add `e2e/teststack/<feature>.spec.ts` (template: `e2e/teststack/add-to-cart.spec.ts`), which loads its scenario in `beforeAll`. Extending an existing spec is fine too. To make the video read like a demo, import `test` and `expect` from `e2e/demo.ts` and use the `demo` fixture:
   - One `demo.chapter(title, description)` per acceptance criterion or step of the flow.
   - `demo.caption(text)` to narrate what is happening (it stays until replaced; `''` clears it).
   - After each step, `expect(...)` the result, then `demo.highlight(locator, label)` the element that proves it worked.
   - Without the fixture, wrap steps in `test.step('...')`: step titles are shown in the video.
   - Cover the happy path first, then important edge or error cases as separate tests; each test gets its own video. Keep each one under about 60 seconds.

4. **Run it:**

   ```bash
   npx playwright test -c playwright.teststack.config.ts <feature>   # or the whole suite: npm run test:e2e:teststack
   ```

   It must pass. If it fails, watch the `.failed.webm` and the trace, fix the feature or the test, and run again.

5. **Report it.** In your final summary to the user:
   - Embed each relevant video with Markdown using its absolute path, for example `![Add to cart](C:\path\to\repo\artifacts\e2e-videos\scenarios\add-to-cart--alice-adds-a-product-to-her-shopping-bag.webm)`.
   - Say what each video demonstrates and the scenario or data used.
   - In a PR description, include the same list and the command to reproduce the videos. Commit the tests, never the videos.

## Demo API (`e2e/demo.ts`)

| Call | Effect |
| --- | --- |
| `demo.chapter(title, description?, ms = 2500)` | Full-screen title card over a blurred page. Blocks for `ms`. |
| `demo.caption(text)` | Sticky caption at the bottom; replaces the previous one. `''` clears. |
| `demo.highlight(locator, label?, ms = 2000)` | Green outline and label around an element. |
| `demo.type(locator, text)` | Click and type with a visible per-key delay. |
| `demo.pause(ms = 1000)` | Give the viewer time on a busy screen. |

## Rules

- Every spec imports `test` and `expect` from `e2e/fixtures.ts` (or from `e2e/demo.ts`, which extends it), never from `@playwright/test`. The base `page`/`context` fixtures are always recorded. The shared fixture also records contexts a test opens with `browser.newContext()`, such as one per user in multi-user flows, with one video per page. Contexts opened in `beforeAll`/`afterAll` are setup and are not recorded, so put the flow you want on video inside the test.
- API-only tests (only the `request` fixture) have no browser and produce no video. That is expected. For a user-facing feature, cover the flow in the UI as well.
- Use seeded users from the scenario (for example `alice` / `Pass123$`) and the `login` helper from `e2e/teststack/scenario.ts`.
- Wait on `expect(...)`, not on `waitForTimeout`. Use `demo.pause` only for pacing.
- Don't commit anything under `artifacts/`.
