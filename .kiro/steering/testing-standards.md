---
domain: testing
owner: ocp team
inclusion_mode: "conditional"
file_patterns:
- "*.cs"
---

# Testing Standards and Practices

## Overview

This document defines mandatory testing standards for all OCP microservices. It establishes a comprehensive testing strategy ensuring code quality, correctness, and reliability.

## Core Philosophy

### Testing Pyramid

All microservices **MUST** implement:

1. **Unit Tests** - Verify components with mocked dependencies (Many)
2. **Property-Based Tests** - Verify universal properties across valid inputs (Many)
3. **BDD Tests** - Executable business specifications (Moderate)
4. **Integration Tests** - Component interactions and external dependencies (Some)
5. **E2E Tests** - Critical user journeys (Few)
6. **Mutation Tests** - Test suite quality validation (Quality Gate)

```
      /\
     /  \    E2E Tests (Few)
    /____\   
   /      \   Integration Tests (Some)
  /________\  BDD Tests (Moderate)
 /          \ Unit + Property Tests (Many)
/____________\ Mutation Tests (Quality Gate)
```

### Feature-Specific Testing

When implementing tests for specific JIRA issues (e.g., ARCXDAUGAI-XX), identify business requirements and acceptance criteria to generate targeted tests aligned with this document.

## Coverage Requirements

| Test Type | Minimum Coverage | Target |
|-----------|------------------|--------|
| Unit Tests | 80% business logic | 85%+ |
| Property Tests | All identified properties | 100% |
| BDD Tests | All acceptance criteria | 100% |
| Integration Tests | All external dependencies | 100% |
| API Tests | All public endpoints | 100% |
| Mutation Score | Critical business logic | 80%+ |

## Project Structure

### Consolidated Directory Layout

```
tests/
├── unit/                            # Unit tests
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Validators/
│   ├── Middlewares/
│   ├── PropertyBased/               # Property-based tests
│   ├── Shared/
│   │   ├── Helpers/
│   │   ├── Mocks/
│   │   └── TestData/
│   └── {Service}.Api.UnitTests.csproj
├── integration/                     # Integration tests
│   ├── Api/
│   ├── Database/
│   ├── Messaging/
│   ├── Shared/
│   └── {Service}.Api.IntegrationTests.csproj
├── e2e/                            # End-to-end tests
│   ├── features/                   # Gherkin feature files
│   │   ├── invoice-management/
│   │   ├── order-processing/
│   │   └── payment-workflows/
│   ├── step-definitions/           # Step implementations
│   ├── support/                    # Test support utilities
│   │   ├── hooks.js
│   │   ├── world.js
│   │   └── helpers.js
│   ├── data/                       # Test data
│   ├── config/                     # Configuration
│   │   ├── environments/
│   │   └── cucumber.js
│   └── reports/                    # Test reports
├── performance/                     # Performance tests
│   ├── load/
│   └── benchmarks/
├── mutation/                        # Mutation testing
│   ├── stryker-config.json
│   └── scripts/
├── scripts/                         # Testing automation
│   ├── build/
│   ├── run/
│   ├── infrastructure/
│   └── reporting/
├── results/                         # Test results
│   └── [unit|integration|bdd|e2e|mutation|performance|coverage]/
└── config/                          # Configuration files
```

**Important**: Add all test projects to the solution file as referenced projects.

## Naming Conventions

### Unit Tests
```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Example: GetInvoiceByNumber_ValidInvoice_ReturnsSuccessResponse
}
```

### Property Tests
```csharp
[Test]
public void PropertyName_ShouldHoldUniversally_ForAllValidInputs()
{
    // **Feature: {feature-name}, Property {number}: {property-description}**
    // **Validates: Requirements {requirement-ids}**
}
```

### BDD Tests
```csharp
[Binding]
public class InvoiceRetrievalSteps
{
    // **Feature: invoice-management, Scenario: Retrieve invoice by valid order number**
    // **Validates: Requirements 1.1, 1.2**
}
```

### Integration Tests
```csharp
[Test]
public void Workflow_Scenario_ExpectedOutcome()
{
    // Example: InvoiceRetrieval_ExistingInvoice_ReturnsCompleteData
}
```

## Property-Based Testing

### Required Packages
```xml
<PackageReference Include="FsCheck" Version="2.16.6" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Shouldly" Version="4.2.1" />
<PackageReference Include="coverlet.collector" Version="3.1.2" />
<PackageReference Include="ReportPortal.NUnit" Version="4.8.0" />
<PackageReference Include="ReportPortal.VSTest.TestLogger" Version="4.0.0" />
```

### Configuration Standards
- **Minimum Iterations**: 100 per property test
- **Test Tagging**: Include feature and property references
- **Generators**: Use smart generators that constrain input space appropriately

### ReportPortal Integration for Unit Tests

**Configuration File: ReportPortal.config.json** (place in test project root)
```json
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-net-nunit/develop/src/ReportPortal.NUnitExtension/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "https://your-reportportal-instance.com",
    "project": "your_project_name",
    "authentication": {
      "uuid": "your_api_key"
    }
  },
  "launch": {
    "name": "Unit Tests - {Service}.Api",
    "description": "Automated unit and property-based tests",
    "debugMode": false,
    "attributes": [
      "unit-tests",
      "property-tests",
      "automated"
    ]
  }
}
```

**Test Assembly Setup** (add to test project):
```csharp
using NUnit.Framework;
using ReportPortal.NUnitExtension;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(4)]

namespace Ibs.Api.UnitTests
{
    [SetUpFixture]
    public class ReportPortalSetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // ReportPortal initialization happens automatically via NUnit extension
            // Additional global setup can be done here if needed
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // Global cleanup
        }
    }
}
```

**Test Execution with ReportPortal**:
```bash
# Using dotnet test with ReportPortal logger
dotnet test --logger:"console;verbosity=normal" --logger:"ReportPortal"

# Using VSTest logger
dotnet test --logger:"ReportPortal.VSTest.TestLogger.ReportPortalLogger,ReportPortal.VSTest.TestLogger"
```

### Domain-Specific Generators

