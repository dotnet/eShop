# Setup Guide for Live Coding Presentation

## Pre-Presentation Checklist (1-2 Days Before)

### 1. Fork and Clone eShop

#### Step 1: Fork on GitHub
1. **Go to**: https://github.com/dotnet/eShop
2. **Click "Fork"** button (top right corner) 
3. **Select your account** as the destination
4. **Wait** for fork to complete (30-60 seconds)
5. Your fork will be at: `https://github.com/YOUR_USERNAME/eShop`

#### Step 2: Clone Your Fork
```powershell
cd C:\Users\mlj\Source\AiToolLab\Agents2

# Clone YOUR forked repository (replace YOUR_USERNAME with your GitHub username)
git clone https://github.com/YOUR_USERNAME/eShop.git
cd eShop

# Verify you're on main branch
git branch

# Check remote points to YOUR fork
git remote -v
```

### 2. Verify Build and Run

```powershell
# Check .NET SDK version (need 9.0+ or 10.0+)
dotnet --version

# Navigate to source directory
cd C:\Users\mlj\Source\AiToolLab\Agents2\eShop

# Build Ordering service specifically (avoids MAUI workload requirement)
cd src
dotnet build Ordering.Domain
dotnet build Ordering.API

# Run existing Ordering unit tests
cd ..\tests\Ordering.UnitTests
dotnet test

# Key files for demo:
# - Domain models: src/Ordering.Domain/AggregatesModel/
# - API: src/Ordering.API/
# - Tests: tests/Ordering.UnitTests/
# - API: src/Services/Ordering/Ordering.API/
# - Existing tests: src/Services/Ordering/Ordering.UnitTests/
```

### 3. Configure Custom Subagents

#### Create/Update `.github/copilot-instructions.md` in demo-repo
```markdown
# Custom Agents

## DomainSpecialist
Researches feasibility and possible solutions for a feature.

## Planner
Creates a detailed, step-by-step development plan for a feature.

## tdd-red
Prepares tests for a feature based on the findings and plan. This is the "Red" phase of TDD. Tests should fail initially.

## tdd-green
Writes production code to make tests pass.

## Orchestrator
The primary interface for the human user. Manages the software development lifecycle using a TDD approach.
```

### 4. Create Feature Branch
```powershell
git checkout -b feature/promotional-discounts

# Push to your repository (will only work after updating remote to your GitHub)
git push -u origin feature/promotional-discounts
```

**Note**: Make sure you've updated the remote to your GitHub repository first (see step 1).

### 5. Review Test Structure

The eShop repository already includes a test project:

```powershell
# Existing test location:
# src/Services/Ordering/Ordering.UnitTests/

# You can add your discount tests here, or create a new test class:
# Ordering.UnitTests/Domain/DiscountAggregateTests/DiscountCalculatorTests.cs

# The test project already references:
# - Ordering.Domain
# - Ordering.Infrastructure  
# - xUnit framework
```

**For the demo, you'll add new test files alongside existing tests.**

### 6. Prepare Documentation
- Copy `FEATURE_SPECIFICATION.md` to demo-repo root
- Copy `PRESENTATION_SCRIPT.md` for reference during presentation
- Copy `PROMPT_TEMPLATES.md` for quick access

### 7. Pre-run the Demo (Optional but Recommended)
- Run through the entire presentation once
- Create a `practice` branch with completed implementation
- This becomes your backup plan if live coding has issues
- Learn the approximate timing for each phase

### 8. Create Backup Checkpoints
```powershell
# After research & planning
git add -A
git commit -m "Checkpoint 1: Research and planning complete"
git tag checkpoint-1

# After Red phase
git commit -m "Checkpoint 2: Tests written (Red phase)"
git tag checkpoint-2

# After Green phase
git commit -m "Checkpoint 3: Implementation complete (Green phase)"
git tag checkpoint-3

# Push tags
git push --tags
```

---

## Day of Presentation Setup

### 1. Environment Preparation (30 minutes before)

#### Clean Up
```powershell
cd C:\Users\mlj\Source\AiToolLab\Agents2\eShop

# Reset to initial state
git checkout feature/promotional-discounts
git reset --hard origin/feature/promotional-discounts

# Ensure everything builds
dotnet build
```

#### VS Code Configuration
- Open eShop in VS Code
- Close unnecessary tabs
- Increase font size for presentation (Ctrl+, search "editor.fontSize": 16)
- Hide minimap (optional)
- Enable auto-save
- Open GitHub Copilot Chat panel
- Test that custom agents are recognized (type `@DomainSpecialist test`)

#### Terminal Setup
- Split terminal if showing command output
- Clear terminal history: `Clear-Host` or `cls`
- Navigate to project root

### 2. Documentation Prep
- Open `PRESENTATION_SCRIPT.md` in separate window/monitor
- Open `PROMPT_TEMPLATES.md` for copy-paste
- Open `FEATURE_SPECIFICATION.md` in browser (for sharing screen if needed)

