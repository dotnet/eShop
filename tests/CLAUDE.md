# Tests

## Overview
Test projects for the eShop application using xUnit and MSTest.

## Test Projects
- `Basket.UnitTests` - Unit tests for Basket.API
- `Catalog.FunctionalTests` - Integration/functional tests for Catalog.API
- `ClientApp.UnitTests` - Unit tests for ClientApp
- `Ordering.FunctionalTests` - Integration/functional tests for Ordering
- `Ordering.UnitTests` - Unit tests for Ordering domain

## Running Tests
```powershell
# Run all tests
dotnet test

# Run specific project
dotnet test tests/Ordering.UnitTests/

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Patterns
- **Unit Tests**: Test individual components in isolation with mocks
- **Functional Tests**: Test API endpoints with in-memory or containerized dependencies

## Test Frameworks
- xUnit
- MSTest (Microsoft.Testing.Platform)
- FluentAssertions (where applicable)
- Moq for mocking