```csharp
public static class OrderNumberGenerators
{
    // Valid EcomOrder: 0 + 9 digits
    public static Gen<string> ValidEcomOrderNumbers() =>
        from digits in Gen.Choose(100000000, 999999999)
        select $"0{digits}";

    // Valid ManualOrder: GS + 8 digits
    public static Gen<string> ValidManualOrderNumbers() =>
        from digits in Gen.Choose(10000000, 99999999)
        select $"GS{digits}";

    // Valid MaoOrder: 21 digits
    public static Gen<string> ValidMaoOrderNumbers() =>
        from number in Gen.Choose(100000000000000000000L, 999999999999999999999L)
        select number.ToString();

    // Combined generator for any valid order number
    public static Gen<string> ValidOrderNumbers() =>
        Gen.OneOf(
            ValidEcomOrderNumbers(),
            ValidManualOrderNumbers(),
            ValidMaoOrderNumbers(),
            ValidReplacementOrderNumbers(),
            ValidRegearOrderNumbers()
        );

    // Invalid order numbers for negative testing
    public static Gen<string> InvalidOrderNumbers() =>
        Gen.OneOf(
            Gen.Constant(""),
            Gen.Constant("   "),
            Gen.Constant("INVALID"),
            Gen.Elements("@#$%^&*()")
        );
}
```

### Property Categories

1. **Invariants**: Properties preserved after transformations
   ```csharp
   collection.Map(f).Count == collection.Count
   ```

2. **Round Trip**: Operation + inverse returns original
   ```csharp
   Deserialize(Serialize(obj)) == obj
   ```

3. **Idempotence**: Doing operation twice equals doing it once
   ```csharp
   collection.Distinct().Distinct() == collection.Distinct()
   ```

4. **Metamorphic**: Relationships between inputs/outputs
   ```csharp
   collection.Filter(predicate).Count <= collection.Count
   ```

### Example Implementation

```csharp
using NUnit.Framework;
using FsCheck;
using Shouldly;
using ReportPortal.Shared;

[TestFixture]
[Category("PropertyTests")]
[Description("API Response Format Property Tests")]
public class ApiResponsePropertyTests
{
    private InvoicesController _controller;
    private Mock<IInvoiceService> _mockService;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Setup shared across all tests
        _mockService = new Mock<IInvoiceService>();
    }

    [SetUp]
    public void SetUp()
    {
        _controller = new InvoicesController(_mockService.Object);
    }

    [Test]
    [Category("API")]
    [Property]
    [Description("Verifies API response structure consistency for all valid order numbers")]
    public void ApiResponse_ShouldHaveConsistentStructure_ForAllValidRequests()
    {
        // **Feature: api-consistency, Property 2: Response Format Consistency**
        // **Validates: Requirements API-2.1, API-2.2**
        
        // Add context to ReportPortal
        Context.Current?.Log.Info("Testing API response structure consistency");
        Context.Current?.Log.Debug($"Generator: ValidOrderNumbers with 100 iterations");
        
        Prop.ForAll(
            OrderNumberGenerators.ValidOrderNumbers(),
            orderNumber =>
            {
                try
                {
                    // Log test input to ReportPortal
                    Context.Current?.Log.Debug($"Testing with order number: {orderNumber}");
                    
                    // Act
                    var response = _controller.GetInvoiceByOrderNumber(orderNumber).Result;

                    // Assert - Universal response structure
                    if (response.Result is OkObjectResult okResult)
                    {
                        var apiResponse = okResult.Value as ApiResponse<InvoiceRestModel>;
                        apiResponse.ShouldNotBeNull();
                        apiResponse.Success.ShouldBeTrue();
                        apiResponse.CorrelationId.ShouldNotBeNullOrEmpty();
                        Context.Current?.Log.Info($"✓ Valid response for order: {orderNumber}");
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Context.Current?.Log.Error($"✗ Failed for order number: {orderNumber}");
                    Context.Current?.Log.Error($"Error: {ex.Message}");
                    throw;
                }
            })
            .QuickCheckThrowOnFailure();
    }

    [Test]
    [Category("Validation")]
    [Property]
    [Description("Verifies invalid order numbers are consistently rejected")]
    public void OrderNumberValidation_ShouldRejectInvalidFormats_Consistently()
    {
        // **Feature: input-validation, Property 3: Order Number Format Validation**
        // **Validates: Requirements VAL-1.1, VAL-1.2**
        
        Context.Current?.Log.Info("Testing order number validation for invalid inputs");
        
        Prop.ForAll(
            OrderNumberGenerators.InvalidOrderNumbers(),
            invalidOrderNumber =>
            {
                Context.Current?.Log.Debug($"Testing invalid order number: {invalidOrderNumber}");
                
                // Act
                var response = _controller.GetInvoiceByOrderNumber(invalidOrderNumber).Result;

                // Assert
                response.Result.ShouldBeOfType<BadRequestObjectResult>();
                
                var badRequestResult = response.Result as BadRequestObjectResult;
                var apiResponse = badRequestResult.Value as ApiResponse<InvoiceRestModel>;
                
                apiResponse.Success.ShouldBeFalse();
                apiResponse.Message.ShouldContain("Invalid order number format");
                
                Context.Current?.Log.Info($"✓ Correctly rejected: {invalidOrderNumber}");
               
                return true;
            })
            .QuickCheckThrowOnFailure();
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup after each test
        _mockService?.Reset();
    }
}
```

### Configuration Profiles

```csharp
public static class PropertyTestConfig
{
    public static Configuration DomainTestConfig => Configuration.Default
        .WithMaxTest(200)
        .WithStartSize(1)
        .WithEndSize(100)
        .WithQuietOnSuccess(false);

    public static Configuration QuickTestConfig => Configuration.Default
        .WithMaxTest(50)
        .WithStartSize(1)
        .WithEndSize(50)
        .WithQuietOnSuccess(true);
}
```

## E2E Testing with BDD and Gherkin

### Overview

End-to-end (E2E) tests validate complete user journeys and critical business workflows across the entire system. These tests use BDD principles with Gherkin syntax to ensure business requirements are met from a user's perspective.

### Purpose and Scope

E2E tests focus on:
- Complete user workflows from start to finish
- Integration of all system components
- Real or near-real production scenarios
- Business-critical paths
- Cross-service interactions

