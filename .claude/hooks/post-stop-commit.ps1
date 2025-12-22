# Post-Stop Hook: Commit changes and optionally create a PR when Claude stops
# This hook runs when Claude Code stops (either by completing or being interrupted)

param()

$ErrorActionPreference = "SilentlyContinue"

# Read input from stdin
$input_json = $input | Out-String
if (-not $input_json) {
    $input_json = [Console]::In.ReadToEnd()
}

# Get project directory
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

    # Check if there are any changes to commit
    $hasChanges = git status --porcelain 2>&1
    if (-not $hasChanges) {
        Write-Host "No changes to commit"
        Pop-Location
        exit 0
    }

    # Get current branch
    $currentBranch = git branch --show-current 2>&1

    # Don't auto-commit on main/master
    if ($currentBranch -eq "main" -or $currentBranch -eq "master") {
        Write-Host "On main/master branch - skipping auto-commit. Please create a feature branch first."
        Pop-Location
        exit 0
    }

    # Stage all changes
    git add -A 2>&1 | Out-Null

    # Generate commit message based on changed files
    $changedFiles = git diff --cached --name-only 2>&1
    $fileCount = ($changedFiles | Measure-Object -Line).Lines

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $commitMessage = @"
Claude Code changes - $timestamp

Changed files:
$changedFiles

Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>
"@

    # Commit changes
    git commit -m $commitMessage 2>&1 | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Committed changes on branch: $currentBranch"

        # Check if branch exists on remote
        $remoteExists = git ls-remote --heads origin $currentBranch 2>&1

        # Push to remote
        if ($remoteExists) {
            git push 2>&1 | Out-Null
        } else {
            git push -u origin $currentBranch 2>&1 | Out-Null
        }

        if ($LASTEXITCODE -eq 0) {
            Write-Host "Pushed to remote"

            # Check if gh CLI is available for PR creation
            $ghAvailable = Get-Command gh -ErrorAction SilentlyContinue
            if ($ghAvailable) {
                # Check if PR already exists
                $existingPR = gh pr view $currentBranch 2>&1
                if ($LASTEXITCODE -ne 0) {
                    # No existing PR, create one
                    Write-Host "Creating Pull Request..."

                    $prBody = @"
## Summary
Automated changes made by Claude Code on branch ``$currentBranch``

## Changed Files
$changedFiles

---
Generated with [Claude Code](https://claude.com/claude-code)
"@

                    gh pr create --title "Claude Code: $currentBranch" --body $prBody 2>&1

                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "Pull Request created successfully"
                    }
                } else {
                    Write-Host "PR already exists for this branch"
                }
            } else {
                Write-Host "GitHub CLI (gh) not found - skipping PR creation"
            }
        }
    }
}
catch {
    Write-Host "Error: $_"
}
finally {
    Pop-Location
}

exit 0
