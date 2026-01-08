#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs End-to-End tests using Playwright
.DESCRIPTION
    Executes E2E tests with comprehensive reporting and browser automation
.PARAMETER Browser
    Browser to use for testing (chromium, firefox, webkit, all)
.PARAMETER Headed
    Run tests in headed mode (visible browser)
.PARAMETER Environment
    Target environment (Local, CI, Performance)
.PARAMETER BaseUrl
    Base URL for the application under test
.PARAMETER Workers
    Number of parallel workers
.EXAMPLE
    ./run-e2e-tests.ps1 -Browser chromium -Environment Local -BaseUrl "https://localhost:5001"
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("chromium", "firefox", "webkit", "all")]
    [string]$Browser = "chromium",
    
    [Parameter(Mandatory = $false)]
    [switch]$Headed = $false,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Local", "CI", "Performance")]
    [string]$Environment = "Local",
    
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "https://localhost:5001",
    
    [Parameter(Mandatory = $false)]
    [int]$Workers = 1,
    
    [Parameter(Mandatory = $false)]
    [switch]$UpdateSnapshots = $false,
    
    [Parameter(Mandatory = $false)]
    [string]$TestPattern = ""
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory and paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$E2eTestsDir = Join-Path $RootDir "e2e"
$ResultsDir = Join-Path $RootDir "results/e2e"

# Ensure results directory exists
if (-not (Test-Path $ResultsDir)) {
    New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
}

