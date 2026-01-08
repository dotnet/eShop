#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs all tests in the eShop testing suite
.DESCRIPTION
    Executes unit tests, property tests, BDD tests, integration tests, and E2E tests
    Generates comprehensive test reports and coverage analysis
.PARAMETER TestType
    Specific test type to run (Unit, Property, BDD, Integration, E2E, Mutation, All)
.PARAMETER Environment
    Target environment (Local, CI, Performance)
.PARAMETER GenerateReports
    Whether to generate detailed test reports
.EXAMPLE
    ./run-all-tests.ps1 -TestType All -Environment Local -GenerateReports
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Unit", "Property", "BDD", "Integration", "E2E", "Mutation", "All")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Local", "CI", "Performance")]
    [string]$Environment = "Local",
    
    [Parameter(Mandatory = $false)]
    [switch]$GenerateReports = $true,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Parallel = $true
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ResultsDir = Join-Path $RootDir "results"

# Ensure results directory exists
if (-not (Test-Path $ResultsDir)) {
    New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
}

# Initialize test results
$TestResults = @{
    StartTime = Get-Date
    Environment = $Environment
    TestType = $TestType
    Results = @{}
    Summary = @{
        TotalTests = 0
        PassedTests = 0
        FailedTests = 0
        SkippedTests = 0
        Duration = [TimeSpan]::Zero
    }
}

Write-Host "🚀 Starting eShop Test Suite Execution" -ForegroundColor Green
Write-Host "Test Type: $TestType" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Generate Reports: $GenerateReports" -ForegroundColor Cyan
Write-Host "Parallel Execution: $Parallel" -ForegroundColor Cyan
Write-Host ""

# Function to run a specific test type
function Invoke-TestType {
    param(
        [string]$Type,
        [string]$Command,
        [string]$WorkingDirectory = $RootDir,
        [hashtable]$Environment = @{}
    )
    
    Write-Host "📋 Running $Type Tests..." -ForegroundColor Yellow
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        # Set environment variables
        foreach ($key in $Environment.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $Environment[$key], "Process")
        }
        
        # Execute command
        $result = Invoke-Expression $Command
        $exitCode = $LASTEXITCODE
        
        $stopwatch.Stop()
        
        if ($exitCode -eq 0) {
            Write-Host "✅ $Type Tests Passed" -ForegroundColor Green
            $TestResults.Results[$Type] = @{
                Status = "Passed"
                Duration = $stopwatch.Elapsed
                ExitCode = $exitCode
            }
        } else {
            Write-Host "❌ $Type Tests Failed (Exit Code: $exitCode)" -ForegroundColor Red
            $TestResults.Results[$Type] = @{
                Status = "Failed"
                Duration = $stopwatch.Elapsed
                ExitCode = $exitCode
            }
        }
    }
    catch {
        $stopwatch.Stop()
        Write-Host "💥 $Type Tests Error: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.Results[$Type] = @{
            Status = "Error"
            Duration = $stopwatch.Elapsed
            Error = $_.Exception.Message
        }
    }
    
    Write-Host "⏱️  $Type Tests Duration: $($stopwatch.Elapsed)" -ForegroundColor Cyan
    Write-Host ""
}

# Build solution if not skipped
if (-not $SkipBuild) {
    Write-Host "🔨 Building Solution..." -ForegroundColor Yellow
    $buildResult = dotnet build "$RootDir/eShop.slnx" --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build Failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build Successful" -ForegroundColor Green
    Write-Host ""
}

# Configure test execution based on environment
$testConfig = @{
    Local = @{
        Timeout = 300000  # 5 minutes
        Verbosity = "normal"
        Parallel = $true
    }
    CI = @{
        Timeout = 600000  # 10 minutes
        Verbosity = "minimal"
        Parallel = $true
    }
    Performance = @{
        Timeout = 1800000  # 30 minutes
        Verbosity = "detailed"
        Parallel = $false
    }
}

$config = $testConfig[$Environment]

