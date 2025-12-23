---
description: Orchestrate full implementation workflow with planning, branching, testing, and PR
argument-hint: [branch-name] [task description]
allowed-tools: Bash, Write, Read, Edit, Glob, Grep, Task, TodoWrite, EnterPlanMode, ExitPlanMode
---

# Full Implementation Workflow

Execute the complete development workflow for: **$ARGUMENTS**

## Workflow Steps

### Step 1: Parse Arguments
Extract from `$ARGUMENTS`:
- First word = branch name (e.g., `feature/add-inventory`)
- Remaining words = task description

If no branch name provided, ask the user for one.

### Step 2: Git Setup
```bash
# Fetch latest and checkout main
git fetch origin main
git checkout main
git pull origin main

# Create and checkout new feature branch
git checkout -b [branch-name]
```

Verify branch was created successfully before proceeding.

### Step 3: Planning Phase
1. Enter plan mode using `EnterPlanMode` tool
2. Analyze the task requirements
3. Explore the codebase to understand:
   - Existing patterns and conventions
   - Related files and modules
   - Dependencies and integrations
4. Create a detailed implementation plan
5. Write the plan to a file and use `ExitPlanMode` for user approval
6. After approval, create todo list using `TodoWrite` with all implementation tasks

### Step 4: Implementation
Execute each task from the todo list:
1. Mark task as `in_progress` before starting
2. Implement the changes following existing patterns
3. Mark task as `completed` after finishing
4. Move to next task

Key implementation guidelines:
- Follow existing code patterns in the codebase
- Reference CLAUDE.md for project conventions
- Keep changes focused and minimal
- Don't over-engineer

### Step 5: Testing with Playwright
After implementation is complete:

```bash
# Navigate to Admin.UI (or relevant UI project)
cd src/Admin.UI

# Install Playwright if needed
npx playwright install

# Run e2e tests
npx playwright test
```

If tests fail:
1. Analyze the failure
2. Add fix tasks to todo list
3. Implement fixes
4. Re-run tests until all pass

### Step 6: Commit and Create PR
After tests pass:

```bash
# Stage all changes
git add -A

# Show what will be committed
git status
git diff --cached --stat
```

Create commit with descriptive message:
```bash
git commit -m "$(cat <<'EOF'
[type]: Brief description of changes

- Detailed point 1
- Detailed point 2

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

Push and create PR:
```bash
# Push to remote
git push -u origin [branch-name]

# Create PR using GitHub CLI
gh pr create --title "[type]: Brief description" --body "$(cat <<'EOF'
## Summary
- What was implemented
- Key changes made

## Test Plan
- [ ] E2E tests pass
- [ ] Manual testing completed

## Screenshots (if applicable)
<!-- Add screenshots here -->

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

### Step 7: Final Summary
Provide a summary including:
- Branch name
- PR URL
- List of files changed
- Test results
- Any follow-up actions needed

## Error Handling

- If git operations fail, stop and report the error
- If tests fail repeatedly (3+ attempts), ask user how to proceed
- If PR creation fails, provide manual instructions

## Example Usage

```
/implement feature/add-inventory Add inventory tracking to warehouse service with stock levels and alerts
```

This will:
1. Create branch `feature/add-inventory`
2. Plan the inventory tracking implementation
3. Implement the feature
4. Run Playwright tests
5. Commit and create PR
