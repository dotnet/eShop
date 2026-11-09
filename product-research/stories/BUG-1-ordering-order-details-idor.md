# BUG-1: [BE - Ordering.API] - Any signed-in user can read any order by id

**Type:** Bug
**Severity:** Critical
**Category:** Security
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Any signed-in user can read any order by id. Caught by the test "a buyer cannot read another buyer's order details" in `e2e/teststack/ordering-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load ordering-two-buyers
```

1. Get an access token for alice with the `orders` scope (see `accessToken()` in `e2e/teststack/ordering-helpers.ts`).
2. `GET http://localhost:18084/api/orders/7` with alice's token and `api-version=1.0`. Order 7 belongs to bob.

**Expected:** `404 NotFound` (or `403`); the order of another buyer is never returned.

**Actual:** `200 OK` with bob's full order: address, items and total.

**Acceptance Criteria:**

- **Given** the test stack is running with the `ordering-two-buyers` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/ordering-api.spec.ts` › "a buyer cannot read another buyer's order details"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/ordering-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Ordering.API/Apis/OrdersApi.cs:80-90` `GetOrderAsync` loads the order by id only.
- `src/Ordering.API/Application/Queries/OrderQueries.cs` `GetOrderAsync` has no buyer filter.
- `IdentityService.GetUserIdentity()` already returns the caller's id; `GetOrdersByUserAsync` (line 93) uses it.
- **Suggested fix:** Filter the query by the caller's identity (join `buyers.IdentityGuid`) and return `404` when the order belongs to someone else.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-2](BUG-2-ordering-cancel-ship-no-ownership.md), [BUG-3](BUG-3-ordering-create-order-trusts-userid.md)