**Key Principle**: E2E tests should be **few but comprehensive** - testing critical paths that provide the highest business value.

### Technology Stack

#### Recommended Tools

| Tool | Purpose | Installation |
|------|---------|--------------|
| **Cucumber.js** | BDD test runner | `npm install --save-dev @cucumber/cucumber` |
| **Playwright** | Browser automation | `npm install --save-dev @playwright/test` |
| **Axios** | API requests | `npm install --save-dev axios` |
| **Chai** | Assertions | `npm install --save-dev chai` |
| **dotenv** | Environment config | `npm install --save-dev dotenv` |

#### Alternative Stack (API-focused)

For API-only E2E tests:
- **Postman/Newman** - Collection-based API testing
- **REST Assured** - Java-based REST API testing
- **SuperTest** - Node.js HTTP assertions

### Gherkin Feature Files

#### Feature File Structure

```gherkin
@tag-category
Feature: Feature Name
  As a [role]
  I want to [action]
  So that [business value]

  Background:
    Given [common precondition]
    And [another precondition]

  @happy-path @critical
  Scenario: Successful scenario name
    Given [initial context]
    When [action occurs]
    Then [expected outcome]
    And [additional verification]

  @error-handling
  Scenario Outline: Scenario with multiple examples
    Given [initial context with "<parameter>"]
    When [action with "<parameter>"]
    Then [expected outcome]

    Examples:
      | parameter | expected_result |
      | value1    | result1        |
      | value2    | result2        |
```

#### Writing Effective Scenarios

**Best Practices:**
1. **Use business language** - Avoid technical implementation details
2. **One scenario per business rule** - Keep scenarios focused
3. **Follow Given-When-Then structure** - Maintain clarity
4. **Make scenarios independent** - Each scenario should stand alone
5. **Use descriptive scenario names** - Clearly state what is being tested

**Example - Good vs Bad:**

```gherkin
# ❌ BAD: Too technical, implementation-focused
Scenario: POST request to /api/invoices returns 200
  Given I send a POST request to "/api/invoices"
  When the response status is 200
  Then the JSON response contains "invoiceNumber"

# ✅ GOOD: Business-focused, clear intent
Scenario: Create invoice for completed order
  Given an order "0123456789" has been completed
  When I request invoice generation for the order
  Then a new invoice should be created with order details
  And the customer should receive an invoice confirmation
```

### Feature Examples

#### Example 1: Invoice Management

**File:** `tests/e2e/features/invoice-management/retrieve-invoice.feature`

```gherkin
@invoice-management @critical
Feature: Invoice Retrieval
  As a customer service representative
  I want to retrieve customer invoices by order number
  So that I can provide invoice information to customers

  Background:
    Given the invoice service is available
    And the following invoices exist in the system:
      | orderNumber           | invoiceNumber | orderType   | totalAmount | currency |
      | 0123456789           | INV-2024-001  | EcomOrder   | 150.00      | USD      |
      | GS12345678           | INV-2024-002  | ManualOrder | 250.00      | USD      |
      | 123456789012345678901| INV-2024-003  | MaoOrder    | 350.00      | USD      |

  @happy-path
  Scenario Outline: Successfully retrieve invoice by valid order number
    Given I am an authenticated customer service representative
    When I request the invoice for order number "<orderNumber>"
    Then I should receive a successful response
    And the invoice number should be "<invoiceNumber>"
    And the order type should be "<orderType>"
    And the total amount should be "<totalAmount>" in "<currency>"
    And a correlation ID should be present in the response

    Examples:
      | orderNumber           | invoiceNumber | orderType   | totalAmount | currency |
      | 0123456789           | INV-2024-001  | EcomOrder   | 150.00      | USD      |
      | GS12345678           | INV-2024-002  | ManualOrder | 250.00      | USD      |
      | 123456789012345678901| INV-2024-003  | MaoOrder    | 350.00      | USD      |

  @error-handling
  Scenario: Attempt to retrieve invoice with non-existent order number
    Given I am an authenticated customer service representative
    When I request the invoice for order number "0999999999"
    Then I should receive a not found response
    And the error message should indicate "Invoice not found for order number"
    And a correlation ID should be present in the response

  @error-handling
  Scenario Outline: Attempt to retrieve invoice with invalid order number format
    Given I am an authenticated customer service representative
    When I request the invoice for order number "<invalidOrderNumber>"
    Then I should receive a bad request response
    And the error message should indicate "Invalid order number format"

    Examples:
      | invalidOrderNumber |
      |                   |
      | INVALID@123       |
      | 123               |
      | GS123             |

  @performance
  Scenario: Invoice retrieval responds within acceptable time
    Given I am an authenticated customer service representative
    When I request the invoice for order number "0123456789"
    Then the response should be received within 2 seconds
    And the response should be successful
```

#### Example 2: Order Processing Workflow

**File:** `tests/e2e/features/order-processing/complete-order-workflow.feature`

```gherkin
@order-processing @critical @workflow
Feature: Complete Order to Invoice Workflow
  As a business
  I want orders to be processed through to invoice generation
  So that customers receive proper billing documentation

  Background:
    Given the order service is available
    And the invoice service is available
    And the payment service is available

  @happy-path @end-to-end
  Scenario: Complete order lifecycle from creation to invoice
    Given a new customer order is created with:
      | productSku | quantity | unitPrice |
      | SKU-001    | 2        | 75.00     |
      | SKU-002    | 1        | 50.00     |
    When the order is submitted for processing
    Then the order should be validated successfully
    And payment should be authorized for 200.00 USD
    When payment is captured successfully
    Then the order status should be "Completed"
    And an invoice should be automatically generated
    And the invoice should contain all order line items
    And the invoice total should match the order total
    And the customer should receive an invoice email notification

  @error-handling
  Scenario: Order processing fails when payment is declined
    Given a new customer order is created with total amount 500.00 USD
    When the order is submitted for processing
    And payment authorization is declined
    Then the order status should be "Payment Failed"
    And no invoice should be generated
    And the customer should receive a payment failure notification

  @compensation
  Scenario: Rollback invoice when order is cancelled after generation
    Given an order "0123456789" is completed with invoice "INV-2024-100"
    When the order is cancelled by customer service
    Then the invoice "INV-2024-100" should be marked as void
    And the order status should be "Cancelled"
    And a credit note should be generated
    And the customer should receive a cancellation confirmation
```

