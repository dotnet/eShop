# BUG-2: [BE - Ordering.API] - Any signed-in user can cancel or ship any order

**Type:** Bug
**Severity:** Critical
**Category:** Security
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Any signed-in user can cancel or ship any order. Caught by the test "a buyer cannot cancel or ship another buyer's order" in `e2e/teststack/ordering-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load ordering-two-buyers
```

1. Get an access token for alice with the `orders` scope.
2. `PUT http://localhost:18084/api/orders/ship` with header `x-requestid: <new guid>` and body `{"orderNumber": 7}` (bob's Paid order).

**Expected:** `404` or `403`, and the order status is unchanged. Shipping should also be limited to a back-office role, not buyers.

**Actual:** `200 OK`, and bob's order 7 becomes `Shipped`. Cancel behaves the same way.

**Acceptance Criteria:**

- **Given** the test stack is running with the `ordering-two-buyers` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/ordering-api.spec.ts` › "a buyer cannot cancel or ship another buyer's order"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/ordering-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Ordering.API/Apis/OrdersApi.cs:22-77` `CancelOrderAsync` and `ShipOrderAsync` send the command without checking who owns the order.
- `src/Ordering.API/Application/Commands/CancelOrderCommandHandler.cs` and `ShipOrderCommandHandler.cs` load the order by number only.
- **Suggested fix:** Check ownership for cancel; require an admin/operator role (policy) for ship. Return `404` for orders the caller can't see.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-1](BUG-1-ordering-order-details-idor.md)
