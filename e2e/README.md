# eShop E2E Tests

End-to-end tests for eShop using Cucumber/Gherkin and Playwright.

## Quick Start

```bash
# Install dependencies
npm install

# Start eShop application
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Run smoke tests (2 min)
npm run test:e2e:smoke

# View report
npm run test:e2e:report
```

## Test Structure

```
e2e/
├── features/              # Gherkin scenarios (29 scenarios)
│   ├── catalog/          # Product browsing (4)
│   ├── basket/           # Shopping cart (6)
│   ├── ordering/         # Order processing (6)
│   ├── identity/         # Authentication (7)
│   └── integration/      # Cross-service (6)
├── step-definitions/     # TypeScript implementations
├── support/              # Test infrastructure
│   ├── world.ts         # Playwright integration
│   └── hooks.ts         # Setup/teardown
├── config/               # Cucumber & environment config
└── reports/              # Generated reports
```

## Running Tests

```bash
# All tests
npm run test:e2e

# Smoke tests (quick validation)
npm run test:e2e:smoke

# Critical tests only
npm run test:e2e:critical

# CI mode (headless, with retries)
npm run test:e2e:ci

# Specific feature
npx cucumber-js e2e/features/catalog/browse-products.feature

# By tags
npx cucumber-js --tags "@happy-path"
npx cucumber-js --tags "@critical and not @flaky"
```

## Environment Setup

Create `.env` in project root:

```env
BASE_URL=http://localhost:5045
USERNAME1=alice
PASSWORD=Pass123$
HEADLESS=false
```

## Test Tags

| Tag | Purpose | Count |
|-----|---------|-------|
| `@smoke` | Quick validation | 2 |
| `@critical` | Must-pass scenarios | 15 |
| `@happy-path` | Successful flows | 18 |
| `@error-handling` | Error scenarios | 8 |
| `@performance` | Performance tests | 5 |
| `@integration` | Cross-service | 6 |

## Writing Tests

### 1. Create Feature File

```gherkin
@my-feature @critical
Feature: My Feature
  As a user
  I want to do something
  So that I achieve value

  @happy-path
  Scenario: Success case
    Given I am on the homepage
    When I perform an action
    Then I see the expected result
```

### 2. Implement Steps

```typescript
import { Given, When, Then } from '@cucumber/cucumber';

Given('I am on the homepage', async function() {
  await this.page.goto('/');
  this.log('✓ On homepage');
});
```

### 3. Run Test

```bash
npx cucumber-js e2e/features/my-feature.feature
```

## Debugging

```bash
# Visible browser
HEADLESS=false npm run test:e2e:smoke

# Slow motion
SLOW_MO=100 npm run test:e2e:smoke

# Record video
RECORD_VIDEO=true npm run test:e2e:smoke
```

## Reports

Generated after each run:
- **HTML**: `e2e/reports/html/cucumber-report.html` (open with `npm run test:e2e:report`)
- **JSON**: `e2e/reports/json/cucumber-report.json`
- **JUnit**: `e2e/reports/junit/cucumber-report.xml`

## ReportPortal Integration

ReportPortal integration is available but currently disabled. See [REPORTPORTAL_ENABLE_GUIDE.md](../REPORTPORTAL_ENABLE_GUIDE.md) for setup instructions.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| App not running | `dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj` |
| Port conflict | Update `BASE_URL` in `.env` |
| Timeout errors | Increase timeout in `e2e/config/environments/local.json` |
| Step not found | Ensure step definition matches feature file exactly |

## References

- [Quick Reference](QUICK_REFERENCE.md) - Common commands and patterns
- [Testing Standards](../.kiro/steering/testing-standards.md) - Full testing guidelines
- [Cucumber Docs](https://cucumber.io/docs/cucumber/)
- [Playwright Docs](https://playwright.dev/)
