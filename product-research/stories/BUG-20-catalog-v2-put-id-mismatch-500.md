# BUG-20: [BE - Catalog.API] - v2 PUT with a body id different from the route id returns 500

**Type:** Bug
**Severity:** Low
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

v2 PUT with a body id different from the route id returns 500. Caught by the test "v2 PUT with a body id different from the route id is 400" in `e2e/teststack/catalog-admin.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. `PUT http://localhost:18082/api/catalog/items/1?api-version=2.0` with a body whose `id` is 2.

**Expected:** `400 BadRequest` "Route id and body id must match".

**Actual:** `500`: EF tries to change the key of a tracked entity.

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-admin.spec.ts` › "v2 PUT with a body id different from the route id is 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-admin.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Apis/CatalogApi.cs:390` `catalogEntry.CurrentValues.SetValues(productToUpdate)` copies the body's `Id` onto the entity loaded by the route id.
- **Suggested fix:** Return `400` when both ids are set and differ, or ignore the body id.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-21](BUG-21-catalog-v1-put-without-id-404.md)
