# Pre-Edit Hook: Create a new git branch before making code changes
# This hook runs before Edit or Write tools are used
#
# Branch name is determined by (in order of priority):
# 1. CLAUDE_BRANCH_NAME environment variable (set before starting Claude)
# 2. Skip branch creation if not set (stay on current branch)
#
# Usage: Set CLAUDE_BRANCH_NAME before starting Claude Code session
#   $env:CLAUDE_BRANCH_NAME = "feature/my-feature-name"
#   claude

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

# Get branch name from environment variable
$branchName = $env:CLAUDE_BRANCH_NAME
if (-not $branchName) {
    # No branch name specified, skip branch creation
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

    # Check if already on the target branch
    if ($currentBranch -eq $branchName) {
        Pop-Location
        exit 0
    }

    # Check if we're on main/master branch
    if ($currentBranch -eq "main" -or $currentBranch -eq "master") {
        # Check if there are any new commits ahead of origin/main or origin/master
        $remoteBranch = "origin/$currentBranch"

        # Fetch latest from remote (silent)
        git fetch origin $currentBranch 2>&1 | Out-Null

        # Count commits ahead of remote
        $commitsAhead = git rev-list --count "$remoteBranch..HEAD" 2>&1

        # If no new commits from main, create/checkout the feature branch
        if ($LASTEXITCODE -eq 0 -and [int]$commitsAhead -eq 0) {
            # Check if branch already exists
            $branchExists = git show-ref --verify --quiet "refs/heads/$branchName" 2>&1

            if ($LASTEXITCODE -eq 0) {
                # Branch exists, checkout
                git checkout $branchName 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Switched to existing branch: $branchName"
                }
            } else {
                # Create and checkout new branch
                git checkout -b $branchName 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Created new branch: $branchName"
                }
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
