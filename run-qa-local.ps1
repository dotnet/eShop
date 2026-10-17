<#
.SYNOPSIS
  Local test harness for the post-merge QA agent. Playwright runs headless by default.

.EXAMPLE
  .\run-qa-local.ps1                      # dry run: impact analysis only (no LLM, no browser, no Docker)
.EXAMPLE
  .\run-qa-local.ps1 -Mode full           # full pipeline on the last commit (HEAD~1..HEAD)
.EXAMPLE
  .\run-qa-local.ps1 -Mode full -Base main -Head HEAD   # everything on your branch since main
.EXAMPLE
  .\run-qa-local.ps1 -Mode full -Headed   # watch the browser while the affected tests run

  The agent compares two COMMITS, so commit your test change on a branch first.
#>
param(
  [ValidateSet('dry', 'full')][string]$Mode = 'dry',
  [string]$Base = 'HEAD~1',
  [string]$Head = 'HEAD',
  [switch]$Headed,
  [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
Set-Location $PSScriptRoot

function Need([string]$cmd, [string]$hint) {
  if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) { throw "'$cmd' was not found on PATH. $hint" }
}

Write-Host "== Checking prerequisites ($Mode mode) ==" -ForegroundColor Cyan
Need git    'Install Git for Windows.'
Need python 'Install Python 3.10+ from python.org and tick "Add python.exe to PATH".'
Write-Host ("python  " + (python --version 2>&1))
Write-Host ("git     " + (git --version))

if ($Mode -eq 'full') {
  Need node   'Install the Node.js LTS release.'
  Need npx    'Comes with Node.js.'
  Need dotnet 'Install the .NET 10 SDK that global.json asks for.'
  Need docker 'Install Docker Desktop (or Podman) - eShop starts Postgres, Redis and RabbitMQ as containers.'
  Write-Host ("dotnet  " + (dotnet --version))
  Write-Host ("node    " + (node --version))
  docker info *> $null
  if ($LASTEXITCODE -ne 0) { throw 'Docker is installed but not running. Start Docker Desktop, wait until it says "running", then retry.' }
  Write-Host 'docker  running'

  if (-not $SkipInstall) {
    if (-not (Test-Path node_modules)) { Write-Host '== npm ci ==' -ForegroundColor Cyan; npm ci; if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' } }
    Write-Host '== Installing Playwright Chromium (skipped if already present) ==' -ForegroundColor Cyan
    npx playwright install chromium
    if ($LASTEXITCODE -ne 0) { throw 'playwright install failed' }
  }

  if (-not ($env:GITHUB_MODELS_TOKEN -or $env:GEMINI_API_KEY -or $env:ANTHROPIC_API_KEY)) {
    Write-Warning ('No LLM credentials in this shell. The agent will fall back to rule-based impact analysis and run every test of the impacted journeys. ' +
      'To test the AI stages set ONE of: $env:GITHUB_MODELS_TOKEN (fine-grained token with Models read), $env:GEMINI_API_KEY, $env:ANTHROPIC_API_KEY.')
  }
}

# Same settings as .github/workflows/playwright.yml (seeded demo user; single quotes keep the '$').
$env:ESHOP_USE_HTTP_ENDPOINTS = '1'
$env:USERNAME1 = 'bob'
$env:PASSWORD = 'Pass123$'
$env:PLAYWRIGHT_HTML_OPEN = 'never'

$env:DIFF_BASE = (git rev-parse --verify $Base).Trim()
$env:DIFF_HEAD = (git rev-parse --verify $Head).Trim()
Write-Host ("Comparing {0}..{1}" -f $env:DIFF_BASE.Substring(0, 8), $env:DIFF_HEAD.Substring(0, 8))
git --no-pager diff --stat $env:DIFF_BASE $env:DIFF_HEAD

if ($Mode -eq 'dry') { $env:QA_DRY_RUN = 'true' } else { Remove-Item Env:QA_DRY_RUN -ErrorAction SilentlyContinue }
if ($Headed) { $env:QA_HEADED = '1' } else { Remove-Item Env:QA_HEADED -ErrorAction SilentlyContinue }

Write-Host "`n== Running the agent ==" -ForegroundColor Cyan
python post_merge_qa_agent.py
$code = $LASTEXITCODE

Write-Host ''
if (Test-Path qa-report.md)                    { Write-Host ("QA report:        " + (Resolve-Path qa-report.md)) }
if (Test-Path playwright-report\index.html)    { Write-Host ("Playwright report: " + (Resolve-Path playwright-report\index.html) + '  (npx playwright show-report)') }
if (Test-Path e2e\generated)                   { Write-Host 'New tests (if any):  e2e\generated\' }
if (Test-Path qa-quarantine)                   { Write-Host 'Held back tests:    qa-quarantine\  (run was red)' }
exit $code