# Run Unit Tests
if ($TestType -eq "All" -or $TestType -eq "Unit") {
    $unitTestsPath = Join-Path $RootDir "testing/unit"
    $unitResultsPath = Join-Path $ResultsDir "unit"
    
    if (-not (Test-Path $unitResultsPath)) {
        New-Item -ItemType Directory -Path $unitResultsPath -Force | Out-Null
    }
    
    $unitCommand = "dotnet test `"$unitTestsPath`" --configuration Release --logger trx --logger html --results-directory `"$unitResultsPath`" --verbosity $($config.Verbosity)"
    
    if ($config.Parallel) {
        $unitCommand += " --parallel"
    }
    
    Invoke-TestType -Type "Unit" -Command $unitCommand
}

# Run Property-Based Tests
if ($TestType -eq "All" -or $TestType -eq "Property") {
    $propertyTestsPath = Join-Path $RootDir "testing/unit/PropertyBased"
    $propertyResultsPath = Join-Path $ResultsDir "property"
    
    if (-not (Test-Path $propertyResultsPath)) {
        New-Item -ItemType Directory -Path $propertyResultsPath -Force | Out-Null
    }
    
    $propertyCommand = "dotnet test `"$propertyTestsPath`" --configuration Release --logger trx --logger html --results-directory `"$propertyResultsPath`" --verbosity $($config.Verbosity) --filter Category=Property"
    
    Invoke-TestType -Type "Property" -Command $propertyCommand
}

# Run BDD Tests
if ($TestType -eq "All" -or $TestType -eq "BDD") {
    $bddTestsPath = Join-Path $RootDir "testing/bdd"
    $bddResultsPath = Join-Path $ResultsDir "bdd"
    
    if (-not (Test-Path $bddResultsPath)) {
        New-Item -ItemType Directory -Path $bddResultsPath -Force | Out-Null
    }
    
    $bddCommand = "dotnet test `"$bddTestsPath`" --configuration Release --logger trx --logger html --results-directory `"$bddResultsPath`" --verbosity $($config.Verbosity)"
    
    $bddEnvironment = @{
        "ASPNETCORE_ENVIRONMENT" = "Testing"
        "ALLURE_RESULTS_DIRECTORY" = $bddResultsPath
    }
    
    Invoke-TestType -Type "BDD" -Command $bddCommand -Environment $bddEnvironment
}

# Run Integration Tests
if ($TestType -eq "All" -or $TestType -eq "Integration") {
    $integrationTestsPath = Join-Path $RootDir "testing/integration"
    $integrationResultsPath = Join-Path $ResultsDir "integration"
    
    if (-not (Test-Path $integrationResultsPath)) {
        New-Item -ItemType Directory -Path $integrationResultsPath -Force | Out-Null
    }
    
    $integrationCommand = "dotnet test `"$integrationTestsPath`" --configuration Release --logger trx --logger html --results-directory `"$integrationResultsPath`" --verbosity $($config.Verbosity)"
    
    $integrationEnvironment = @{
        "ASPNETCORE_ENVIRONMENT" = "Testing"
        "USE_TEST_CONTAINERS" = "true"
    }
    
    Invoke-TestType -Type "Integration" -Command $integrationCommand -Environment $integrationEnvironment
}

