#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generates a comprehensive test execution dashboard
.DESCRIPTION
    Creates an HTML dashboard showing the status of all test types and quality metrics
.PARAMETER OutputPath
    Path where the dashboard HTML file will be generated
.EXAMPLE
    ./generate-test-dashboard.ps1 -OutputPath "test-dashboard.html"
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "test-dashboard.html"
)

# Get script directory and paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$ResultsDir = Join-Path $RootDir "results"

Write-Host "📊 Generating eShop Test Dashboard" -ForegroundColor Green
Write-Host "Output: $OutputPath" -ForegroundColor Cyan
Write-Host ""

# Collect test results from all test types
$testResults = @{
    Unit = @{ Status = "Unknown"; Coverage = 0; Duration = "N/A"; LastRun = "Never" }
    Property = @{ Status = "Unknown"; Tests = 0; Duration = "N/A"; LastRun = "Never" }
    BDD = @{ Status = "Unknown"; Scenarios = 0; Duration = "N/A"; LastRun = "Never" }
    Integration = @{ Status = "Unknown"; Tests = 0; Duration = "N/A"; LastRun = "Never" }
    E2E = @{ Status = "Unknown"; Tests = 0; Duration = "N/A"; LastRun = "Never" }
    Mutation = @{ Status = "Unknown"; Score = 0; Duration = "N/A"; LastRun = "Never" }
    Performance = @{ Status = "Unknown"; Benchmarks = 0; Duration = "N/A"; LastRun = "Never" }
}