#### Example 3: Multi-Service Integration

**File:** `tests/e2e/features/integration/cross-service-communication.feature`

```gherkin
@integration @cross-service
Feature: Cross-Service Data Consistency
  As a system
  I want data to remain consistent across all microservices
  So that business operations are reliable

  @data-consistency @critical
  Scenario: Order data propagates correctly to invoice service
    Given a new order is created in the order service with:
      | orderNumber  | customerEmail       | totalAmount |
      | 0987654321  | test@example.com    | 300.00      |
    And the order is completed successfully
    When the invoice is generated
    Then the invoice service should have the correct order details:
      | field         | expectedValue      |
      | orderNumber   | 0987654321        |
      | customerEmail | test@example.com  |
      | totalAmount   | 300.00            |
    And the invoice service should maintain data consistency
    And both services should return the same correlation ID

  @idempotency
  Scenario: Duplicate invoice generation requests are handled correctly
    Given an order "0123456789" has an existing invoice "INV-2024-001"
    When I request invoice generation for order "0123456789" again
    Then the system should return the existing invoice "INV-2024-001"
    And no duplicate invoice should be created
    And the response should indicate idempotent operation
```

### Step Definitions

#### JavaScript/Cucumber Example

**File:** `tests/e2e/step-definitions/invoice-steps.js`

```javascript
const { Given, When, Then } = require('@cucumber/cucumber');
const { expect } = require('chai');
const axios = require('axios');

// Configuration
const BASE_URL = process.env.API_BASE_URL || 'https://service.k8s-preprod03.arcteryx.io';
const API_VERSION = 'v1';

Given('the invoice service is available', async function() {
  try {
    const response = await axios.get(`${BASE_URL}/health`);
    expect(response.status).to.equal(200);
    this.log('✓ Invoice service is healthy');
  } catch (error) {
    throw new Error(`Invoice service is not available: ${error.message}`);
  }
});

Given('the following invoices exist in the system:', async function(dataTable) {
  const invoices = dataTable.hashes();
  
  for (const invoice of invoices) {
    // Seed test data via API or database
    await this.testDataHelper.seedInvoice(invoice);
    this.log(`Seeded invoice: ${invoice.invoiceNumber} for order: ${invoice.orderNumber}`);
  }
  
  // Store for later verification
  this.seededInvoices = invoices;
});

Given('I am an authenticated customer service representative', async function() {
  // Obtain authentication token
  this.authToken = await this.authHelper.getServiceToken('customer-service-rep');
  this.log('✓ Authenticated as customer service representative');
});

When('I request the invoice for order number {string}', async function(orderNumber) {
  this.correlationId = this.generateCorrelationId();
  
  try {
    this.response = await axios.get(
      `${BASE_URL}/api/${API_VERSION}/orders/${orderNumber}/invoices`,
      {
        headers: {
          'Authorization': `Bearer ${this.authToken}`,
          'X-Correlation-ID': this.correlationId,
          'Content-Type': 'application/json'
        }
      }
    );
    
    this.log(`Request sent for order: ${orderNumber}`);
    this.log(`Response status: ${this.response.status}`);
    
  } catch (error) {
    // Store error response for validation
    this.response = error.response;
    this.log(`Request failed with status: ${error.response?.status}`);
  }
});

Then('I should receive a successful response', function() {
  expect(this.response.status).to.equal(200);
  expect(this.response.data.success).to.be.true;
  this.log('✓ Received successful response');
});

Then('the invoice number should be {string}', function(expectedInvoiceNumber) {
  const actualInvoiceNumber = this.response.data.data.invoiceNumber;
  expect(actualInvoiceNumber).to.equal(expectedInvoiceNumber);
  this.log(`✓ Invoice number verified: ${actualInvoiceNumber}`);
});

Then('the order type should be {string}', function(expectedOrderType) {
  const actualOrderType = this.response.data.data.orderType;
  expect(actualOrderType).to.equal(expectedOrderType);
  this.log(`✓ Order type verified: ${actualOrderType}`);
});

Then('the total amount should be {string} in {string}', function(expectedAmount, expectedCurrency) {
  const invoice = this.response.data.data;
  expect(invoice.totalAmount.toString()).to.equal(expectedAmount);
  expect(invoice.currency).to.equal(expectedCurrency);
  this.log(`✓ Amount verified: ${expectedAmount} ${expectedCurrency}`);
});

Then('a correlation ID should be present in the response', function() {
  const responseCorrelationId = this.response.data.correlationId;
  expect(responseCorrelationId).to.exist;
  expect(responseCorrelationId).to.equal(this.correlationId);
  this.log(`✓ Correlation ID verified: ${responseCorrelationId}`);
});

Then('I should receive a not found response', function() {
  expect(this.response.status).to.equal(404);
  expect(this.response.data.success).to.be.false;
  this.log('✓ Received 404 Not Found response');
});

Then('I should receive a bad request response', function() {
  expect(this.response.status).to.equal(400);
  expect(this.response.data.success).to.be.false;
  this.log('✓ Received 400 Bad Request response');
});

Then('the error message should indicate {string}', function(expectedMessageContent) {
  const actualMessage = this.response.data.message;
  expect(actualMessage).to.include(expectedMessageContent);
  this.log(`✓ Error message verified: ${actualMessage}`);
});

Then('the response should be received within {int} seconds', function(maxSeconds) {
  const responseTime = this.response.headers['x-response-time'] || this.responseTime;
  const responseTimeSeconds = parseInt(responseTime) / 1000;
  
  expect(responseTimeSeconds).to.be.lessThan(maxSeconds);
  this.log(`✓ Response time: ${responseTimeSeconds}s (limit: ${maxSeconds}s)`);
});
```

#### Support Files

**File:** `tests/e2e/support/world.js`

