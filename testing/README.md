# eShop Comprehensive Testing Suite

This directory contains the complete testing infrastructure for the eShop reference application, implementing the comprehensive testing approach defined in the testing standards.

## Testing Philosophy

The eShop testing suite implements a multi-layered testing strategy:

1. **Unit Tests**: Fast, isolated tests with mocked dependencies
2. **Property-Based Tests**: Verify universal properties across all valid inputs using FsCheck
3. **BDD Tests**: Business requirements validation through executable specifications using Reqnroll
4. **Integration Tests**: Component interactions and external dependencies
5. **Mutation Tests**: Test suite quality validation using Stryker.NET
6. **E2E Tests**: Full application workflow tests using Playwright

## Directory Structure

```
testing/
├── unit/                        # Unit tests for all services
├── integration/                 # Integration tests
├── bdd/                        # BDD tests (Reqnroll)
├── e2e/                        # End-to-end tests (Playwright)
├── performance/                # Performance and load tests
├── mutation/                   # Mutation testing configuration
├── scripts/                    # Testing automation scripts
├── results/                    # Test execution results
├── config/                     # Testing configuration
└── shared/                     # Shared test utilities
```

## Services Covered

- **Basket.API**: Shopping cart management
- **Catalog.API**: Product catalog and search
- **Identity.API**: Authentication and user management
- **Ordering.API**: Order processing and management
- **Webhooks.API**: Webhook notifications
- **OrderProcessor**: Background order processing
- **PaymentProcessor**: Payment handling
- **WebApp**: Blazor web application
- **ClientApp**: .NET MAUI mobile app

## Quality Gates

- **Unit Test Coverage**: 80% minimum for business logic
- **Property Test Coverage**: All identified universal properties
- **BDD Test Coverage**: All acceptance criteria from requirements
- **Mutation Score**: 80% minimum for critical business logic
- **Integration Test Coverage**: All external dependencies

## Running Tests

### All Tests
```bash
dotnet run --project testing/scripts/run/run-all-tests.ps1
```

### Specific Test Types
```bash
# Unit tests
dotnet test testing/unit/ --logger trx

# Property tests
dotnet test testing/unit/PropertyBased/ --logger trx

# BDD tests
dotnet test testing/bdd/ --logger trx

# Integration tests
dotnet test testing/integration/ --logger trx

# E2E tests
npx playwright test

# Mutation tests
dotnet stryker --config-file testing/mutation/stryker-config.json
```

## Test Data Management

All tests use controlled test data following domain-specific patterns:
- **Order Numbers**: Valid formats for EcomOrder, ManualOrder, MaoOrder, etc.
- **Product IDs**: Catalog-specific identifiers
- **Customer Data**: Realistic but anonymized customer information
- **Payment Data**: Test payment scenarios with mock processors

## Environment Configuration

Tests are configured to run against:
- **Local**: In-memory repositories and mock services
- **Integration**: Test containers with real databases
- **E2E**: Full application stack with Aspire orchestration

## Reporting

Test results are automatically generated in multiple formats:
- **TRX**: For CI/CD integration
- **HTML**: For detailed analysis
- **JSON**: For programmatic processing
- **Allure**: For comprehensive BDD reporting

## Maintenance

This testing suite is maintained according to the testing standards and should be updated whenever:
- New features are added to any service
- Business requirements change
- New integration points are introduced
- Performance requirements are modified

For detailed implementation guidelines, see the individual test project documentation.