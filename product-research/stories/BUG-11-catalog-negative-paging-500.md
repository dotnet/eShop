# BUG-11: [BE - Catalog.API] - Negative pageSize or pageIndex returns 500

**Type:** Bug
**Severity:** Medium
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Negative pageSize or pageIndex returns 500. Caught by the test "negative pageSize or pageIndex is rejected with 400" in `e2e/teststack/catalog-api.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-pictures
```

1. `GET http://localhost:18082/api/catalog/items?api-version=2.0&pageSize=-1`.
2. `GET http://localhost:18082/api/catalog/items?api-version=2.0&pageIndex=-1`. The mobile BFF route (`http://localhost:18087`) behaves the same.

**Expected:** `400 BadRequest` with a validation problem.

**Actual:** `500`; Postgres reports `LIMIT must not be negative` / `OFFSET must not be negative`.

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-pictures` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-api.spec.ts` › "negative pageSize or pageIndex is rejected with 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-api.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Model/PaginationRequest.cs:8-12` has no range constraints.
- `src/Catalog.API/Apis/CatalogApi.cs:138-163,291-333` pass the values straight to `Skip`/`Take`.
- **Suggested fix:** Validate `PageSize` (1..100) and `PageIndex` (>= 0) and return `400`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-12](BUG-12-webapp-page-zero-broken.md)
