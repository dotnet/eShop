# BUG-14: [FE - WebApp] - Typing a cart quantity, pressing Enter, then clicking Decrease can save the wrong quantity

**Type:** Bug
**Severity:** Low
**Category:** Reliability
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Typing a cart quantity, pressing Enter, then clicking Decrease can save the wrong quantity. Caught by the test "typing a quantity, pressing Enter, then clicking Decrease reliably stores the decremented quantity" in `e2e/teststack/basket-cart.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load basket-edge
```

1. Sign in as bob and open the cart.
2. Type `5` in a quantity box and press Enter.
3. Immediately click the Decrease (−) button for that line.

**Expected:** The stored quantity is 4, matching the UI.

**Actual:** Sometimes Redis keeps 5 while the UI shows 4 (reproduced intermittently).

**Acceptance Criteria:**

- **Given** the test stack is running with the `basket-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/basket-cart.spec.ts` › "typing a quantity, pressing Enter, then clicking Decrease reliably stores the decremented quantity"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/basket-cart.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/WebApp/wwwroot/js/cart-quantity.js:37-55` commits on Enter but leaves the input focused. The click blurs it, which fires `change` and submits the typed value again, in parallel with the decrement POST.
- **Suggested fix:** Blur (or mark as committed) on Enter so the following `change` does not submit again, or ignore `change` while a submit is in flight.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** None
