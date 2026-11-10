# BUG-19: [BE - Catalog.API] - Unknown brand or type ids on create or update return 500

**Type:** Bug
**Severity:** Low
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Unknown brand or type ids on create or update return 500. Caught by the test "unknown brand or type ids are rejected with 400" in `e2e/teststack/catalog-admin.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. `POST http://localhost:18082/api/catalog/items?api-version=2.0` with `"catalogBrandId": 999`.
2. `PUT http://localhost:18082/api/catalog/items/1?api-version=2.0` with `"catalogTypeId": 999` (test: "v2 PUT with an unknown type id is 400").

**Expected:** `400 BadRequest` naming the unknown id.

**Actual:** `500`; Postgres raises a foreign key violation.

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-admin.spec.ts` › "unknown brand or type ids are rejected with 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-admin.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Apis/CatalogApi.cs:373-436` saves without checking `CatalogBrandId` and `CatalogTypeId`.
- **Suggested fix:** Check that the brand and type exist before saving and return `400`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-17](BUG-17-catalog-create-without-name-500.md)
