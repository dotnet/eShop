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
├── bdd/                            # BDD tests (Reqnroll)
│   ├── Features/                    # Gherkin feature files
│   ├── StepDefinitions/
│   ├── Drivers/                     # Driver pattern
│   ├── Support/
│   ├── Hooks/
│   ├── ReportPortal.json
│   └── {Service}.Api.BddTests.csproj
├── e2e/                            # End-to-end tests
│   ├── collections/                 # Postman collections
│   ├── environments/
│   ├── scripts/
│   └── data/
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
                        
                        Context.Current?.Log.Info($"âœ" Valid response for order: {orderNumber}");
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Context.Current?.Log.Error($"âŒ Failed for order number: {orderNumber}");
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
                
                Context.Current?.Log.Info($"âœ" Correctly rejected: {invalidOrderNumber}");
                
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

## BDD Testing (Reqnroll)

### Required Packages
```xml
<PackageReference Include="Reqnroll" Version="3.3.0" />
<PackageReference Include="Reqnroll.NUnit" Version="3.3.0" />
<PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="3.3.0" />
<PackageReference Include="ReportPortal.Reqnroll" Version="1.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="Shouldly" Version="4.2.1" />
```

### ReportPortal Configuration for BDD Tests

**Configuration File: ReportPortal.json** (place in BDD test project root)
```json
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-dotnet-reqnroll/master/src/ReportPortal.ReqnrollPlugin/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "https://your-reportportal-instance.com",
    "project": "your_project_name",
    "authentication": {
      "uuid": "your_api_key"
    }
  },
  "launch": {
    "name": "BDD Tests - {Service}.Api",
    "description": "Automated BDD acceptance tests",
    "debugMode": false,
    "attributes": [
      "bdd",
      "acceptance",
      "automated",
      "reqnroll"
    ],
    "tags": []
  },
  "stepReporting": {
    "enabled": true,
    "stepStartedEvent": "TestStepStarted"
  },
  "logging": {
    "attachments": {
      "enabled": true,
      "mimeTypes": [
        "text/plain",
        "application/json",
        "image/png"
      ]
    }
  }
}
```

**Reqnroll Configuration: reqnroll.json**
```json
{
  "$schema": "https://schemas.reqnroll.net/reqnroll-config-latest.json",
  "language": {
    "feature": "en-US"
  },
  "bindingCulture": {
    "name": "en-US"
  },
  "runtime": {
    "missingOrPendingStepsOutcome": "Inconclusive"
  },
  "trace": {
    "traceSuccessfulSteps": true,
    "traceTimings": true,
    "minTracedDuration": "0:0:0.1",
    "listener": "ReportPortal.ReqnrollPlugin.ReportPortalListener, ReportPortal.ReqnrollPlugin"
  },
  "plugins": [
    {
      "type": "GeneratorPlugin",
      "path": "ReportPortal.ReqnrollPlugin"
    }
  ]
}
```

### Valid Order Number Formats

Use these specific formats in feature files:

| Order Type | Format | Example |
|------------|--------|---------|
| EcomOrder | 0 + 9 digits | 0123456789 |
| ManualOrder | GS + 8 digits | GS12345678 |
| MaoOrder | 21 digits | 123456789012345678901 |
| ReplacementOrder | 18 + 8 digits | 1812345678 |
| RegearOrder | 8 alphanumeric | ABC12345 |

### Feature File Example

```gherkin
@invoice-management
Feature: Invoice Retrieval by Order Number

  Background:
    Given the invoice service is available
    And test data is seeded in the system

  @happy-path
  Scenario Outline: Retrieve existing invoice
    Given an invoice exists with order number "<orderNumber>"
    When I request the invoice for order number "<orderNumber>"
    Then I should receive the invoice details
    And the response should indicate success
    And the correlation ID should be present

    Examples:
      | orderNumber           | orderType        |
      | 0123456789           | EcomOrder        |
      | GS12345678           | ManualOrder      |
      | 123456789012345678901| MaoOrder         |
```

### In-Memory Repository Pattern

**Problem**: BDD tests should not depend on physical databases.

**Solution**: Implement in-memory repositories.

```csharp
public class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly Dictionary<string, Invoice> _invoices = new();
    private readonly List<string> _seededIds = new();

    public async Task<Invoice> GetByOrderNumberAsync(string orderNumber) =>
        await Task.FromResult(_invoices.Values.FirstOrDefault(i => i.OrderNumber == orderNumber));

    public async Task SaveAsync(Invoice invoice)
    {
        _invoices[invoice.Id] = invoice;
        await Task.CompletedTask;
    }

    public async Task ClearSeededDataAsync()
    {
        foreach (var id in _seededIds.ToList())
        {
            if (_invoices.ContainsKey(id))
            {
                _invoices.Remove(id);
                _seededIds.Remove(id);
            }
        }
        await Task.CompletedTask;
    }

    public void TrackSeededId(string id)
    {
        if (!_seededIds.Contains(id)) _seededIds.Add(id);
    }
}
```

### Test Data Seeding

```csharp
public class InMemoryDatabaseSeeder
{
    private readonly IInMemoryInvoiceRepository _invoiceRepository;

    public async Task SeedTestDataAsync()
    {
        var invoices = new[]
        {
            CreateInvoice("0123456789", "1234567890", "EcomOrder"),
            CreateInvoice("GS12345678", "9876543210", "ManualOrder"),
            CreateInvoice("123456789012345678901", "1111111111", "MaoOrder")
        };

        foreach (var invoice in invoices)
        {
            await _invoiceRepository.SaveAsync(invoice);
            _invoiceRepository.TrackSeededId(invoice.Id);
        }
    }

    private Invoice CreateInvoice(string orderNumber, string invoiceNumber, string orderType) =>
        new Invoice
        {
            Id = Guid.NewGuid().ToString(),
            OrderNumber = orderNumber,
            InvoiceNumber = invoiceNumber,
            OrderType = orderType,
            InvoiceDate = DateTime.UtcNow,
            Currency = "USD",
            TotalAmount = 100.00m
        };
}
```

### Test Hooks (Lifecycle Management)

```csharp
[Binding]
public class TestHooks
{
    private static IServiceProvider _serviceProvider;
    private static InMemoryDatabaseSeeder _seeder;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        _serviceProvider = services.BuildServiceProvider();

        _seeder = _serviceProvider.GetRequiredService<InMemoryDatabaseSeeder>();
        await _seeder.SeedTestDataAsync();
    }

    [BeforeScenario]
    public async Task BeforeScenario(ScenarioContext scenarioContext)
    {
        scenarioContext["StartTime"] = DateTime.UtcNow;
        scenarioContext["CorrelationId"] = Guid.NewGuid().ToString();
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        // Clean up only scenario-specific data
        var driver = _serviceProvider.GetService<InvoiceTestDriver>();
        if (driver != null)
            await driver.CleanupScenarioDataAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_seeder != null)
            await _seeder.ClearAllSeededDataAsync();

        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
```

### Driver Pattern

```csharp
public class InvoiceTestDriver
{
    private readonly HttpClient _httpClient;
    private readonly IInMemoryInvoiceRepository _repository;

    public InvoiceTestDriver(HttpClient httpClient, IInMemoryInvoiceRepository repository)
    {
        _httpClient = httpClient;
        _repository = repository;
    }

    public async Task<Invoice> GetSeededInvoiceAsync(string orderNumber) =>
        await _repository.GetByOrderNumberAsync(orderNumber);

    public async Task<ApiResponse<InvoiceRestModel>> GetInvoiceByOrderNumberAsync(string orderNumber)
    {
        var response = await _httpClient.GetAsync($"/api/v1/orders/{orderNumber}/invoices");
        var content = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<ApiResponse<InvoiceRestModel>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        
        return new ApiResponse<InvoiceRestModel>
        {
            Success = false,
            Message = content,
            StatusCode = (int)response.StatusCode
        };
    }

    public async Task CleanupScenarioDataAsync() =>
        await _repository.ClearScenarioDataAsync();
}
```

### Step Definitions (Avoid Ambiguity)

```csharp
using NUnit.Framework;
using Reqnroll;
using Shouldly;
using ReportPortal.Shared;

[Binding]
public class InvoiceRetrievalSteps
{
    private readonly InvoiceTestDriver _driver;
    private readonly ScenarioContext _scenarioContext;

    public InvoiceRetrievalSteps(InvoiceTestDriver driver, ScenarioContext scenarioContext)
    {
        _driver = driver;
        _scenarioContext = scenarioContext;
    }

    [Given(@"an invoice exists with order number ""(.*)""")]
    public async Task GivenAnInvoiceExistsWithOrderNumber(string orderNumber)
    {
        Context.Current?.Log.Debug($"Verifying invoice exists for order number: {orderNumber}");
        
        var invoice = await _driver.GetSeededInvoiceAsync(orderNumber);
        invoice.ShouldNotBeNull($"Seeded invoice with order number {orderNumber} should exist");
        
        _scenarioContext["ExpectedInvoice"] = invoice;
        
        Context.Current?.Log.Info($"Invoice found: {invoice.InvoiceNumber}");
    }

    [When(@"I request the invoice for order number ""(.*)""")]
    public async Task WhenIRequestTheInvoiceForOrderNumber(string orderNumber)
    {
        Context.Current?.Log.Debug($"Requesting invoice for order number: {orderNumber}");
        
        var correlationId = _scenarioContext["CorrelationId"].ToString();
        var response = await _driver.GetInvoiceByOrderNumberAsync(orderNumber, correlationId);
        
        _scenarioContext["ApiResponse"] = response;
        
        Context.Current?.Log.Info($"Response received with status: {response.StatusCode}");
    }

    [Then(@"I should receive the invoice details")]
    public void ThenIShouldReceiveTheInvoiceDetails()
    {
        var response = _scenarioContext.Get<ApiResponse<InvoiceRestModel>>("ApiResponse");
        var expectedInvoice = _scenarioContext.Get<Invoice>("ExpectedInvoice");
        
        Context.Current?.Log.Debug("Validating invoice details in response");
        
        response.Success.ShouldBeTrue();
        response.Data.ShouldNotBeNull();
        response.Data.OrderNumber.ShouldBe(expectedInvoice.OrderNumber);
        
        Context.Current?.Log.Info($"âœ" Invoice validation successful for order: {expectedInvoice.OrderNumber}");
        Context.Current?.Log.Debug($"Invoice Number: {response.Data.InvoiceNumber}");
        Context.Current?.Log.Debug($"Total Amount: {response.Data.TotalAmount}");
    }

    [Then(@"I should receive a not found response")]
    public void ThenIShouldReceiveANotFoundResponse()
    {
        var response = _scenarioContext.Get<ApiResponse<InvoiceRestModel>>("ApiResponse");
        
        Context.Current?.Log.Debug("Validating not found response");
        
        response.Success.ShouldBeFalse();
        response.StatusCode.ShouldBe(404);
        response.Message.ShouldNotBeNullOrEmpty();
        
        Context.Current?.Log.Info("âœ" Not found response validated successfully");
    }

    [Then(@"the error message should be descriptive")]
    public void ThenTheErrorMessageShouldBeDescriptive()
    {
        var response = _scenarioContext.Get<ApiResponse<InvoiceRestModel>>("ApiResponse");
        
        response.Message.ShouldNotBeNullOrEmpty();
        response.Message.Length.ShouldBeGreaterThan(10);
        
        Context.Current?.Log.Info($"Error message: {response.Message}");
    }
}

// Generic steps in CommonSteps.cs to avoid ambiguity
[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _scenarioContext;

    public CommonSteps(ScenarioContext scenarioContext) =>
        _scenarioContext = scenarioContext;

    [Given(@"the invoice service is available")]
    public void GivenTheInvoiceServiceIsAvailable()
    {
        Context.Current?.Log.Debug("Verifying service availability");
        // Service availability check
        Context.Current?.Log.Info("âœ" Service is available");
    }

    [Given(@"test data is seeded in the system")]
    public void GivenTestDataIsSeededInTheSystem()
    {
        Context.Current?.Log.Debug("Verifying test data is seeded");
        // Data seeding verification
        Context.Current?.Log.Info("âœ" Test data is seeded");
    }

    [Then(@"the correlation ID should be present")]
    public void ThenTheCorrelationIdShouldBePresent()
    {
        var response = _scenarioContext.Get<object>("ApiResponse");
        var correlationId = response.GetType().GetProperty("CorrelationId")?.GetValue(response);
        
        correlationId.ShouldNotBeNull();
        Context.Current?.Log.Info($"Correlation ID present: {correlationId}");
    }

    [Then(@"the response should indicate success")]
    public void ThenTheResponseShouldIndicateSuccess()
    {
        var response = _scenarioContext.Get<object>("ApiResponse");
        var successProperty = response.GetType().GetProperty("Success");
        successProperty.ShouldNotBeNull();
        
        var success = (bool)successProperty.GetValue(response);
        success.ShouldBeTrue();
        
        Context.Current?.Log.Info("âœ" Response indicates success");
    }
}
```

### ReportPortal Configuration

**File: ReportPortal.json**
```json
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-dotnet-reqnroll/master/src/ReportPortal.ReqnrollPlugin/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "https://reportportal.epam.com/",
    "project": "YOUR_PROJECT",
    "apiKey": "YOUR_API_KEY"
  },
  "launch": {
    "name": "BDD Test Execution",
    "description": "Automated BDD tests",
    "debugMode": true,
    "attributes": ["bdd", "automated"]
  }
}
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
        
        Context.Current?.Log.Info($"âœ" Test passed for invoice: {invoiceId}");
        
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
        
        Context.Current?.Log.Info("âœ" Invalid ID properly rejected");
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
        
        Context.Current?.Log.Info($"âœ" Correlation ID propagated: {correlationId}");
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
| BDD Testing | Reqnroll | Yes |
| Mutation Testing | Stryker.NET | Yes |
| Mocking | Moq | Yes |
| Assertions | Shouldly | Yes |
| BDD Reporting | ReportPortal.Reqnroll | Yes |
| Unit Reporting | ReportPortal.NUnit | Yes |

## References

- [FsCheck Documentation](https://fscheck.github.io/FsCheck/)
- [Reqnroll Documentation](https://docs.reqnroll.net/latest/)
- [ReportPortal Reqnroll](https://github.com/reportportal/agent-dotnet-reqnroll)
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