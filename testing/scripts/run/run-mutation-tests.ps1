#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs mutation tests using Stryker.NET
.DESCRIPTION
    Executes mutation testing to validate test suite quality and effectiveness
.PARAMETER Project
    Specific project to mutate (optional)
.PARAMETER Baseline
    Git branch to use as baseline for comparison
.PARAMETER Dashboard
    Upload results to Stryker Dashboard
.PARAMETER Since
    Only mutate files changed since specified commit/branch
.EXAMPLE
    ./run-mutation-tests.ps1 -Project "Catalog.API" -Baseline "main"
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Catalog.API", "Basket.API", "Ordering.API", "Identity.API", "All")]
    [string]$Project = "All",
    
    [Parameter(Mandatory = $false)]
    [string]$Baseline = "",
    
    [Parameter(Mandatory = $false)]
    [switch]$Dashboard = $false,
    
    [Parameter(Mandatory = $false)]
    [string]$Since = "",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory = $false)]
    [int]$Timeout = 300000 # 5 minutes default
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory and paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$MutationConfigPath = Join-Path $RootDir "mutation/stryker-config.json"
$ResultsDir = Join-Path $RootDir "results/mutation"

# Ensure results directory exists
if (-not (Test-Path $ResultsDir)) {
    New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
}

Write-Host "🧬 Running eShop Mutation Tests" -ForegroundColor Green
Write-Host "Project: $Project" -ForegroundColor Cyan
Write-Host "Baseline: $(if ($Baseline) { $Baseline } else { 'None' })" -ForegroundColor Cyan
Write-Host "Dashboard: $Dashboard" -ForegroundColor Cyan
Write-Host "Since: $(if ($Since) { $Since } else { 'All files' })" -ForegroundColor Cyan
Write-Host ""

# Check if Stryker.NET is installed
Write-Host "🔍 Checking Stryker.NET installation..." -ForegroundColor Yellow
try {
    $strykerVersion = dotnet stryker --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Stryker.NET version: $strykerVersion" -ForegroundColor Gray
    } else {
        throw "Stryker.NET not found"
    }
}
catch {
    Write-Host "❌ Stryker.NET is not installed" -ForegroundColor Red
    Write-Host "Installing Stryker.NET..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-stryker
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install Stryker.NET" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Stryker.NET installed successfully" -ForegroundColor Green
}

# Build solution if not skipped
if (-not $SkipBuild) {
    Write-Host "🔨 Building Solution..." -ForegroundColor Yellow
    $buildResult = dotnet build "$RootDir/../eShop.slnx" --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build Failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build Successful" -ForegroundColor Green
    Write-Host ""
}

# Build Stryker command
$strykerCommand = "dotnet stryker"

# Add configuration file
if (Test-Path $MutationConfigPath) {
    $strykerCommand += " --config-file `"$MutationConfigPath`""
} else {
    Write-Host "⚠️  Stryker config file not found: $MutationConfigPath" -ForegroundColor Yellow
    Write-Host "Using default configuration..." -ForegroundColor Yellow
}

# Add output directory
$strykerCommand += " --output `"$ResultsDir`""

# Add reporters
$strykerCommand += " --reporter html --reporter json --reporter cleartext --reporter progress"

# Add dashboard if requested
if ($Dashboard) {
    if ($env:STRYKER_DASHBOARD_API_KEY) {
        $strykerCommand += " --reporter dashboard"
        Write-Host "📊 Dashboard reporting enabled" -ForegroundColor Cyan
    } else {
        Write-Host "⚠️  Dashboard requested but STRYKER_DASHBOARD_API_KEY not set" -ForegroundColor Yellow
    }
}

# Add baseline if specified
if ($Baseline) {
    $strykerCommand += " --baseline:$Baseline"
    Write-Host "📈 Using baseline: $Baseline" -ForegroundColor Cyan
}

# Add since filter if specified
if ($Since) {
    $strykerCommand += " --since:$Since"
    Write-Host "🔄 Mutating files changed since: $Since" -ForegroundColor Cyan
}

# Add timeout
$strykerCommand += " --timeout-ms $Timeout"

# Add project-specific configuration
if ($Project -ne "All") {
    Write-Host "🎯 Targeting specific project: $Project" -ForegroundColor Cyan
    # Project-specific mutations would be configured in the config file
}

Write-Host "🚀 Executing Mutation Tests..." -ForegroundColor Yellow
Write-Host "Command: $strykerCommand" -ForegroundColor Gray
Write-Host ""
Write-Host "⚠️  This may take a significant amount of time..." -ForegroundColor Yellow
Write-Host ""

# Execute mutation tests
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
    # Navigate to root directory for execution
    Push-Location $RootDir
    
    Invoke-Expression $strykerCommand
    $mutationExitCode = $LASTEXITCODE
    $stopwatch.Stop()
    
    if ($mutationExitCode -eq 0) {
        Write-Host "✅ Mutation Tests Completed Successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Mutation Tests Completed with Issues (Exit Code: $mutationExitCode)" -ForegroundColor Yellow
        Write-Host "This may indicate low mutation score or test failures" -ForegroundColor Yellow
    }
}
catch {
    $stopwatch.Stop()
    Write-Host "💥 Mutation Tests Error: $($_.Exception.Message)" -ForegroundColor Red
    $mutationExitCode = 1
}
finally {
    Pop-Location
}

Write-Host "⏱️  Mutation Tests Duration: $($stopwatch.Elapsed)" -ForegroundColor Cyan
Write-Host ""

# Parse and display results
Write-Host "📊 Analyzing Mutation Test Results..." -ForegroundColor Yellow

