# BUG-17: [BE - Catalog.API] - Creating a catalog item without a name returns 500

**Type:** Bug
**Severity:** Low
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Creating a catalog item without a name returns 500. Caught by the test "a body without a name is rejected with 400" in `e2e/teststack/catalog-admin.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. `POST http://localhost:18082/api/catalog/items?api-version=2.0` with a body that has no `name`.

**Expected:** `400 BadRequest` with a validation problem.

**Actual:** `500`; Postgres raises a NOT NULL violation on `Name`.

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-admin.spec.ts` › "a body without a name is rejected with 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-admin.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Apis/CatalogApi.cs:415-436` `CreateItem` saves the body without validation.
- **Suggested fix:** Validate required fields before saving (shared validator for create and update).

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-13](BUG-13-catalog-empty-name-negative-price-accepted.md), [BUG-18](BUG-18-catalog-duplicate-id-500.md), [BUG-19](BUG-19-catalog-unknown-brand-type-500.md)
