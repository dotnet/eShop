# Pre-Edit Hook: Create a new git branch before making code changes
# This hook runs before Edit or Write tools are used

param()

$ErrorActionPreference = "SilentlyContinue"

# Read input from stdin
$input_json = $input | Out-String
if (-not $input_json) {
    $input_json = [Console]::In.ReadToEnd()
}

try {
    $data = $input_json | ConvertFrom-Json
    $file_path = $data.tool_input.file_path
}
catch {
    # If we can't parse input, just continue
    exit 0
}

# Get current directory
$projectDir = $env:CLAUDE_PROJECT_DIR
if (-not $projectDir) {
    $projectDir = Get-Location
}

Push-Location $projectDir

try {
    # Check if we're in a git repository
    $gitStatus = git rev-parse --is-inside-work-tree 2>&1
    if ($LASTEXITCODE -ne 0) {
        Pop-Location
        exit 0
    }

    # Get current branch
    $currentBranch = git branch --show-current 2>&1

    # Check if we're on main/master branch
    if ($currentBranch -eq "main" -or $currentBranch -eq "master") {
        # Check if there are any uncommitted changes already
        $hasChanges = git status --porcelain 2>&1

        if (-not $hasChanges) {
            # No changes yet, create a new feature branch
            $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
            $branchName = "feature/claude-$timestamp"

            # Create and checkout new branch
            git checkout -b $branchName 2>&1 | Out-Null

            if ($LASTEXITCODE -eq 0) {
                Write-Host "Created new branch: $branchName"
            }
        }
    }
}
catch {
    # Silently continue on any error
}
finally {
    Pop-Location
}

exit 0
