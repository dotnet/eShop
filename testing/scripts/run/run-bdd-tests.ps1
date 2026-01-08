#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs BDD tests using Reqnroll
.DESCRIPTION
    Executes BDD tests with Allure reporting and comprehensive output
.PARAMETER Feature
    Specific feature to test (optional)
.PARAMETER Tag
    Specific tag to filter tests (optional)
.PARAMETER Environment
    Target environment (Local, CI, Performance)
.PARAMETER GenerateReport
    Whether to generate Allure reports
.EXAMPLE
    ./run-bdd-tests.ps1 -Feature "CatalogManagement" -Environment Local
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Feature = "",
    
    [Parameter(Mandatory = $false)]
    [string]$Tag = "",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Local", "CI", "Performance")]
    [string]$Environment = "Local",
    
    [Parameter(Mandatory = $false)]
    [switch]$GenerateReport = $true,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild = $false
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory and paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$BddTestsDir = Join-Path $RootDir "bdd"
$ResultsDir = Join-Path $RootDir "results/bdd"

# Ensure results directory exists
if (-not (Test-Path $ResultsDir)) {
    New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
}

Write-Host "🧪 Running eShop BDD Tests" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Feature: $(if ($Feature) { $Feature } else { 'All Features' })" -ForegroundColor Cyan
Write-Host "Tag: $(if ($Tag) { $Tag } else { 'All Tags' })" -ForegroundColor Cyan
Write-Host ""

# Build solution if not skipped
if (-not $SkipBuild) {
    Write-Host "🔨 Building BDD Test Project..." -ForegroundColor Yellow
    $buildResult = dotnet build "$BddTestsDir" --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build Failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build Successful" -ForegroundColor Green
    Write-Host ""
}

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:ALLURE_RESULTS_DIRECTORY = Join-Path $ResultsDir "allure-results"

# Ensure Allure results directory exists
if (-not (Test-Path $env:ALLURE_RESULTS_DIRECTORY)) {
    New-Item -ItemType Directory -Path $env:ALLURE_RESULTS_DIRECTORY -Force | Out-Null
}

# Build test command
$testCommand = "dotnet test `"$BddTestsDir`" --configuration Release --logger trx --logger html --results-directory `"$ResultsDir`""

# Add verbosity based on environment
switch ($Environment) {
    "Local" { $testCommand += " --verbosity normal" }
    "CI" { $testCommand += " --verbosity minimal" }
    "Performance" { $testCommand += " --verbosity detailed" }
}

# Add feature filter if specified
if ($Feature) {
    $testCommand += " --filter `"Category=$Feature`""
}

# Add tag filter if specified
if ($Tag) {
    if ($Feature) {
        $testCommand += " | Category=$Tag"
    } else {
        $testCommand += " --filter `"Category=$Tag`""
    }
}

Write-Host "📋 Executing BDD Tests..." -ForegroundColor Yellow
Write-Host "Command: $testCommand" -ForegroundColor Gray
Write-Host ""

# Execute tests
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
    Invoke-Expression $testCommand
    $testExitCode = $LASTEXITCODE
    $stopwatch.Stop()
    
    if ($testExitCode -eq 0) {
        Write-Host "✅ BDD Tests Passed" -ForegroundColor Green
    } else {
        Write-Host "❌ BDD Tests Failed (Exit Code: $testExitCode)" -ForegroundColor Red
    }
}
catch {
    $stopwatch.Stop()
    Write-Host "💥 BDD Tests Error: $($_.Exception.Message)" -ForegroundColor Red
    $testExitCode = 1
}

Write-Host "⏱️  BDD Tests Duration: $($stopwatch.Elapsed)" -ForegroundColor Cyan
Write-Host ""

# Generate Allure report if requested and allure is available
if ($GenerateReport) {
    Write-Host "📊 Generating Allure Report..." -ForegroundColor Yellow
    
    # Check if Allure is installed
    $allureInstalled = $false
    try {
        $allureVersion = allure --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            $allureInstalled = $true
            Write-Host "Allure version: $allureVersion" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "Allure not found in PATH" -ForegroundColor Yellow
    }
    
    if ($allureInstalled) {
        $allureReportDir = Join-Path $ResultsDir "allure-report"
        
        try {
            allure generate "$env:ALLURE_RESULTS_DIRECTORY" -o "$allureReportDir" --clean
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Allure Report Generated: $allureReportDir" -ForegroundColor Green
                
                # Try to open report in browser (local environment only)
                if ($Environment -eq "Local") {
                    $indexPath = Join-Path $allureReportDir "index.html"
                    if (Test-Path $indexPath) {
                        Write-Host "🌐 Opening Allure Report in browser..." -ForegroundColor Cyan
                        Start-Process $indexPath
                    }
                }
            } else {
                Write-Host "⚠️  Allure Report Generation Failed" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "⚠️  Allure Report Generation Error: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  Allure not installed. Install with: npm install -g allure-commandline" -ForegroundColor Yellow
    }
}

# Generate summary report
Write-Host "📋 Generating Test Summary..." -ForegroundColor Yellow

$summaryReport = @"
# BDD Test Execution Summary

**Execution Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Environment:** $Environment
**Feature Filter:** $(if ($Feature) { $Feature } else { "All Features" })
**Tag Filter:** $(if ($Tag) { $Tag } else { "All Tags" })
**Duration:** $($stopwatch.Elapsed)
**Exit Code:** $testExitCode

## Test Results
- **Status:** $(if ($testExitCode -eq 0) { "✅ PASSED" } else { "❌ FAILED" })
- **Results Directory:** $ResultsDir
- **Allure Results:** $env:ALLURE_RESULTS_DIRECTORY

## Files Generated
- TRX Report: Available in $ResultsDir
- HTML Report: Available in $ResultsDir
$(if ($GenerateReport -and $allureInstalled) { "- Allure Report: Available in $ResultsDir/allure-report" } else { "" })

## Command Executed
```
$testCommand
```

## Next Steps
$(if ($testExitCode -ne 0) { 
"- Review test failures in the generated reports
- Check Allure report for detailed scenario results
- Verify test data and environment setup" 
} else { 
"- Review test results in Allure report
- All BDD scenarios passed successfully" 
})
"@

$summaryPath = Join-Path $ResultsDir "bdd-test-summary.md"
$summaryReport | Out-File -FilePath $summaryPath -Encoding UTF8

Write-Host "✅ Summary Report Generated: $summaryPath" -ForegroundColor Green

# Display final results
Write-Host ""
Write-Host "🏁 BDD Test Execution Complete" -ForegroundColor Green
Write-Host "📊 Results:" -ForegroundColor Cyan
Write-Host "  Status: $(if ($testExitCode -eq 0) { "PASSED" } else { "FAILED" })" -ForegroundColor $(if ($testExitCode -eq 0) { "Green" } else { "Red" })
Write-Host "  Duration: $($stopwatch.Elapsed)" -ForegroundColor Cyan
Write-Host "  Results: $ResultsDir" -ForegroundColor Cyan

if ($GenerateReport -and $allureInstalled) {
    Write-Host "  Allure Report: $ResultsDir/allure-report/index.html" -ForegroundColor Cyan
}

# Exit with test result code
exit $testExitCode