```javascript
const { setWorldConstructor, Before, After } = require('@cucumber/cucumber');
const { v4: uuidv4 } = require('uuid');

class CustomWorld {
  constructor() {
    this.response = null;
    this.authToken = null;
    this.correlationId = null;
    this.seededInvoices = [];
    this.testStartTime = Date.now();
  }

  generateCorrelationId() {
    return uuidv4();
  }

  log(message) {
    console.log(`[${new Date().toISOString()}] ${message}`);
  }

  async cleanup() {
    // Cleanup test data
    if (this.seededInvoices.length > 0) {
      this.log('Cleaning up seeded test data...');
      for (const invoice of this.seededInvoices) {
        await this.testDataHelper.deleteInvoice(invoice.invoiceNumber);
      }
    }
  }
}

setWorldConstructor(CustomWorld);

Before(async function() {
  this.log('=== Starting scenario ===');
  this.testStartTime = Date.now();
});

After(async function() {
  const duration = Date.now() - this.testStartTime;
  this.log(`=== Scenario completed in ${duration}ms ===`);
  await this.cleanup();
});
```

**File:** `tests/e2e/support/hooks.js`

```javascript
const { BeforeAll, AfterAll, Before, After, Status } = require('@cucumber/cucumber');
const { TestDataHelper } = require('./helpers/test-data-helper');
const { AuthHelper } = require('./helpers/auth-helper');

BeforeAll(async function() {
  console.log('=== E2E Test Suite Starting ===');
  console.log(`Environment: ${process.env.TEST_ENV || 'preprod'}`);
  console.log(`Base URL: ${process.env.API_BASE_URL}`);
});

Before(async function() {
  // Initialize helpers for each scenario
  this.testDataHelper = new TestDataHelper();
  this.authHelper = new AuthHelper();
  this.responseTime = null;
});

After(async function(scenario) {
  if (scenario.result.status === Status.FAILED) {
    // Capture failure information
    this.log(`FAILED: ${scenario.pickle.name}`);
    this.log(`Error: ${scenario.result.message}`);
    
    // Attach response data if available
    if (this.response) {
      this.attach(JSON.stringify(this.response.data, null, 2), 'application/json');
    }
  }
});

AfterAll(async function() {
  console.log('=== E2E Test Suite Completed ===');
});
```

### Configuration

#### Cucumber Configuration

**File:** `tests/e2e/config/cucumber.js`

```javascript
module.exports = {
  default: {
    require: [
      'tests/e2e/step-definitions/**/*.js',
      'tests/e2e/support/**/*.js'
    ],
    format: [
      'progress-bar',
      'html:tests/e2e/reports/html/cucumber-report.html',
      'json:tests/e2e/reports/json/cucumber-report.json',
      'junit:tests/e2e/reports/junit/cucumber-report.xml'
    ],
    formatOptions: {
      snippetInterface: 'async-await'
    },
    publishQuiet: true,
    parallel: 2,
    retry: 1,
    retryTagFilter: '@flaky'
  },
  
  ci: {
    format: [
      'json:tests/e2e/reports/json/cucumber-report.json',
      'junit:tests/e2e/reports/junit/cucumber-report.xml'
    ],
    parallel: 4,
    retry: 2
  }
};
```

#### Environment Configuration

**File:** `tests/e2e/config/environments/preprod.json`

```json
{
  "environment": "preprod",
  "baseUrl": "https://service.k8s-preprod03.arcteryx.io",
  "apiVersion": "v1",
  "auth": {
    "tokenEndpoint": "https://auth.k8s-preprod03.arcteryx.io/token",
    "clientId": "${AUTH_CLIENT_ID}",
    "clientSecret": "${AUTH_CLIENT_SECRET}"
  },
  "timeouts": {
    "default": 30000,
    "extended": 60000
  },
  "retry": {
    "attempts": 2,
    "delay": 1000

  }
}
```

### Running E2E Tests

#### Local Execution

```bash
# Run all E2E tests
npm run test:e2e

# Run specific feature
npm run test:e2e -- tests/e2e/features/invoice-management/retrieve-invoice.feature

# Run tests with specific tags
npm run test:e2e -- --tags "@critical"
npm run test:e2e -- --tags "@happy-path and not @flaky"

# Run in specific environment
TEST_ENV=preprod npm run test:e2e

# Run with detailed output
npm run test:e2e -- --format progress
```

#### Package.json Scripts

```json
{
  "scripts": {
    "test:e2e": "cucumber-js --config tests/e2e/config/cucumber.js",
    "test:e2e:ci": "cucumber-js --profile ci --config tests/e2e/config/cucumber.js",
    "test:e2e:critical": "npm run test:e2e -- --tags '@critical'",
    "test:e2e:smoke": "npm run test:e2e -- --tags '@smoke'",
    "test:e2e:report": "open tests/e2e/reports/html/cucumber-report.html"
  }
}
```

### E2E CI/CD Integration

#### GitHub Actions Example

```yaml
name: E2E Tests

on:
  pull_request:
    branches: [ main, develop ]
  schedule:
    - cron: '0 2 * * *'  # Nightly at 2 AM

jobs:
  e2e-tests:
    runs-on: ubuntu-latest
    
    strategy:
      matrix:
        environment: [preprod]
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '18'
        cache: 'npm'
    
    - name: Install dependencies
      run: npm ci
    
    - name: Run E2E Tests
      env:
        TEST_ENV: ${{ matrix.environment }}
        API_BASE_URL: ${{ secrets.PREPROD_API_URL }}
        AUTH_CLIENT_ID: ${{ secrets.AUTH_CLIENT_ID }}
        AUTH_CLIENT_SECRET: ${{ secrets.AUTH_CLIENT_SECRET }}
      run: npm run test:e2e:ci
    
    - name: Upload Test Results
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: e2e-test-results
        path: tests/e2e/reports/
    
    - name: Publish Test Report
      if: always()
      uses: dorny/test-reporter@v1
      with:
        name: E2E Test Results
        path: tests/e2e/reports/junit/*.xml
        reporter: java-junit
```

### E2E Best Practices

#### Feature Writing

1. **Focus on business value** - Write from user perspective
2. **Keep scenarios independent** - No dependencies between scenarios
3. **Use meaningful tags** - Organize and filter effectively
4. **Avoid UI details** - Focus on behavior, not implementation
5. **Keep scenarios concise** - One behavior per scenario

#### Step Definitions