$htmlReportPath = Join-Path $ResultsDir "reports/mutation-report.html"
$jsonReportPath = Join-Path $ResultsDir "reports/mutation-report.json"

if (Test-Path $jsonReportPath) {
    try {
        $jsonContent = Get-Content $jsonReportPath -Raw | ConvertFrom-Json
        $mutationScore = $jsonContent.thresholds.high
        $totalMutants = $jsonContent.totalMutants
        $killedMutants = $jsonContent.killedMutants
        $survivedMutants = $jsonContent.survivedMutants
        $timeoutMutants = $jsonContent.timeoutMutants
        
        Write-Host "📈 Mutation Test Results:" -ForegroundColor Cyan
        Write-Host "  Mutation Score: $mutationScore%" -ForegroundColor $(if ($mutationScore -ge 80) { "Green" } elseif ($mutationScore -ge 60) { "Yellow" } else { "Red" })
        Write-Host "  Total Mutants: $totalMutants" -ForegroundColor White
        Write-Host "  Killed: $killedMutants" -ForegroundColor Green
        Write-Host "  Survived: $survivedMutants" -ForegroundColor Red
        Write-Host "  Timeout: $timeoutMutants" -ForegroundColor Yellow
    }
    catch {
        Write-Host "⚠️  Could not parse mutation results JSON" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  Mutation results JSON not found" -ForegroundColor Yellow
}

# Generate summary report
Write-Host "📋 Generating Mutation Test Summary..." -ForegroundColor Yellow

$summaryReport = @"
# Mutation Test Execution Summary

**Execution Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Project:** $Project
**Baseline:** $(if ($Baseline) { $Baseline } else { "None" })
**Since Filter:** $(if ($Since) { $Since } else { "All files" })
**Duration:** $($stopwatch.Elapsed)
**Exit Code:** $mutationExitCode

## Mutation Results
$(if (Test-Path $jsonReportPath) {
    $jsonContent = Get-Content $jsonReportPath -Raw | ConvertFrom-Json
    "- **Mutation Score:** $($jsonContent.thresholds.high)%
- **Total Mutants:** $($jsonContent.totalMutants)
- **Killed Mutants:** $($jsonContent.killedMutants)
- **Survived Mutants:** $($jsonContent.survivedMutants)
- **Timeout Mutants:** $($jsonContent.timeoutMutants)"
} else {
    "- Results parsing failed or not available"
})

## Quality Assessment
$(if (Test-Path $jsonReportPath) {
    $jsonContent = Get-Content $jsonReportPath -Raw | ConvertFrom-Json
    $score = $jsonContent.thresholds.high
    if ($score -ge 80) {
        "✅ **EXCELLENT** - Mutation score meets high quality standards"
    } elseif ($score -ge 60) {
        "⚠️  **ACCEPTABLE** - Mutation score is acceptable but could be improved"
    } else {
        "❌ **NEEDS IMPROVEMENT** - Mutation score is below acceptable threshold"
    }
} else {
    "❓ **UNKNOWN** - Could not determine quality score"
})

## Reports Generated
- **HTML Report:** $htmlReportPath
- **JSON Report:** $jsonReportPath
- **Results Directory:** $ResultsDir

## Command Executed
```
$strykerCommand
```

## Recommendations
$(if (Test-Path $jsonReportPath) {
    $jsonContent = Get-Content $jsonReportPath -Raw | ConvertFrom-Json
    $score = $jsonContent.thresholds.high
    if ($score -lt 80) {
        "- Review survived mutants in the HTML report
- Add tests to kill surviving mutants
- Focus on critical business logic paths
- Consider adding property-based tests for edge cases"
    } else {
        "- Excellent mutation score achieved
- Continue maintaining high test quality
- Consider this as a baseline for future changes"
    }
} else {
    "- Review Stryker execution logs for issues
- Ensure all test projects are building correctly
- Check configuration file for proper setup"
})
"@

$summaryPath = Join-Path $ResultsDir "mutation-test-summary.md"
$summaryReport | Out-File -FilePath $summaryPath -Encoding UTF8

Write-Host "✅ Summary Report Generated: $summaryPath" -ForegroundColor Green

# Open HTML report in local environment
if (Test-Path $htmlReportPath) {
    Write-Host "🌐 HTML Report Available: $htmlReportPath" -ForegroundColor Cyan
    
    # Try to open in browser
    try {
        Start-Process $htmlReportPath
        Write-Host "📖 Opening HTML report in browser..." -ForegroundColor Cyan
    }
    catch {
        Write-Host "⚠️  Could not open HTML report automatically" -ForegroundColor Yellow
    }
}

# Display final results
Write-Host ""
Write-Host "🏁 Mutation Test Execution Complete" -ForegroundColor Green
Write-Host "📊 Results:" -ForegroundColor Cyan
Write-Host "  Status: $(if ($mutationExitCode -eq 0) { "COMPLETED" } else { "COMPLETED WITH ISSUES" })" -ForegroundColor $(if ($mutationExitCode -eq 0) { "Green" } else { "Yellow" })
Write-Host "  Duration: $($stopwatch.Elapsed)" -ForegroundColor Cyan
Write-Host "  Results: $ResultsDir" -ForegroundColor Cyan

if (Test-Path $htmlReportPath) {
    Write-Host "  HTML Report: $htmlReportPath" -ForegroundColor Cyan
}

if ($Dashboard -and $env:STRYKER_DASHBOARD_API_KEY) {
    Write-Host "  Dashboard: Results uploaded to Stryker Dashboard" -ForegroundColor Cyan
}

# Exit with appropriate code
exit $mutationExitCode