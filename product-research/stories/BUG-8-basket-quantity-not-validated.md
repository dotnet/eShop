# BUG-8: [BE - Basket.API] - Basket.API stores zero and negative quantities

**Type:** Bug
**Severity:** Medium
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Basket.API stores zero and negative quantities. Caught by the test "UpdateBasket rejects zero or negative quantities" in `e2e/teststack/basket-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load basket-edge
```

1. Call gRPC `BasketApi.UpdateBasket` as alice with items `{productId: 7, quantity: -3}` and `{productId: 6, quantity: 0}`.

**Expected:** `INVALID_ARGUMENT`; Redis is unchanged.

**Actual:** `OK`, and both lines are stored in Redis with quantities -3 and 0.

**Acceptance Criteria:**

- **Given** the test stack is running with the `basket-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/basket-api.spec.ts` › "UpdateBasket rejects zero or negative quantities"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/basket-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Basket.API/Model/BasketItem.cs:13` already says `Quantity < 1` is invalid (`IValidatableObject`), but nothing calls it.
- `src/Basket.API/Grpc/BasketService.cs:36-50` `UpdateBasket` maps and saves the request without validation.
- **Suggested fix:** Validate the mapped basket in `UpdateBasket` (reuse `BasketItem.Validate`) and throw `RpcException(StatusCode.InvalidArgument)`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-7](BUG-7-webapp-missing-product-breaks-site.md)
