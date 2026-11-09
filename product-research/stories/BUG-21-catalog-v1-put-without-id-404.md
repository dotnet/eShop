# BUG-21: [BE - Catalog.API] - v1 PUT without an id returns 404 instead of 400

**Type:** Bug
**Severity:** Low
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

v1 PUT without an id returns 404 instead of 400. Caught by the test "v1 PUT without an id in the body is 400" in `e2e/teststack/catalog-admin.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. `PUT http://localhost:18082/api/catalog/items?api-version=1.0` with a body that has no `id`.

**Expected:** `400 BadRequest` "Item id must be provided in the request body."

**Actual:** `404` "Item with id 0 not found."

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-admin.spec.ts` › "v1 PUT without an id in the body is 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-admin.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Apis/CatalogApi.cs:364` checks `productToUpdate?.Id == null`, but `Id` is an `int`, so the check is dead code and a missing id becomes `0`.
- **Suggested fix:** Check for `Id <= 0` (or make the bound id nullable) and return the existing `400`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-20](BUG-20-catalog-v2-put-id-mismatch-500.md)
