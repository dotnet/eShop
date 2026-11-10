# BUG-7: [FE - WebApp] - A basket line for a product no longer in the catalog breaks every page

**Type:** Bug
**Severity:** High
**Category:** Reliability
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

A basket line for a product no longer in the catalog breaks every page. Caught by the test "a basket line for a product no longer in the catalog does not break the site" in `e2e/teststack/basket-cart.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load basket-edge
```

1. Load `basket-edge`. Oscar's Redis basket holds product `99999`, which is not in the catalog.
2. Sign in as oscar (`Pass123$`) and open any page, for example `http://localhost:18080/`.

**Expected:** The site works. The cart drops (or flags) the missing product, and the rest of the basket is shown.

**Actual:** Every page fails for that user. WebApp logs `KeyNotFoundException: The given key '99999' was not present in the dictionary`. The cart menu renders on every page, so the whole site is unusable until the basket is cleared.

**Acceptance Criteria:**

- **Given** the test stack is running with the `basket-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/basket-cart.spec.ts` › "a basket line for a product no longer in the catalog does not break the site"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/basket-cart.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/WebApp/Services/BasketState.cs:135` indexes `catalogItems[item.ProductId]` without checking that the product exists.
- **Suggested fix:** Skip (and optionally remove from the basket) lines whose product is missing; use `TryGetValue`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-8](BUG-8-basket-quantity-not-validated.md)
