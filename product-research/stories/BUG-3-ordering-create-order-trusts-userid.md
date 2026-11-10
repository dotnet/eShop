# BUG-3: [BE - Ordering.API] - Create order uses the userId from the request body instead of the token

**Type:** Bug
**Severity:** Critical
**Category:** Security
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Create order uses the userId from the request body instead of the token. Caught by the test "an order is created for the signed-in buyer, not the userId in the body" in `e2e/teststack/ordering-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load ordering-two-buyers
```

1. Get an access token for bob with the `orders` scope.
2. `POST http://localhost:18084/api/orders` with `x-requestid: <new guid>` and a valid `CreateOrderRequest` whose `userId` is alice's id (`a11ce000-0000-4000-8000-000000000001`).

**Expected:** The order is created for bob (the token's `sub`), or the request is rejected with `400`/`403` when the ids differ.

**Actual:** `200 OK`, and the order is created on alice's account.

**Acceptance Criteria:**

- **Given** the test stack is running with the `ordering-two-buyers` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/ordering-api.spec.ts` › "an order is created for the signed-in buyer, not the userId in the body"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/ordering-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Ordering.API/Apis/OrdersApi.cs:141` passes `request.UserId` and `request.UserName` into `CreateOrderCommand`.
- **Suggested fix:** Take the buyer id and name from `IIdentityService` and ignore (or reject a mismatched) `userId` in the body.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-1](BUG-1-ordering-order-details-idor.md), [BUG-9](BUG-9-ordering-create-order-returns-200-on-failure.md)
