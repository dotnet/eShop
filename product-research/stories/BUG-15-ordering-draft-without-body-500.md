# BUG-15: [BE - Ordering.API] - Creating an order draft without a body returns 500

**Type:** Bug
**Severity:** Low
**Category:** Error handling
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Creating an order draft without a body returns 500. Caught by the test "a draft request without a body is rejected with 400" in `e2e/teststack/ordering-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load ordering-two-buyers
```

1. `POST http://localhost:18084/api/orders/draft` as alice with no body.

**Expected:** `400 BadRequest`.

**Actual:** `500 Internal Server Error`.

**Acceptance Criteria:**

- **Given** the test stack is running with the `ordering-two-buyers` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/ordering-api.spec.ts` › "a draft request without a body is rejected with 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/ordering-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Ordering.API/Apis/OrdersApi.cs:106` `CreateOrderDraftAsync(CreateOrderDraftCommand command, ...)` binds a required body with no null handling.
- **Suggested fix:** Validate the body (null or empty `Items`) and return `400`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** None