1. **Reuse common steps** - Create shared step library
2. **Use page/service objects** - Abstract API/UI interactions
3. **Handle errors gracefully** - Provide clear failure messages
4. **Log appropriately** - Aid debugging without noise
5. **Clean up test data** - Ensure idempotent tests

#### Performance

1. **Run E2E tests in parallel** - Reduce execution time
2. **Tag slow tests** - Separate from smoke tests
3. **Use appropriate waits** - Avoid brittle sleep statements
4. **Monitor test duration** - Identify and optimize slow tests
5. **Implement retry logic** - Handle transient failures

### E2E Tag Strategy

| Tag | Purpose | When to Use |
|-----|---------|-------------|
| `@critical` | Business-critical paths | Must pass before deployment |
| `@smoke` | Quick validation | Post-deployment verification |
| `@regression` | Full test coverage | Scheduled test runs |
| `@happy-path` | Successful scenarios | Primary user flows |
| `@error-handling` | Error scenarios | Edge cases and failures |
| `@performance` | Performance tests | SLA validation |
| `@flaky` | Unstable tests | Tests needing investigation |
| `@wip` | Work in progress | Tests under development |

### E2E Reporting

#### HTML Report Example

```javascript
// Generate custom HTML report
const reporter = require('cucumber-html-reporter');

const options = {
  theme: 'bootstrap',
  jsonFile: 'tests/e2e/reports/json/cucumber-report.json',
  output: 'tests/e2e/reports/html/cucumber-report.html',
  reportSuiteAsScenarios: true,
  scenarioTimestamp: true,
  launchReport: true,
  metadata: {
    'Test Environment': process.env.TEST_ENV,
    'Browser': 'N/A (API Tests)',
    'Platform': process.platform,
    'Executed': new Date().toISOString()
  }
};

reporter.generate(options);
```

### E2E Troubleshooting

#### Common Issues

**Issue: Step definition not found**
```
Solution: Ensure step definitions are in the correct directory and match feature file steps exactly
```

**Issue: Authentication failures**
```
Solution: Verify environment variables are set correctly and tokens are valid
```

**Issue: Flaky tests**
```
Solution: 
- Add appropriate waits instead of fixed sleeps
- Ensure proper test data cleanup
- Tag as @flaky and investigate root cause
```

**Issue: Timeout errors**
```
Solution: Increase timeout in cucumber configuration or optimize slow operations
```

## Mutation Testing (Stryker.NET)

### Installation
```bash
dotnet tool install -g dotnet-stryker
```

### Configuration (stryker-config.json)
```json
{
  "stryker-config": {
    "project": "src/{Service}.Api/{Service}.Api.csproj",
    "test-projects": [
      "tests/unit/{Service}.Api.UnitTests.csproj",
      "tests/bdd/{Service}.Api.BddTests.csproj"
    ],
    "reporters": ["html", "json", "cleartext"],
    "thresholds": {
      "high": 80,
      "low": 60,
      "break": 60
    },
    "mutate": [
      "src/{Service}.Api/**/*.cs",
      "!src/{Service}.Api/Program.cs",
      "!src/{Service}.Api/Startup.cs"
    ],
    "coverage-analysis": "perTest",
    "max-concurrent-test-runners": 4
  }
}
```

### Mutation Score Targets

| Component | Minimum Score |
|-----------|--------------|
| Critical Business Logic | 80% |
| Service Layer | 75% |
| API Controllers | 70% |
| Repository Layer | 65% |

### Execution
```bash
# Run mutation tests
dotnet stryker

# With configuration file
dotnet stryker --config-file stryker-config.json

# With baseline comparison
dotnet stryker --baseline:main
```

## Unit Testing

### Test Structure (AAA Pattern)
```csharp
using NUnit.Framework;
using Moq;
using Shouldly;
using ReportPortal.Shared;

[TestFixture]
[Category("UnitTests")]
[Description("Invoice Controller Unit Tests")]
public class InvoiceControllerTests
{
    private InvoicesController _controller;
    private Mock<IInvoiceService> _mockService;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Context.Current?.Log.Info("Setting up Invoice Controller test suite");
    }

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IInvoiceService>();
        _controller = new InvoicesController(_mockService.Object);
        
        Context.Current?.Log.Debug("Test setup completed");
    }

    [Test]
    [Category("Controllers")]
    [Description("Verifies successful invoice retrieval with valid ID")]
    public async Task GetInvoice_ValidId_ReturnsOkResult()
    {
        // Arrange
        var invoiceId = "INV-2024-001";
        var expectedInvoice = new Invoice { InvoiceNumber = invoiceId };
        
        _mockService.Setup(s => s.GetInvoiceAsync(invoiceId))
            .ReturnsAsync(expectedInvoice);
        
        Context.Current?.Log.Debug($"Testing with invoice ID: {invoiceId}");
        
        // Act
        var result = await _controller.GetInvoice(invoiceId);
        
        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var apiResponse = okResult.Value.ShouldBeOfType<ApiResponse<InvoiceModel>>();
        apiResponse.Success.ShouldBeTrue();
        apiResponse.Data.ShouldNotBeNull();
        
        Context.Current?.Log.Info($"✓ Test passed for invoice: {invoiceId}");

        
        // Verify mock interactions
        _mockService.Verify(s => s.GetInvoiceAsync(invoiceId), Times.Once);
    }

    [Test]
    [Category("Controllers")]
    [Category("ErrorHandling")]
    [Description("Verifies proper error handling for invalid invoice ID")]
    public async Task GetInvoice_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        var invalidId = "INVALID@ID";
        
        Context.Current?.Log.Debug($"Testing with invalid ID: {invalidId}");
        
        // Act
        var result = await _controller.GetInvoice(invalidId);
        
        // Assert
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        var apiResponse = badRequestResult.Value.ShouldBeOfType<ApiResponse<InvoiceModel>>();
        apiResponse.Success.ShouldBeFalse();
        apiResponse.Message.ShouldContain("Invalid");
    
        Context.Current?.Log.Info("✓ Invalid ID properly rejected");
    }

    [Test]
    [Category("Controllers")]
    [Description("Verifies correlation ID is properly propagated")]
    public async Task GetInvoice_WithCorrelationId_PropagatesId()
    {
        // Arrange
        var invoiceId = "INV-2024-001";
        var correlationId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        _mockService.Setup(s => s.GetInvoiceAsync(invoiceId))
            .ReturnsAsync(new Invoice { InvoiceNumber = invoiceId });
        
        Context.Current?.Log.Debug($"Testing correlation ID propagation: {correlationId}");
        
        // Act
        var result = await _controller.GetInvoice(invoiceId);
        
        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var apiResponse = okResult.Value.ShouldBeOfType<ApiResponse<InvoiceModel>>();
        apiResponse.CorrelationId.ShouldBe(correlationId);
        

        Context.Current?.Log.Info($"✓ Correlation ID propagated: {correlationId}");
    }

    [TearDown]
    public void TearDown()
    {
        _mockService?.Reset();
        Context.Current?.Log.Debug("Test cleanup completed");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Context.Current?.Log.Info("Invoice Controller test suite completed");
    }
}
```

