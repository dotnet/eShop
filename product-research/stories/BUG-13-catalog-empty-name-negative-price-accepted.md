# BUG-13: [BE - Catalog.API] - Catalog accepts items with an empty name or a negative price

**Type:** Bug
**Severity:** Medium
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Catalog accepts items with an empty name or a negative price. Caught by the test "an empty name or a negative price is rejected with 400" in `e2e/teststack/catalog-admin.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. `POST http://localhost:18082/api/catalog/items?api-version=2.0` with `"name": ""`.
2. `POST` again with `"price": -5`.

**Expected:** `400 BadRequest` for both.

**Actual:** `201 Created` for both; the items show up in the store with no name or a negative price.

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-admin.spec.ts` › "an empty name or a negative price is rejected with 400"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-admin.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/Catalog.API/Apis/CatalogApi.cs:373-436` `UpdateItem` and `CreateItem` have no validation.
- `src/Catalog.API/Model/CatalogItem.cs` has no data annotations for `Name` or `Price`.
- **Suggested fix:** Require a non-empty name and `Price >= 0` (or `> 0`) on create and update.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-17](BUG-17-catalog-create-without-name-500.md)
