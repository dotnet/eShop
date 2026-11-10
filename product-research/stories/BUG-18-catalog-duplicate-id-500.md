# BUG-18: [BE - Catalog.API] - Creating a catalog item with an existing id returns 500

**Type:** Bug
**Severity:** Low
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Creating a catalog item with an existing id returns 500. Caught by the test "creating an item with an existing id is rejected with 409" in `e2e/teststack/catalog-admin.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. `POST http://localhost:18082/api/catalog/items?api-version=2.0` with `"id": 1` (already exists).

**Expected:** `409 Conflict`, or the id is ignored and a new one is generated.

**Actual:** `500`; Postgres raises a primary key violation.

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-admin.spec.ts` › "creating an item with an existing id is rejected with 409"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-admin.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Apis/CatalogApi.cs:415-436` copies `Id` from the body into the new entity.
- **Suggested fix:** Ignore the client id on create (let the database generate it) or check for an existing id and return `409`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-17](BUG-17-catalog-create-without-name-500.md)