### 3. Screen Recording (Backup)
```powershell
# Start recording using OBS Studio, Windows Game Bar, or similar
# Win+G to open Game Bar
# Or use PowerShell to remind you:
Write-Host "====================================" -ForegroundColor Yellow
Write-Host "REMINDER: Start screen recording!" -ForegroundColor Red
Write-Host "====================================" -ForegroundColor Yellow
```

### 4. Final Checklist
- [ ] Repository cloned and building
- [ ] Custom agents configured and tested
- [ ] Test framework running
- [ ] Feature specification ready
- [ ] Presentation script accessible
- [ ] Font size increased
- [ ] Screen recording started
- [ ] Presentation timer ready (phone or app)
- [ ] Water bottle nearby
- [ ] Backup checkpoints tagged

---

## During Presentation - Quick Commands

### Reset if Something Goes Wrong
```powershell
# Go back to specific checkpoint
git reset --hard checkpoint-2
git clean -fd

# Or go back to start
git reset --hard origin/feature/promotional-discounts
```

### Quick Status Check
```powershell
# Check what's been modified
git status

# Check commit history
git log --oneline

# Check which branch
git branch
```

### Run Tests
```powershell
# .NET
dotnet test --logger "console;verbosity=detailed"
```

### Build and Run
```powershell
# .NET
dotnet build
dotnet run --project src/Services/Ordering/Ordering.API
```

---

## Post-Presentation Cleanup

### 1. Save the Work
```powershell
# Commit final state
git add -A
git commit -m "Complete: Promotional discount system demo"
git push

# Create demo tag
git tag demo-complete-$(Get-Date -Format 'yyyy-MM-dd')
git push --tags
```

### 2. Create Summary Document
```markdown
# Presentation Summary

Date: [DATE]
Repository: [NAME]
Feature: Promotional Discount System

## What We Built
- [List key files]
- [Test coverage stats]
- [Commit count]

## Time Breakdown
- Research & Planning: X minutes
- Red Phase: X minutes
- Green Phase: X minutes
- Orchestration: X minutes

## Key Learnings
- [Audience feedback]
- [What worked well]
- [What to improve]

## Resources
- Repository: [URL]
- Recording: [URL if available]
- Slides: [URL if created]
```

### 3. Share Resources
- Push code to public repo or gist
- Share recording link
- Distribute prompt templates
- Send follow-up email with resources

---

## Troubleshooting

### Agent Not Responding
**Issue**: Agent doesn't seem to understand commands
**Solution**:
1. Check `.github/copilot-instructions.md` exists
2. Reload VS Code window (Ctrl+Shift+P > "Developer: Reload Window")
3. Check Copilot subscription status
4. Use `@workspace` as fallback if specific agent fails

### Tests Won't Run
**Issue**: Test framework errors
**Solution**:
```powershell
# Restore packages
dotnet restore
dotnet build
```

### Build Errors
**Issue**: Code doesn't compile
**Solution**:
1. Check all using/import statements
2. Verify project references
3. Use Copilot: "Why is this code not compiling?"
4. Fallback to checkpoint: `git reset --hard checkpoint-X`

### Merge Conflicts (if pulling latest)
**Solution**:
```powershell
# Accept theirs (from remote)
git checkout --theirs [file]

# Accept ours (local)
git checkout --ours [file]

# Or abort merge
git merge --abort
```

### Performance Issues
**Issue**: VS Code or Copilot is slow
**Solution**:
1. Close extra applications
2. Disable extensions except Copilot
3. Clear VS Code cache
4. Restart VS Code

---

## Tips for Success

### Before Starting
✅ Practice at least once
✅ Know the codebase structure
✅ Have backup checkpoints
✅ Test all commands work
✅ Verify agents respond correctly

### During Presentation
✅ Stay calm if something fails - it's reality
✅ Narrate what you're doing
✅ Engage audience with questions
✅ Watch timing - don't rush or drag
✅ Have fun and be enthusiastic

### If Running Over Time
- Skip the BOGO orchestrator demo
- Summarize instead of showing
- Speed up explanations
- Jump to checkpoint 3 and review

### If Ahead of Schedule
- Go deeper into test explanations
- Show refactoring phase
- Add Q&A time
- Demonstrate debugging with Copilot

---

## Additional Resources

### Learn More About TDD
- https://martinfowler.com/bliki/TestDrivenDevelopment.html
- "Test Driven Development: By Example" - Kent Beck

### GitHub Copilot Resources
- https://docs.github.com/copilot
- GitHub Copilot Discord community
- GitHub Copilot blog

### Demo Repository
- eShop: https://github.com/dotnet/eShop
- Microsoft Microservices eBook: https://docs.microsoft.com/dotnet/architecture/microservices/
