# Migration baseline

Baseline captured on 2026-08-16 before the first service port.

## Automated results

```powershell
dotnet test --solution eShop.Web.slnf --no-progress --output detailed
```

- 85 tests passed.
- 0 tests failed or skipped.
- Includes Basket and Ordering unit tests plus Catalog and Ordering functional
  tests against containerized PostgreSQL.

The Catalog pagination test was made order-independent because its shared
fixture permits two add-item cases to finish before or after the pagination
assertion. Its contract remains 101 seeded items and at most two test-created
items.

```powershell
$env:ESHOP_USE_HTTP_ENDPOINTS = '1'
$env:USERNAME1 = 'bob'
$env:PASSWORD = 'Pass123$'
npx playwright test
```

- 4 Playwright setup/journey tests passed.
- Covered login, anonymous catalog browsing, adding an item, and removing an
  item.

## Known baseline warnings

- NuGet reports existing moderate and high severity advisories for transitive
  `MessagePack` 2.5.192 in the functional test projects.
- `npm ci` reports two existing high severity development-dependency
  vulnerabilities.

These warnings predate the Java foundation. They should be tracked separately
and must not be hidden by the migration.
