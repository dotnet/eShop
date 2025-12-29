# Feature Implementation Orchestrator

You are implementing a new feature for the eShop microservices application. Follow this structured workflow to ensure high-quality, well-architected implementations.

## Feature Request
$ARGUMENTS

---

## Workflow Steps

Execute the following steps in order. Do not skip any step.

### Step 1: Clarify Requirements

Before proceeding, ensure you fully understand the requirements:

1. **Analyze the feature request** - Break down what is being asked
2. **Identify affected services** - Which microservices will be impacted?
3. **Identify data requirements** - What data needs to be stored/retrieved?
4. **Identify integration points** - What events or APIs are needed?
5. **Ask clarifying questions** - Use AskUserQuestion tool if anything is unclear:
   - Scope boundaries
   - Edge cases
   - User experience expectations
   - Performance requirements
   - Any constraints

**Output**: A clear, bullet-pointed list of requirements that the user confirms.

---

### Step 2: Propose Architecture-Aligned Solutions

Based on the eShop architecture patterns, propose solutions that follow best practices:

1. **Review relevant CLAUDE.md files** - Read the documentation for affected services
2. **Review EVENT-DRIVEN-ARCHITECTURE.md** - If events are needed
3. **Propose 2-3 solution approaches** with trade-offs:
   - Consider DDD patterns (aggregates, domain events)
   - Consider CQRS if complex read/write scenarios
   - Consider integration events for cross-service communication
   - Consider the transactional outbox pattern for reliability
4. **Recommend the best approach** with justification
5. **Get user approval** before proceeding

**Output**: Selected solution approach confirmed by user.

---

### Step 3: Create Implementation Plan (Plan Mode)

Enter plan mode to create a detailed implementation plan:

1. **Use EnterPlanMode tool** to start planning
2. **Explore the codebase** to understand existing patterns
3. **Create a detailed plan** including:
   - Files to create/modify
   - Database changes (if any)
   - API endpoints to add
   - Events to create
   - UI components to add
   - Test files to create
4. **Write the plan** to the plan file
5. **Use ExitPlanMode** after user approves the plan

**Output**: Approved implementation plan.

---

### Step 4: Create Feature Branch

Create a new git branch for the feature:

1. **Ensure clean working directory** - Check git status
2. **Pull latest from main** - Ensure up to date
3. **Create descriptive branch name** following convention:
   - `feature/<short-description>`
   - Example: `feature/add-wishlist-api`
4. **Switch to the new branch**

```bash
git checkout main
git pull origin main
git checkout -b feature/<feature-name>
```

**Output**: Confirmation of new branch creation.

---

### Step 5: Implement the Feature

Execute the implementation plan:

1. **Use TodoWrite tool** to track all implementation tasks
2. **Implement in order**:
   - Domain models first (if DDD)
   - Database migrations
   - Repository/data access
   - Application services/commands/queries
   - API endpoints
   - Integration events (if cross-service)
   - UI components
3. **Follow existing patterns** in each service
4. **Add appropriate error handling**
5. **Add logging with OpenTelemetry**
6. **Mark todos as completed** as you go

**Output**: All code implemented and todos completed.

---

### Step 6: End-to-End Testing with Playwright

Create and run E2E tests to verify the feature:

1. **Navigate to e2e directory**: `cd e2e`
2. **Install dependencies if needed**: `npm install`
3. **Create new test file** in `e2e/tests/` for the feature:
   ```typescript
   // e2e/tests/<feature-name>.spec.ts
   import { test, expect } from '@playwright/test';

   test.describe('Feature: <Feature Name>', () => {
     test('should <test description>', async ({ page }) => {
       // Test implementation
       await page.goto('/');
       // ... test steps

       // Take screenshot for verification
       await page.screenshot({
         path: `test-results/<feature-name>-result.png`,
         fullPage: true
       });

       // Assertions
       expect(...).toBe(...);
     });
   });
   ```
4. **Run the test**:
   ```bash
   npx playwright test tests/<feature-name>.spec.ts --headed
   ```
5. **Capture screenshots** at key verification points
6. **Fix any issues** found during testing
7. **Re-run until all tests pass**

**Output**: All E2E tests passing with screenshots saved.

---

### Step 7: Commit the Code

Create a well-structured commit:

1. **Review all changes**: `git status` and `git diff`
2. **Stage relevant files**: `git add <files>`
3. **Do NOT commit**:
   - `.env` files or secrets
   - Generated files (unless intentional)
   - Test screenshots (unless documentation)
4. **Create descriptive commit message**:
   ```
   feat(<scope>): <short description>

   - <bullet point of change 1>
   - <bullet point of change 2>
   - <bullet point of change 3>

   Closes #<issue-number> (if applicable)

   🤖 Generated with Claude Code

   Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
   ```
5. **Commit the changes**

**Output**: Feature committed to the feature branch.

---

## Post-Implementation Checklist

Before marking complete, verify:

- [ ] All requirements are met
- [ ] Code follows existing patterns in the codebase
- [ ] Integration events use transactional outbox (if applicable)
- [ ] Error handling is in place
- [ ] Logging is added for observability
- [ ] E2E tests pass and screenshots captured
- [ ] Code is committed to feature branch
- [ ] No secrets or sensitive data committed

---

## Final Summary

After completing all steps, provide a summary:

1. **What was implemented** - List of changes
2. **Files created/modified** - With line counts
3. **Events added** (if any)
4. **API endpoints added** (if any)
5. **How to test manually** - Step-by-step instructions
6. **Screenshots** - Reference to E2E test screenshots
7. **Next steps** - PR creation, deployment notes

---

**Now begin with Step 1: Clarify Requirements**