### Mock Usage Guidelines

**When to Mock:**
- External dependencies (databases, APIs, file systems)
- Complex collaborators with side effects
- Slow or unreliable dependencies

**When NOT to Mock:**
- Value objects and data structures
- Simple collaborators without side effects
- The system under test

### Test Data Builders
```csharp
public class InvoiceTestBuilder
{
    private string _invoiceNumber = "INV-2024-001";
    private string _orderNumber = "ORD-2024-001";
    
    public InvoiceTestBuilder WithInvoiceNumber(string invoiceNumber)
    {
        _invoiceNumber = invoiceNumber;
        return this;
    }
    
    public Invoice Build() => new Invoice
    {
        InvoiceNumber = _invoiceNumber,
        OrderNumber = _orderNumber
    };
}
```

## Integration Testing

### Database Helper (Flexible Constructors)
```csharp
public class DatabaseHelper
{
    private readonly IMongoDatabase _database;

    // Support multiple constructor patterns
    public DatabaseHelper(IMongoDatabase database) =>
        _database = database ?? throw new ArgumentNullException(nameof(database));

    public DatabaseHelper(string connectionString)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("test_database");
    }

    public async Task ClearDatabase()
    {
        var collections = await _database.ListCollectionNamesAsync();
        await collections.ForEachAsync(async collectionName =>
            await _database.DropCollectionAsync(collectionName));
    }
}
```

### Type Conflict Resolution
```csharp
// Handle namespace conflicts
using DataObjectsInvoice = Ibs.Api.DataObjects.
using DataObjectsInvoice = Ibs.Api.DataObjects.Invoice;
using PlatformInvoice = Ibs.Api.DataObjects.Platform.Invoice;

public class TestDataHelper
{
    public DataObjectsInvoice CreateTestInvoice(string orderNumber) =>
        new DataObjectsInvoice { OrderNumber = orderNumber };

    public PlatformInvoice CreatePlatformInvoice(string orderNumber) =>
        new PlatformInvoice { OrderNumber = orderNumber };
}
```

## API Testing

### Controller Testing
```csharp
[Test]
public async Task GetInvoice_ValidId_ReturnsOkResult()
{
    // Arrange
    var invoiceId = "INV-2024-001";
    SetupMockData(invoiceId);
    
    // Act
    var result = await _controller.GetInvoice(invoiceId);
    
    // Assert
    var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
    var apiResponse = okResult.Value.ShouldBeOfType<ApiResponse<InvoiceModel>>();
    apiResponse.Success.ShouldBeTrue();
}
```

### Correlation ID Testing
```csharp
[Test]
public async Task ApiEndpoint_WithCorrelationId_PropagatesId()
{
    // Arrange
    var correlationId = Guid.NewGuid().ToString();
    _httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
    
    // Act
    var result = await _controller.GetInvoice("INV-001");
    
    // Assert
    var apiResponse = ExtractApiResponse(result);
    apiResponse.CorrelationId.ShouldBe(correlationId);
}
```

## E2E Testing

### Postman Collection Structure
```json
{
  "info": {
    "name": "{Service} E2E Tests"
  },
  "item": [
    {
      "name": "Happy Path Scenarios",
      "item": []
    },
    {
      "name": "Error Handling",
      "item": []
    }
  ]
}
```

### Test Scripts
```javascript
pm.test('Status code is 200', () => {
    pm.response.to.have.status(200);
});

pm.test('Response has correct structure', () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('success', true);
    pm.expect(jsonData).to.have.property('correlationId');
});
```

## Environment Strategy

### Environment Usage Rules

**Pre-Production (Default for Testing):**
- âœ… **MUST** use for all testing
- âœ… Safe for experimentation
- âœ… Used in all examples
- Base URL: `https://service.k8s-preprod03.arcteryx.io`

**Production:**
- âŒ **NEVER** use for testing
- âœ… Live traffic only
- âœ… Reference in documentation only

## CI/CD Integration


### GitHub Actions Pipeline

This pipeline is fully aligned with the official ReportPortal CI/CD integration guidelines:
https://reportportal.io/docs/quality-gates/IntegrationWithCICD/IntegrationWithGitHubActions

#### ReportPortal CI/CD Principles (Mandatory)

- Each CI job **MUST create a unique launch**
- Launch names **MUST be deterministic and traceable**
- All ReportPortal credentials **MUST be injected via secrets**
- Quality Gates **MUST be evaluated and enforced**
- Pipeline **MUST fail** if a Quality Gate fails

#### Launch Naming Strategy

**Required format:**
```
GitHubActions | <repository> | <test-type> | <branch> | #<run-number>
```

**Example:**
```
GitHubActions | invoice-api | UnitTests | main | #1432
```

#### Mandatory Environment Variables

| Variable | Description |
|--------|-------------|
| RP_UUID | ReportPortal API key |
| RP_ENDPOINT | ReportPortal endpoint |
| RP_PROJECT | ReportPortal project |
| RP_LAUNCH | Unique launch name |
| RP_ATTRIBUTES | Launch attributes (recommended) |

#### Launch Attributes (Strongly Recommended)

Attributes are required for filtering, dashboards, and quality gates.

```
unit-tests,
branch:${{ github.ref_name }},
commit:${{ github.sha }},
trigger:${{ github.event_name }}
```

#### Quality Gate Enforcement

Every pipeline **MUST explicitly validate** the ReportPortal Quality Gate.

