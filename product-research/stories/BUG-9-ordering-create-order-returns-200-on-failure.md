# BUG-9: [BE - Ordering.API] - Create order returns 200 when the order was not created

**Type:** Bug
**Severity:** Medium
**Category:** Error handling
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Create order returns 200 when the order was not created. Caught by the test "an invalid order is rejected with 400" in `e2e/teststack/ordering-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load ordering-two-buyers
```

1. `POST http://localhost:18084/api/orders` as alice with `x-requestid: <new guid>` and a `CreateOrderRequest` with an empty `items` list.

**Expected:** `400 BadRequest` with the validation errors; no order is created.

**Actual:** `200 OK`, but no order is created (the command fails validation and only a warning is logged).

**Acceptance Criteria:**

- **Given** the test stack is running with the `ordering-two-buyers` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/ordering-api.spec.ts` › "an invalid order is rejected with 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/ordering-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Ordering.API/Apis/OrdersApi.cs:155-166` logs `CreateOrderCommand failed` and still returns `TypedResults.Ok()`.
- **Suggested fix:** Return `400` (with the validator's messages) when the command fails, or surface the `ValidationException` from the pipeline as `400`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-10](BUG-10-ordering-domain-errors-return-500.md)
