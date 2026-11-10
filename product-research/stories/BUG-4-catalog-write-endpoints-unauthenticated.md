# BUG-4: [BE - Catalog.API] - Catalog create, update and delete endpoints need no authentication

**Type:** Bug
**Severity:** Critical
**Category:** Security
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Catalog create, update and delete endpoints need no authentication. Caught by the test "anonymous callers cannot create, update or delete items" in `e2e/teststack/catalog-admin.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. With no `Authorization` header, `POST http://localhost:18082/api/catalog/items?api-version=2.0` with a valid item.
2. `PUT http://localhost:18082/api/catalog/items/1?api-version=2.0` and `DELETE http://localhost:18082/api/catalog/items/1?api-version=2.0`, also anonymous.

**Expected:** `401 Unauthorized` for anonymous callers; only an admin role can change the catalog.

**Actual:** `201`, `201` and `204`: anyone on the network can change or delete products and prices. A price change also publishes `ProductPriceChangedIntegrationEvent`.

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-admin.spec.ts` › "anonymous callers cannot create, update or delete items"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-admin.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Apis/CatalogApi.cs:100-115` maps `UpdateItemV1`, `UpdateItem`, `CreateItem` and `DeleteItemById` with no `RequireAuthorization()`.
- Catalog.API does not call `AddDefaultAuthentication()` today, and Identity.API has no `catalog` API scope.
- **Suggested fix:** Add authentication to Catalog.API, a `catalog` scope/role in Identity.API, and `RequireAuthorization("CatalogAdmin")` on the write endpoints. Reads stay anonymous.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** None