```yaml
- name: ReportPortal Quality Gate
  uses: reportportal/action-quality-gate@v1
  with:
    endpoint: ${{ secrets.RP_ENDPOINT }}
    project: ${{ secrets.RP_PROJECT }}
    token: ${{ secrets.RP_UUID }}
    launch: ${{ env.RP_LAUNCH }}
```

If the gate fails, the pipeline **MUST fail**.

#### Parallel Jobs Strategy

- Unit, BDD, Integration, and E2E tests **MUST use separate launches**
- Never reuse a launch name across jobs or retries
- Retries **MUST include** `github.run_attempt` in the launch name

---

```yaml
name: Test Pipeline

on:
  pull_request:
    branches: [ main, develop ]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Run Unit Tests with ReportPortal
      run: dotnet test tests/unit --logger:"console;verbosity=normal" --logger:"ReportPortal"
      env:
        RP_UUID: ${{ secrets.REPORTPORTAL_API_KEY }}
        RP_ENDPOINT: ${{ secrets.REPORTPORTAL_ENDPOINT }}
        RP_PROJECT: ${{ secrets.REPORTPORTAL_PROJECT }}
        RP_LAUNCH: >
          GitHubActions |
          ${{ github.repository }} |
          UnitTests |
          ${{ github.ref_name }} |
          #${{ github.run_number }}

    - name: Enforce ReportPortal Quality Gate
      uses: reportportal/action-quality-gate@v1
      with:
        endpoint: ${{ secrets.REPORTPORTAL_ENDPOINT }}
        project: ${{ secrets.REPORTPORTAL_PROJECT }}
        token: ${{ secrets.REPORTPORTAL_API_KEY }}
        launch: ${{ env.RP_LAUNCH }}
```


### ReportPortal Environment Variables

For CI/CD integration, configure these secrets in your GitHub repository:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `REPORTPORTAL_API_KEY` | Your ReportPortal API key (UUID) | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `REPORTPORTAL_ENDPOINT` | ReportPortal server URL | `https://reportportal.yourcompany.com` |
| `REPORTPORTAL_PROJECT` | Project name in ReportPortal | `ocp-microservices` |

**Override Configuration via Environment Variables:**
```bash
# Override ReportPortal settings for local execution
export RP_UUID="your-api-key"
export RP_ENDPOINT="https://reportportal.yourcompany.com"
export RP_PROJECT="your-project"
export RP_LAUNCH="Local Development - $(date +%Y%m%d_%H%M%S)"

# Run tests
dotnet test tests/unit --logger:"ReportPortal"
```

## Troubleshooting

### Common Issues

#### Ambiguous Step Definitions
```csharp
// ❌ WRONG: Duplicate steps in multiple files
[Then(@"the correlation ID should be present")]
public void ThenTheCorrelationIdShouldBePresent() { }

// ✅ CORRECT: Single step in CommonSteps.cs
[Binding]
public class CommonSteps
{
    [Then(@"the correlation ID should be present")]
    public void ThenTheCorrelationIdShouldBePresent() { }
}
```

#### Invalid Order Numbers
```gherkin
# ❌ WRONG
Given an invoice exists with order number "ORD-BDD-001"

# ✅ CORRECT
Given an invoice exists with order number "0123456789"
```

#### Type Conflicts
```csharp
// ✅ Use type aliases
using DataObjectsInvoice = Ibs.Api.DataObjects.Invoice;
using PlatformInvoice = Ibs.Api.DataObjects.Platform.Invoice;
```

### Debugging

```csharp
[Binding]
public class DiagnosticSteps
{
    [AfterStep]
    public void LogStepExecution()
    {
        var stepInfo = _scenarioContext.StepContext.StepInfo;
        Console.WriteLine($"Step: {stepInfo.StepDefinitionType} {stepInfo.Text}");
    }

    [AfterScenario]
    public void LogScenarioResult()
    {
        if (_scenarioContext.TestError != null)
        {
            Console.WriteLine($"Scenario failed: {_scenarioContext.ScenarioInfo.Title}");
            Console.WriteLine($"Error: {_scenarioContext.TestError.Message}");
        }
    }
}
```

## Quality Gates

### Definition of Done
- [ ] Unit tests: 80%+ coverage
- [ ] Property tests: All properties implemented
- [ ] BDD tests: All acceptance criteria covered
- [ ] Integration tests: All dependencies tested
- [ ] API tests: All endpoints covered
- [ ] Mutation score: 80%+ for critical logic
- [ ] All tests pass in CI
- [ ] Documentation updated

### Performance Criteria
- Unit tests: < 100ms per test
- Property tests: 100+ iterations
- BDD scenarios: < 30s per scenario
- Integration tests: < 5s per test
- E2E scenarios: < 2min per scenario

## Tool Stack

### .NET Services
| Purpose | Tool | Mandatory |
|---------|------|-----------|
| Unit Testing | NUnit | Yes |
| Property Testing | FsCheck | Yes |
| BDD Testing | Playwright + Cucumber | Yes |
| Mutation Testing | Stryker.NET | Yes |
| Mocking | Moq | Yes |
| Assertions | Shouldly | Yes |
| BDD Reporting | ReportPortal.CucumberJS | Yes |
| Unit Reporting | ReportPortal.NUnit | Yes |

## References

- [FsCheck Documentation](https://fscheck.github.io/FsCheck/)
- [ReportPortal Playwright](https://reportportal.io/docs/log-data-in-reportportal/test-framework-integration/JavaScript/Playwright/)
- [ReportPortal Playwright Example](https://github.com/reportportal/agent-js-playwright#readme)
- [ReportPortal NUnit](https://github.com/reportportal/agent-net-nunit)
- [ReportPortal VSTest](https://github.com/reportportal/agent-net-vstest)
- [ReportPortal Serilog](https://github.com/reportportal/logger-net-serilog#readme)
- [ReportPortal GitHub Actions](https://reportportal.io/docs/quality-gates/IntegrationWithCICD/IntegrationWithGitHubActions)
- [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/)
- [Gherkin Reference](https://cucumber.io/docs/gherkin/reference/)

---

**Last Updated**: January 2025  
**Maintained By**: OCP Team  
**Review Cycle**: Quarterly