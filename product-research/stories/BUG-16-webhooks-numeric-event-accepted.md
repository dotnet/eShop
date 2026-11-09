# BUG-16: [BE - Webhooks.API] - Webhooks.API accepts numeric event names outside the allowed list

**Type:** Bug
**Severity:** Low
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Webhooks.API accepts numeric event names outside the allowed list. Caught by the test "event names outside WebhookType are rejected, including numeric ones" in `e2e/teststack/webhooks-subscriptions.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load webhooks-subscriptions
```

1. As alice, `POST http://localhost:18085/api/webhooks` with a valid url/grant url and `"event": "99"`.

**Expected:** `400 BadRequest` "99 is invalid event name".

**Actual:** `201 Created`, and a subscription of type 99 is stored. It never fires.

**Acceptance Criteria:**

- **Given** the test stack is running with the `webhooks-subscriptions` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/webhooks-subscriptions.spec.ts` › "event names outside WebhookType are rejected, including numeric ones"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/webhooks-subscriptions.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Webhooks.API/Model/WebhookSubscriptionRequest.cs:22` uses `Enum.TryParse`, which accepts any number.
- **Suggested fix:** Also require `Enum.IsDefined` (or reject purely numeric input).

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** None