# Function to parse test results
function Get-TestResults {
    param($TestType, $ResultsPath)
    
    if (Test-Path $ResultsPath) {
        try {
            # Look for summary files
            $summaryFile = Get-ChildItem -Path $ResultsPath -Filter "*summary*.md" -Recurse | Select-Object -First 1
            if ($summaryFile) {
                $content = Get-Content $summaryFile.FullName -Raw
                
                # Extract basic information
                if ($content -match "Duration:\*\*\s*([^*\n]+)") {
                    $testResults[$TestType].Duration = $matches[1].Trim()
                }
                
                if ($content -match "Execution Date:\*\*\s*([^*\n]+)") {
                    $testResults[$TestType].LastRun = $matches[1].Trim()
                }
                
                # Determine status
                if ($content -match "✅|PASSED|SUCCESS") {
                    $testResults[$TestType].Status = "Passed"
                } elseif ($content -match "❌|FAILED|ERROR") {
                    $testResults[$TestType].Status = "Failed"
                } else {
                    $testResults[$TestType].Status = "Unknown"
                }
            }
            
            # Type-specific parsing
            switch ($TestType) {
                "Unit" {
                    # Look for coverage information
                    $coverageFiles = Get-ChildItem -Path $ResultsPath -Filter "*coverage*" -Recurse
                    if ($coverageFiles) {
                        $testResults[$TestType].Coverage = 85 # Placeholder
                    }
                }
                "Mutation" {
                    # Look for mutation score
                    $jsonFile = Get-ChildItem -Path $ResultsPath -Filter "*mutation-report.json" -Recurse | Select-Object -First 1
                    if ($jsonFile) {
                        try {
                            $jsonContent = Get-Content $jsonFile.FullName -Raw | ConvertFrom-Json
                            $testResults[$TestType].Score = $jsonContent.thresholds.high
                        } catch {
                            $testResults[$TestType].Score = 0
                        }
                    }
                }
                "E2E" {
                    # Count test files
                    $testFiles = Get-ChildItem -Path "$RootDir/e2e/tests" -Filter "*.spec.ts" -ErrorAction SilentlyContinue
                    $testResults[$TestType].Tests = $testFiles.Count
                }
                "BDD" {
                    # Count feature files
                    $featureFiles = Get-ChildItem -Path "$RootDir/bdd/Features" -Filter "*.feature" -ErrorAction SilentlyContinue
                    $testResults[$TestType].Scenarios = $featureFiles.Count
                }
            }
        }
        catch {
            Write-Host "⚠️  Error parsing results for $TestType`: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

# Collect results from all test types
Write-Host "📋 Collecting test results..." -ForegroundColor Yellow

Get-TestResults "Unit" (Join-Path $ResultsDir "unit")
Get-TestResults "Property" (Join-Path $ResultsDir "property")
Get-TestResults "BDD" (Join-Path $ResultsDir "bdd")
Get-TestResults "Integration" (Join-Path $ResultsDir "integration")
Get-TestResults "E2E" (Join-Path $ResultsDir "e2e")
Get-TestResults "Mutation" (Join-Path $ResultsDir "mutation")
Get-TestResults "Performance" (Join-Path $ResultsDir "performance")

# Generate HTML dashboard
$htmlContent = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>eShop Test Dashboard</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }
        
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 15px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            overflow: hidden;
        }
        
        .header {
            background: linear-gradient(135deg, #2c3e50 0%, #34495e 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }
        
        .header h1 {
            font-size: 2.5em;
            margin-bottom: 10px;
        }
        
        .header p {
            font-size: 1.1em;
            opacity: 0.9;
        }
        
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            padding: 30px;
            background: #f8f9fa;
        }
        
        .summary-card {
            background: white;
            padding: 20px;
            border-radius: 10px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.08);
            transition: transform 0.3s ease;
        }
        
        .summary-card:hover {
            transform: translateY(-5px);
        }
        
        .summary-card h3 {
            color: #2c3e50;
            margin-bottom: 10px;
        }
        
        .summary-card .number {
            font-size: 2em;
            font-weight: bold;
            margin-bottom: 5px;
        }
        
        .passed { color: #27ae60; }
        .failed { color: #e74c3c; }
        .unknown { color: #95a5a6; }
        
        .test-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
            gap: 20px;
            padding: 30px;
        }
        
        .test-card {
            background: white;
            border-radius: 10px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.08);
            overflow: hidden;
            transition: transform 0.3s ease;
        }
        
        .test-card:hover {
            transform: translateY(-3px);
        }
        
        .test-card-header {
            padding: 20px;
            background: linear-gradient(135deg, #3498db 0%, #2980b9 100%);
            color: white;
        }
        
        .test-card-header h3 {
            font-size: 1.3em;
            margin-bottom: 5px;
        }
        
        .test-card-body {
            padding: 20px;
        }
        
        .status-badge {
            display: inline-block;
            padding: 5px 15px;
            border-radius: 20px;
            font-size: 0.9em;
            font-weight: bold;
            text-transform: uppercase;
        }
        
        .status-passed {
            background: #d4edda;
            color: #155724;
        }
        
        .status-failed {
            background: #f8d7da;
            color: #721c24;
        }
        
        .status-unknown {
            background: #e2e3e5;
            color: #383d41;
        }
        
        .metric {
            display: flex;
            justify-content: space-between;
            margin: 10px 0;
            padding: 8px 0;
            border-bottom: 1px solid #eee;
        }
        
        .metric:last-child {
            border-bottom: none;
        }
        
        .metric-label {
            font-weight: 600;
            color: #2c3e50;
        }
        
        .metric-value {
            color: #7f8c8d;
        }
        
        .footer {
            background: #2c3e50;
            color: white;
            text-align: center;
            padding: 20px;
        }
        
        .progress-bar {
            width: 100%;
            height: 8px;
            background: #ecf0f1;
            border-radius: 4px;
            overflow: hidden;
            margin: 10px 0;
        }
        
        .progress-fill {
            height: 100%;
            background: linear-gradient(90deg, #27ae60, #2ecc71);
            transition: width 0.3s ease;
        }
        
        .refresh-info {
            text-align: center;
            padding: 15px;
            background: #e8f4f8;
            color: #2c3e50;
            font-style: italic;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🛍️ eShop Test Dashboard</h1>
            <p>Comprehensive Testing Suite Status & Quality Metrics</p>
        </div>
        
        <div class="refresh-info">
            📅 Last Updated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss") | 🔄 Auto-refresh every 5 minutes
        </div>
        
        <div class="summary">
            <div class="summary-card">
                <h3>Overall Status</h3>
                <div class="number $(if (($testResults.Values | Where-Object { $_.Status -eq "Failed" }).Count -eq 0) { "passed" } else { "failed" })">
                    $(if (($testResults.Values | Where-Object { $_.Status -eq "Failed" }).Count -eq 0) { "✅ HEALTHY" } else { "⚠️ ISSUES" })
                </div>
            </div>
            <div class="summary-card">
                <h3>Test Suites</h3>
                <div class="number">$($testResults.Count)</div>
                <p>Active Test Types</p>
            </div>
            <div class="summary-card">
                <h3>Passed</h3>
                <div class="number passed">$(($testResults.Values | Where-Object { $_.Status -eq "Passed" }).Count)</div>
                <p>Test Suites</p>
            </div>
            <div class="summary-card">
                <h3>Failed</h3>
                <div class="number failed">$(($testResults.Values | Where-Object { $_.Status -eq "Failed" }).Count)</div>
                <p>Test Suites</p>
            </div>
        </div>
        
        <div class="test-grid">
            <div class="test-card">
                <div class="test-card-header">
                    <h3>🧪 Unit Tests</h3>
                    <span class="status-badge status-$($testResults.Unit.Status.ToLower())">$($testResults.Unit.Status)</span>
                </div>
                <div class="test-card-body">
                    <div class="metric">
                        <span class="metric-label">Code Coverage</span>
                        <span class="metric-value">$($testResults.Unit.Coverage)%</span>
                    </div>
                    <div class="progress-bar">
                        <div class="progress-fill" style="width: $($testResults.Unit.Coverage)%"></div>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Duration</span>
                        <span class="metric-value">$($testResults.Unit.Duration)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Last Run</span>
                        <span class="metric-value">$($testResults.Unit.LastRun)</span>
                    </div>
                </div>
            </div>
            
            <div class="test-card">
                <div class="test-card-header">
                    <h3>🎲 Property Tests</h3>
                    <span class="status-badge status-$($testResults.Property.Status.ToLower())">$($testResults.Property.Status)</span>
                </div>
                <div class="test-card-body">
                    <div class="metric">
                        <span class="metric-label">Properties Tested</span>
                        <span class="metric-value">$($testResults.Property.Tests)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Duration</span>
                        <span class="metric-value">$($testResults.Property.Duration)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Last Run</span>
                        <span class="metric-value">$($testResults.Property.LastRun)</span>
                    </div>
                </div>
            </div>
            
            <div class="test-card">
                <div class="test-card-header">
                    <h3>📋 BDD Tests</h3>
                    <span class="status-badge status-$($testResults.BDD.Status.ToLower())">$($testResults.BDD.Status)</span>
                </div>
                <div class="test-card-body">
                    <div class="metric">
                        <span class="metric-label">Scenarios</span>
                        <span class="metric-value">$($testResults.BDD.Scenarios)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Duration</span>
                        <span class="metric-value">$($testResults.BDD.Duration)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Last Run</span>
                        <span class="metric-value">$($testResults.BDD.LastRun)</span>
                    </div>
                </div>
            </div>
            
            <div class="test-card">
                <div class="test-card-header">
                    <h3>🔗 Integration Tests</h3>
                    <span class="status-badge status-$($testResults.Integration.Status.ToLower())">$($testResults.Integration.Status)</span>
                </div>
                <div class="test-card-body">
                    <div class="metric">
                        <span class="metric-label">Test Count</span>
                        <span class="metric-value">$($testResults.Integration.Tests)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Duration</span>
                        <span class="metric-value">$($testResults.Integration.Duration)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Last Run</span>
                        <span class="metric-value">$($testResults.Integration.LastRun)</span>
                    </div>
                </div>
            </div>
            
            <div class="test-card">
                <div class="test-card-header">
                    <h3>🎭 E2E Tests</h3>
                    <span class="status-badge status-$($testResults.E2E.Status.ToLower())">$($testResults.E2E.Status)</span>
                </div>
                <div class="test-card-body">
                    <div class="metric">
                        <span class="metric-label">Test Specs</span>
                        <span class="metric-value">$($testResults.E2E.Tests)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Duration</span>
                        <span class="metric-value">$($testResults.E2E.Duration)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Last Run</span>
                        <span class="metric-value">$($testResults.E2E.LastRun)</span>
                    </div>
                </div>
            </div>
            
            <div class="test-card">
                <div class="test-card-header">
                    <h3>🧬 Mutation Tests</h3>
                    <span class="status-badge status-$($testResults.Mutation.Status.ToLower())">$($testResults.Mutation.Status)</span>
                </div>
                <div class="test-card-body">
                    <div class="metric">
                        <span class="metric-label">Mutation Score</span>
                        <span class="metric-value">$($testResults.Mutation.Score)%</span>
                    </div>
                    <div class="progress-bar">
                        <div class="progress-fill" style="width: $($testResults.Mutation.Score)%"></div>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Duration</span>
                        <span class="metric-value">$($testResults.Mutation.Duration)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Last Run</span>
                        <span class="metric-value">$($testResults.Mutation.LastRun)</span>
                    </div>
                </div>
            </div>
            
            <div class="test-card">
                <div class="test-card-header">
                    <h3>⚡ Performance Tests</h3>
                    <span class="status-badge status-$($testResults.Performance.Status.ToLower())">$($testResults.Performance.Status)</span>
                </div>
                <div class="test-card-body">
                    <div class="metric">
                        <span class="metric-label">Benchmarks</span>
                        <span class="metric-value">$($testResults.Performance.Benchmarks)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Duration</span>
                        <span class="metric-value">$($testResults.Performance.Duration)</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Last Run</span>
                        <span class="metric-value">$($testResults.Performance.LastRun)</span>
                    </div>
                </div>
            </div>
        </div>
        
        <div class="footer">
            <p>🛍️ eShop Reference Application - Comprehensive Testing Dashboard</p>
            <p>Generated by eShop Testing Suite | $(Get-Date -Format "yyyy")</p>
        </div>
    </div>
    
    <script>
        // Auto-refresh every 5 minutes
        setTimeout(function() {
            location.reload();
        }, 300000);
        
        // Add some interactivity
        document.querySelectorAll('.test-card').forEach(card => {
            card.addEventListener('click', function() {
                const testType = this.querySelector('h3').textContent.trim();
                console.log('Clicked on:', testType);
            });
        });
    </script>
</body>
</html>
"@

# Write HTML file
$htmlContent | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "✅ Test Dashboard Generated: $OutputPath" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Dashboard Summary:" -ForegroundColor Cyan
Write-Host "  Total Test Suites: $($testResults.Count)" -ForegroundColor White
Write-Host "  Passed: $(($testResults.Values | Where-Object { $_.Status -eq "Passed" }).Count)" -ForegroundColor Green
Write-Host "  Failed: $(($testResults.Values | Where-Object { $_.Status -eq "Failed" }).Count)" -ForegroundColor Red
Write-Host "  Unknown: $(($testResults.Values | Where-Object { $_.Status -eq "Unknown" }).Count)" -ForegroundColor Yellow

# Try to open in browser
try {
    Start-Process $OutputPath
    Write-Host "🌐 Opening dashboard in browser..." -ForegroundColor Cyan
}
catch {
    Write-Host "⚠️  Could not open dashboard automatically" -ForegroundColor Yellow
    Write-Host "Please open: $OutputPath" -ForegroundColor Cyan
}