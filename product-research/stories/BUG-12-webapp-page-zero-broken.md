# BUG-12: [FE - WebApp] - Catalog page ?page=0 or ?page=-1 never loads

**Type:** Bug
**Severity:** Medium
**Category:** Validation
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Catalog page ?page=0 or ?page=-1 never loads. Caught by the test "?page=0 shows the first page" in `e2e/teststack/catalog-browse.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load catalog-edge
```

1. Open `http://localhost:18080/?page=0` (and `?page=-1`).

**Expected:** The first catalog page is shown.

**Actual:** The loading skeleton never goes away, and the error bar shows "An unhandled error has occurred." The WebApp asks Catalog.API for `pageIndex=-1`, which fails (see the related ticket).

**Acceptance Criteria:**

- **Given** the test stack is running with the `catalog-edge` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/catalog-browse.spec.ts` › "?page=0 shows the first page"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-browse.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/WebApp/Components/Pages/Catalog/Catalog.razor` line 199 passes `Page.GetValueOrDefault(1) - 1` as the page index without clamping `Page` to at least 1.
- **Suggested fix:** Clamp `Page` to at least 1 before calling the catalog service.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** [BUG-11](BUG-11-catalog-negative-paging-500.md)
