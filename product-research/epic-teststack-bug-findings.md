# Epic: Test Stack Bug Findings

## Summary

Fix the app bugs found while mapping every user and API flow against the
scenario-driven test stack (`testing/`). Each bug already has a Playwright
test in `e2e/teststack/` that asserts the correct behavior and is marked
`test.fixme`. A ticket is done when its test passes with the marker removed.

**Status:** Proposed
**Source:** `testing/flows/catalog.md`, `testing/flows/identity-basket.md`,
`testing/flows/ordering.md`

## Goals

- Close the security holes first: cross-account order access, anonymous
  catalog writes, the sign-in open redirect and missing token audience checks.
- Return `4xx` for bad input instead of `500` or silent success.
- Keep the site usable when stored data is stale (missing products).
- Turn every `test.fixme` in `e2e/teststack/` into a passing test.

## Non-Goals

- New features or UI changes beyond what each fix needs
- Changing the test stack itself (the tests already describe the target
  behavior)

## How to Work a Ticket

```bash
testing/teststack up                      # prebuilt images, ~35 s
testing/teststack load <scenario>         # from the ticket
# fix the code, then rebuild the changed service image
testing/teststack build && testing/teststack up
# remove test.fixme from the ticket's test and run its spec
npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/<spec>.spec.ts
```

## Tickets

4 critical, 3 high, 6 medium
and 8 low.

| Ticket | Severity | Category | Area | Title |
| --- | --- | --- | --- | --- |
| [BUG-1](stories/BUG-1-ordering-order-details-idor.md) | Critical | Security | BE - Ordering.API | Any signed-in user can read any order by id |
| [BUG-2](stories/BUG-2-ordering-cancel-ship-no-ownership.md) | Critical | Security | BE - Ordering.API | Any signed-in user can cancel or ship any order |
| [BUG-3](stories/BUG-3-ordering-create-order-trusts-userid.md) | Critical | Security | BE - Ordering.API | Create order uses the userId from the request body instead of the token |
| [BUG-4](stories/BUG-4-catalog-write-endpoints-unauthenticated.md) | Critical | Security | BE - Catalog.API | Catalog create, update and delete endpoints need no authentication |
| [BUG-5](stories/BUG-5-webapp-login-open-redirect.md) | High | Security | FE - WebApp | Open redirect after sign-in through a protocol-relative returnUrl |
| [BUG-6](stories/BUG-6-basket-token-without-scope-accepted.md) | High | Security | BE - Basket.API, Identity.API | Basket.API accepts access tokens issued without the basket scope |
| [BUG-7](stories/BUG-7-webapp-missing-product-breaks-site.md) | High | Reliability | FE - WebApp | A basket line for a product no longer in the catalog breaks every page |
| [BUG-8](stories/BUG-8-basket-quantity-not-validated.md) | Medium | Validation | BE - Basket.API | Basket.API stores zero and negative quantities |
| [BUG-9](stories/BUG-9-ordering-create-order-returns-200-on-failure.md) | Medium | Error handling | BE - Ordering.API | Create order returns 200 when the order was not created |
| [BUG-10](stories/BUG-10-ordering-domain-errors-return-500.md) | Medium | Error handling | BE - Ordering.API | Invalid order status changes return 500 instead of a client error |
| [BUG-11](stories/BUG-11-catalog-negative-paging-500.md) | Medium | Validation | BE - Catalog.API | Negative pageSize or pageIndex returns 500 |
| [BUG-12](stories/BUG-12-webapp-page-zero-broken.md) | Medium | Validation | FE - WebApp | Catalog page ?page=0 or ?page=-1 never loads |
| [BUG-13](stories/BUG-13-catalog-empty-name-negative-price-accepted.md) | Medium | Validation | BE - Catalog.API | Catalog accepts items with an empty name or a negative price |
| [BUG-14](stories/BUG-14-webapp-cart-quantity-race.md) | Low | Reliability | FE - WebApp | Typing a cart quantity, pressing Enter, then clicking Decrease can save the wrong quantity |
| [BUG-15](stories/BUG-15-ordering-draft-without-body-500.md) | Low | Error handling | BE - Ordering.API | Creating an order draft without a body returns 500 |
| [BUG-16](stories/BUG-16-webhooks-numeric-event-accepted.md) | Low | Validation | BE - Webhooks.API | Webhooks.API accepts numeric event names outside the allowed list |
| [BUG-17](stories/BUG-17-catalog-create-without-name-500.md) | Low | Validation | BE - Catalog.API | Creating a catalog item without a name returns 500 |
| [BUG-18](stories/BUG-18-catalog-duplicate-id-500.md) | Low | Validation | BE - Catalog.API | Creating a catalog item with an existing id returns 500 |
| [BUG-19](stories/BUG-19-catalog-unknown-brand-type-500.md) | Low | Validation | BE - Catalog.API | Unknown brand or type ids on create or update return 500 |
| [BUG-20](stories/BUG-20-catalog-v2-put-id-mismatch-500.md) | Low | Validation | BE - Catalog.API | v2 PUT with a body id different from the route id returns 500 |
| [BUG-21](stories/BUG-21-catalog-v1-put-without-id-404.md) | Low | Validation | BE - Catalog.API | v1 PUT without an id returns 404 instead of 400 |

## Suggested Order

1. Security (BUG-1 to BUG-6). BUG-6 (token audience) changes Identity.API
   config that every API depends on, so ship it with its own regression run.
2. Reliability (missing product, cart race).
3. Error handling and validation, grouped by service: Ordering.API, then
   Catalog.API, then Basket.API and Webhooks.API.