# Run E2E Tests
if ($TestType -eq "All" -or $TestType -eq "E2E") {
    $e2eTestsPath = Join-Path $RootDir "testing/e2e"
    $e2eResultsPath = Join-Path $ResultsDir "e2e"
    
    if (-not (Test-Path $e2eResultsPath)) {
        New-Item -ItemType Directory -Path $e2eResultsPath -Force | Out-Null
    }
    
    # Check if Playwright is installed
    Push-Location $e2eTestsPath
    try {
        $playwrightCommand = "npx playwright test --reporter=html --reporter=json --output-dir=`"$e2eResultsPath`""
        
        $e2eEnvironment = @{
            "BASE_URL" = "https://localhost:5001"
            "CI" = if ($Environment -eq "CI") { "true" } else { "false" }
        }
        
        Invoke-TestType -Type "E2E" -Command $playwrightCommand -WorkingDirectory $e2eTestsPath -Environment $e2eEnvironment
    }
    finally {
        Pop-Location
    }
}

# Run Mutation Tests
if ($TestType -eq "All" -or $TestType -eq "Mutation") {
    $mutationConfigPath = Join-Path $RootDir "testing/mutation/stryker-config.json"
    $mutationResultsPath = Join-Path $ResultsDir "mutation"
    
    if (-not (Test-Path $mutationResultsPath)) {
        New-Item -ItemType Directory -Path $mutationResultsPath -Force | Out-Null
    }
    
    $mutationCommand = "dotnet stryker --config-file `"$mutationConfigPath`" --output `"$mutationResultsPath`""
    
    # Only run mutation tests in CI or when explicitly requested
    if ($Environment -eq "CI" -or $TestType -eq "Mutation") {
        Invoke-TestType -Type "Mutation" -Command $mutationCommand
    } else {
        Write-Host "⏭️  Skipping Mutation Tests (only run in CI or when explicitly requested)" -ForegroundColor Yellow
    }
}

# Calculate summary
$TestResults.EndTime = Get-Date
$TestResults.Summary.Duration = $TestResults.EndTime - $TestResults.StartTime

foreach ($result in $TestResults.Results.Values) {
    $TestResults.Summary.TotalTests++
    switch ($result.Status) {
        "Passed" { $TestResults.Summary.PassedTests++ }
        "Failed" { $TestResults.Summary.FailedTests++ }
        "Error" { $TestResults.Summary.FailedTests++ }
        default { $TestResults.Summary.SkippedTests++ }
    }
}

# Generate reports if requested
if ($GenerateReports) {
    Write-Host "📊 Generating Test Reports..." -ForegroundColor Yellow
    
    # Generate summary report
    $summaryReport = @"
# eShop Test Suite Execution Summary

**Execution Date:** $($TestResults.StartTime.ToString("yyyy-MM-dd HH:mm:ss"))
**Environment:** $($TestResults.Environment)
**Test Type:** $($TestResults.TestType)
**Total Duration:** $($TestResults.Summary.Duration)

## Summary
- **Total Tests:** $($TestResults.Summary.TotalTests)
- **Passed:** $($TestResults.Summary.PassedTests)
- **Failed:** $($TestResults.Summary.FailedTests)
- **Skipped:** $($TestResults.Summary.SkippedTests)

## Detailed Results
"@

    foreach ($testType in $TestResults.Results.Keys) {
        $result = $TestResults.Results[$testType]
        $summaryReport += @"

### $testType Tests
- **Status:** $($result.Status)
- **Duration:** $($result.Duration)
"@
        if ($result.ExitCode) {
            $summaryReport += "`n- **Exit Code:** $($result.ExitCode)"
        }
        if ($result.Error) {
            $summaryReport += "`n- **Error:** $($result.Error)"
        }
    }
    
    $summaryReportPath = Join-Path $ResultsDir "test-summary.md"
    $summaryReport | Out-File -FilePath $summaryReportPath -Encoding UTF8
    
    # Generate JSON report for CI integration
    $jsonReport = $TestResults | ConvertTo-Json -Depth 10
    $jsonReportPath = Join-Path $ResultsDir "test-results.json"
    $jsonReport | Out-File -FilePath $jsonReportPath -Encoding UTF8
    
    Write-Host "✅ Reports Generated:" -ForegroundColor Green
    Write-Host "  - Summary: $summaryReportPath" -ForegroundColor Cyan
    Write-Host "  - JSON: $jsonReportPath" -ForegroundColor Cyan
}

# Display final summary
Write-Host ""
Write-Host "🏁 Test Suite Execution Complete" -ForegroundColor Green
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "  Total Tests: $($TestResults.Summary.TotalTests)" -ForegroundColor White
Write-Host "  Passed: $($TestResults.Summary.PassedTests)" -ForegroundColor Green
Write-Host "  Failed: $($TestResults.Summary.FailedTests)" -ForegroundColor Red
Write-Host "  Duration: $($TestResults.Summary.Duration)" -ForegroundColor Cyan

# Exit with appropriate code
if ($TestResults.Summary.FailedTests -gt 0) {
    Write-Host ""
    Write-Host "❌ Some tests failed. Check the detailed results above." -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "✅ All tests passed successfully!" -ForegroundColor Green
    exit 0
}