Write-Host "🎭 Running eShop E2E Tests" -ForegroundColor Green
Write-Host "Browser: $Browser" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "Workers: $Workers" -ForegroundColor Cyan
Write-Host "Headed Mode: $Headed" -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is installed
try {
    $nodeVersion = node --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Node.js version: $nodeVersion" -ForegroundColor Gray
    } else {
        throw "Node.js not found"
    }
}
catch {
    Write-Host "❌ Node.js is required but not installed" -ForegroundColor Red
    Write-Host "Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Navigate to E2E tests directory
Push-Location $E2eTestsDir
try {
    # Check if package.json exists
    if (-not (Test-Path "package.json")) {
        Write-Host "❌ package.json not found in E2E tests directory" -ForegroundColor Red
        exit 1
    }

    # Install dependencies if node_modules doesn't exist
    if (-not (Test-Path "node_modules")) {
        Write-Host "📦 Installing E2E test dependencies..." -ForegroundColor Yellow
        npm ci
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to install dependencies" -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ Dependencies installed" -ForegroundColor Green
    }

    # Install Playwright browsers if needed
    Write-Host "🎭 Ensuring Playwright browsers are installed..." -ForegroundColor Yellow
    npx playwright install --with-deps
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  Playwright browser installation had issues, continuing..." -ForegroundColor Yellow
    } else {
        Write-Host "✅ Playwright browsers ready" -ForegroundColor Green
    }

    # Set environment variables
    $env:BASE_URL = $BaseUrl
    $env:CI = if ($Environment -eq "CI") { "true" } else { "false" }
    $env:PWTEST_HTML_REPORT_OPEN = if ($Environment -eq "Local") { "on-failure" } else { "never" }

    # Build Playwright command
    $playwrightCommand = "npx playwright test"
    
    # Add browser selection
    if ($Browser -ne "all") {
        $playwrightCommand += " --project=$Browser"
    }
    
    # Add headed mode
    if ($Headed) {
        $playwrightCommand += " --headed"
    }
    
    # Add workers
    $playwrightCommand += " --workers=$Workers"
    
    # Add test pattern if specified
    if ($TestPattern) {
        $playwrightCommand += " --grep `"$TestPattern`""
    }
    
    # Add update snapshots if requested
    if ($UpdateSnapshots) {
        $playwrightCommand += " --update-snapshots"
    }
    
    # Add reporters
    $playwrightCommand += " --reporter=html,json,junit"
    
    # Add output directory
    $playwrightCommand += " --output-dir=`"$ResultsDir`""

    Write-Host "🚀 Executing E2E Tests..." -ForegroundColor Yellow
    Write-Host "Command: $playwrightCommand" -ForegroundColor Gray
    Write-Host ""

    # Execute tests
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        Invoke-Expression $playwrightCommand
        $testExitCode = $LASTEXITCODE
        $stopwatch.Stop()
        
        if ($testExitCode -eq 0) {
            Write-Host "✅ E2E Tests Passed" -ForegroundColor Green
        } else {
            Write-Host "❌ E2E Tests Failed (Exit Code: $testExitCode)" -ForegroundColor Red
        }
    }
    catch {
        $stopwatch.Stop()
        Write-Host "💥 E2E Tests Error: $($_.Exception.Message)" -ForegroundColor Red
        $testExitCode = 1
    }

    Write-Host "⏱️  E2E Tests Duration: $($stopwatch.Elapsed)" -ForegroundColor Cyan
    Write-Host ""

    # Copy reports to results directory
    if (Test-Path "playwright-report") {
        Write-Host "📊 Copying Playwright HTML report..." -ForegroundColor Yellow
        $htmlReportDest = Join-Path $ResultsDir "playwright-report"
        if (Test-Path $htmlReportDest) {
            Remove-Item -Recurse -Force $htmlReportDest
        }
        Copy-Item -Recurse "playwright-report" $htmlReportDest
        Write-Host "✅ HTML Report: $htmlReportDest/index.html" -ForegroundColor Green
    }

    if (Test-Path "test-results") {
        Write-Host "📊 Copying test results..." -ForegroundColor Yellow
        $testResultsDest = Join-Path $ResultsDir "test-results"
        if (Test-Path $testResultsDest) {
            Remove-Item -Recurse -Force $testResultsDest
        }
        Copy-Item -Recurse "test-results" $testResultsDest
        Write-Host "✅ Test Results: $testResultsDest" -ForegroundColor Green
    }

    # Generate summary report
    Write-Host "📋 Generating Test Summary..." -ForegroundColor Yellow

    $summaryReport = @"
# E2E Test Execution Summary

**Execution Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Environment:** $Environment
**Browser:** $Browser
**Base URL:** $BaseUrl
**Workers:** $Workers
**Headed Mode:** $Headed
**Duration:** $($stopwatch.Elapsed)
**Exit Code:** $testExitCode

## Test Results
- **Status:** $(if ($testExitCode -eq 0) { "✅ PASSED" } else { "❌ FAILED" })
- **Results Directory:** $ResultsDir
- **HTML Report:** $ResultsDir/playwright-report/index.html

## Test Configuration
- **Test Pattern:** $(if ($TestPattern) { $TestPattern } else { "All Tests" })
- **Update Snapshots:** $UpdateSnapshots
- **Parallel Workers:** $Workers

## Command Executed
```
$playwrightCommand
```

## Browser Coverage
$(if ($Browser -eq "all") { 
"- Chromium: ✅
- Firefox: ✅  
- WebKit: ✅" 
} else { 
"- $Browser: ✅" 
})

## Next Steps
$(if ($testExitCode -ne 0) { 
"- Review test failures in the HTML report
- Check screenshots and videos for failed tests
- Verify application is running at $BaseUrl
- Check browser compatibility issues" 
} else { 
"- Review test results in HTML report
- All E2E scenarios passed successfully
- Consider running on additional browsers" 
})
"@

    $summaryPath = Join-Path $ResultsDir "e2e-test-summary.md"
    $summaryReport | Out-File -FilePath $summaryPath -Encoding UTF8

    Write-Host "✅ Summary Report Generated: $summaryPath" -ForegroundColor Green

    # Open HTML report in local environment
    if ($Environment -eq "Local" -and $testExitCode -ne 0) {
        $htmlReportPath = Join-Path $ResultsDir "playwright-report/index.html"
        if (Test-Path $htmlReportPath) {
            Write-Host "🌐 Opening HTML Report in browser..." -ForegroundColor Cyan
            Start-Process $htmlReportPath
        }
    }

    # Display final results
    Write-Host ""
    Write-Host "🏁 E2E Test Execution Complete" -ForegroundColor Green
    Write-Host "📊 Results:" -ForegroundColor Cyan
    Write-Host "  Status: $(if ($testExitCode -eq 0) { "PASSED" } else { "FAILED" })" -ForegroundColor $(if ($testExitCode -eq 0) { "Green" } else { "Red" })
    Write-Host "  Duration: $($stopwatch.Elapsed)" -ForegroundColor Cyan
    Write-Host "  Browser: $Browser" -ForegroundColor Cyan
    Write-Host "  Results: $ResultsDir" -ForegroundColor Cyan
    Write-Host "  HTML Report: $ResultsDir/playwright-report/index.html" -ForegroundColor Cyan

    # Exit with test result code
    exit $testExitCode
}
finally {
    Pop-Location
}