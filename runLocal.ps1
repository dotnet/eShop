<#
.SYNOPSIS
    Runs the eShop Aspire application locally.

.DESCRIPTION
    This script starts the eShop application using .NET Aspire.
    It ensures Docker is running and launches the AppHost project.

.PARAMETER Build
    Force rebuild before running.

.PARAMETER Watch
    Run with hot reload enabled.

.PARAMETER HttpOnly
    Use HTTP endpoints instead of HTTPS (useful for testing).

.EXAMPLE
    .\runLocal.ps1

.EXAMPLE
    .\runLocal.ps1 -Build

.EXAMPLE
    .\runLocal.ps1 -Watch -HttpOnly
#>

param(
    [switch]$Build,
    [switch]$Watch,
    [switch]$HttpOnly
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }

# Script location
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$appHostPath = Join-Path $scriptPath "src\eShop.AppHost\eShop.AppHost.csproj"

Write-Info "=========================================="
Write-Info "  eShop - .NET Aspire Local Runner"
Write-Info "=========================================="
Write-Host ""

# Check if Docker is running
Write-Info "Checking Docker status..."

# First check if docker command exists
$dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerCmd) {
    Write-Error "Docker is not installed or not in PATH."
    Write-Warning "Download Docker Desktop: https://docs.docker.com/get-docker/"
    exit 1
}

# Then check if Docker daemon is running
$prevErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
$null = & docker info 2>&1
$dockerExitCode = $LASTEXITCODE
$ErrorActionPreference = $prevErrorAction

if ($dockerExitCode -ne 0) {
    Write-Error "Docker daemon is not running. Please start Docker Desktop."
    exit 1
}
Write-Success "Docker is running"

# Check .NET SDK
Write-Info "Checking .NET SDK..."

$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    Write-Error ".NET SDK is not installed or not in PATH."
    Write-Warning "Download .NET SDK: https://dot.net/download"
    exit 1
}

$prevErrorAction = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
$dotnetVersion = & dotnet --version 2>&1
$ErrorActionPreference = $prevErrorAction
Write-Success ".NET SDK version: $dotnetVersion"

# Set environment variables if needed
if ($HttpOnly) {
    Write-Warning "Running with HTTP endpoints only (ESHOP_USE_HTTP_ENDPOINTS=1)"
    $env:ESHOP_USE_HTTP_ENDPOINTS = "1"
}

# Build if requested
if ($Build) {
    Write-Info "Building the solution..."
    dotnet build $appHostPath -c Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        exit 1
    }
    Write-Success "Build completed successfully"
}

Write-Host ""
Write-Info "Starting eShop Aspire application..."
Write-Info "Project: $appHostPath"
Write-Host ""

# Run the application
if ($Watch) {
    Write-Info "Running with hot reload (dotnet watch)..."
    dotnet watch run --project $appHostPath
} else {
    dotnet run --project $appHostPath
}

# Cleanup environment variables
if ($HttpOnly) {
    Remove-Item Env:\ESHOP_USE_HTTP_ENDPOINTS -ErrorAction SilentlyContinue
}
