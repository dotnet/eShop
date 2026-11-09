# BUG-6: [BE - Basket.API, Identity.API] - Basket.API accepts access tokens issued without the basket scope

**Type:** Bug
**Severity:** High
**Category:** Security
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Basket.API accepts access tokens issued without the basket scope. Caught by the test "a token issued without the basket scope cannot update the basket" in `e2e/teststack/basket-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load basket-edge
```

1. Get an access token for alice with scopes `openid profile orders` only (no `basket`), via `getTokens()` in `e2e/teststack/auth-basket-helpers.ts`.
2. Call gRPC `BasketApi.UpdateBasket` on `http://localhost:18083` with that token.

**Expected:** `UNAUTHENTICATED` (or `PERMISSION_DENIED`); nothing is written to Redis.

**Actual:** `OK`, and the basket is written to Redis. Any token from Identity.API works against any API.

**Acceptance Criteria:**

- **Given** the test stack is running with the `basket-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/basket-api.spec.ts` › "a token issued without the basket scope cannot update the basket"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/basket-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Identity.API/Configuration/Config.cs:6-14` declares the `ApiResource`s without scopes, so tokens carry no `aud` claim.
- `src/eShop.ServiceDefaults/AuthenticationExtensions.cs:49` sets `ValidateAudience = false` for every service.
- **Suggested fix:** Attach each `ApiScope` to its `ApiResource` so tokens get an audience, then validate the audience (or require the scope claim) in each API. The same change protects Ordering.API and Webhooks.API.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** None
