# BUG-10: [BE - Ordering.API] - Invalid order status changes return 500 instead of a client error

**Type:** Bug
**Severity:** Medium
**Category:** Error handling
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Invalid order status changes return 500 instead of a client error. Caught by the test "invalid transitions return a client error, not 500" in `e2e/teststack/ordering-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load ordering-two-buyers
```

1. As alice, `PUT http://localhost:18084/api/orders/cancel` with `x-requestid: <new guid>` and body `{"orderNumber": 5}` (her Shipped order).

**Expected:** `400` or `409` with a message such as "Shipped orders can't be cancelled".

**Actual:** `500` "Cancel order failed to process."

**Acceptance Criteria:**

- **Given** the test stack is running with the `ordering-two-buyers` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/ordering-api.spec.ts` › "invalid transitions return a client error, not 500"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/ordering-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Ordering.API/Apis/OrdersApi.cs:45,74` map every failed cancel/ship to a `500` problem.
- `OrderingDomainException` from `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs` (`SetCancelledStatus`, `SetShippedStatus`) is a rule violation, not a server fault.
- **Suggested fix:** Map `OrderingDomainException` to `409 Conflict` (or `400`) and return `404` when the order doesn't exist.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-9](BUG-9-ordering-create-order-returns-200-on-failure.md)
