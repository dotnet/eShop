#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs unit tests for the eShop application
.DESCRIPTION
    Executes unit tests with coverage analysis and generates detailed reports
.PARAMETER Configuration
    Build configuration (Debug, Release)
.PARAMETER Coverage
    Generate code coverage reports
.PARAMETER Filter
    Test filter expression
.EXAMPLE
    ./run-unit-tests.ps1 -Configuration Release -Coverage -Filter "Category=Unit"
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory = $false)]
    [switch]$Coverage = $true,
    
    [Parameter(Mandatory = $false)]
    [string]$Filter = "",
    
    [Parameter(Mandatory = $false)]
    [switch]$Parallel = $true,
    
    [Parameter(Mandatory = $false)]
    [string]$Logger = "trx;html"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory and paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$UnitTestsDir = Join-Path $RootDir "testing/unit"
$ResultsDir = Join-Path $RootDir "results/unit"
$CoverageDir = Join-Path $RootDir "results/coverage"

# Ensure directories exist
@($ResultsDir, $CoverageDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

Write-Host "🧪 Running eShop Unit Tests" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Coverage: $Coverage" -ForegroundColor Cyan
Write-Host "Parallel: $Parallel" -ForegroundColor Cyan
Write-Host "Filter: $Filter" -ForegroundColor Cyan
Write-Host ""

# Build the test project
Write-Host "🔨 Building Unit Test Project..." -ForegroundColor Yellow
$buildCommand = "dotnet build `"$UnitTestsDir`" --configuration $Configuration --verbosity minimal"
Invoke-Expression $buildCommand

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Prepare test command
$testCommand = "dotnet test `"$UnitTestsDir`" --configuration $Configuration --no-build"

# Add logger
if ($Logger) {
    $testCommand += " --logger `"$Logger`""
}

# Add results directory
$testCommand += " --results-directory `"$ResultsDir`""

# Add filter if specified
if ($Filter) {
    $testCommand += " --filter `"$Filter`""
}

# Add parallel execution
if ($Parallel) {
    $testCommand += " --parallel"
}

# Add coverage collection
if ($Coverage) {
    $testCommand += " --collect:`"XPlat Code Coverage`""
    $testCommand += " --settings `"$RootDir/testing/config/coverage-config.json`""
}

# Add verbosity
$testCommand += " --verbosity normal"

# Execute tests
Write-Host "🏃 Executing Unit Tests..." -ForegroundColor Yellow
Write-Host "Command: $testCommand" -ForegroundColor Gray
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
Invoke-Expression $testCommand
$testExitCode = $LASTEXITCODE
$stopwatch.Stop()

Write-Host ""
Write-Host "⏱️  Test execution time: $($stopwatch.Elapsed)" -ForegroundColor Cyan

# Process coverage results if enabled
if ($Coverage -and $testExitCode -eq 0) {
    Write-Host ""
    Write-Host "📊 Processing Coverage Results..." -ForegroundColor Yellow
    
    # Find coverage files
    $coverageFiles = Get-ChildItem -Path $ResultsDir -Filter "coverage.cobertura.xml" -Recurse
    
    if ($coverageFiles.Count -gt 0) {
        # Generate HTML coverage report using ReportGenerator
        $reportGeneratorCommand = "dotnet tool run reportgenerator -reports:`"$($coverageFiles[0].FullName)`" -targetdir:`"$CoverageDir`" -reporttypes:Html;JsonSummary;Badges"
        
        try {
            Invoke-Expression $reportGeneratorCommand
            Write-Host "✅ Coverage report generated: $CoverageDir/index.html" -ForegroundColor Green
            
            # Display coverage summary
            $summaryFile = Join-Path $CoverageDir "Summary.json"
            if (Test-Path $summaryFile) {
                $summary = Get-Content $summaryFile | ConvertFrom-Json
                Write-Host ""
                Write-Host "📈 Coverage Summary:" -ForegroundColor Cyan
                Write-Host "  Line Coverage: $($summary.summary.linecoverage)%" -ForegroundColor White
                Write-Host "  Branch Coverage: $($summary.summary.branchcoverage)%" -ForegroundColor White
                Write-Host "  Method Coverage: $($summary.summary.methodcoverage)%" -ForegroundColor White
            }
        }
        catch {
            Write-Host "⚠️  Could not generate HTML coverage report: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  No coverage files found" -ForegroundColor Yellow
    }
}

# Display results summary
Write-Host ""
if ($testExitCode -eq 0) {
    Write-Host "✅ Unit Tests Passed" -ForegroundColor Green
} else {
    Write-Host "❌ Unit Tests Failed (Exit Code: $testExitCode)" -ForegroundColor Red
}

# List generated files
Write-Host ""
Write-Host "📁 Generated Files:" -ForegroundColor Cyan
Get-ChildItem -Path $ResultsDir -Recurse -File | ForEach-Object {
    Write-Host "  $($_.FullName)" -ForegroundColor Gray
}

if ($Coverage -and (Test-Path $CoverageDir)) {
    Get-ChildItem -Path $CoverageDir -Recurse -File -Include "*.html", "*.json" | ForEach-Object {
        Write-Host "  $($_.FullName)" -ForegroundColor Gray
    }
}

exit $testExitCode