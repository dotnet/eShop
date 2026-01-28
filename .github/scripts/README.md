# Agent Workflow Scripts

This directory contains scripts to support the agent-based development workflow in eShop.

## Scripts

### validate-feature-files.ps1

Validates that all files in `.github/features` follow the correct naming conventions.

**Usage:**

```powershell
# Validate all features
.\.github\scripts\validate-feature-files.ps1

# Validate a specific feature
.\.github\scripts\validate-feature-files.ps1 -FeatureName promotional-discounts

# Auto-fix naming issues
.\.github\scripts\validate-feature-files.ps1 -Fix
```

**Naming Conventions:**
- Feature folder name: `kebab-case` (lowercase with hyphens, e.g., `promotional-discounts`)
- Files in feature folder:
  - `{feature-name}.findings.md` - DomainSpecialist research and recommendations
  - `{feature-name}.plan.md` - Planner's development plan
  - `{feature-name}.memory.md` - Consolidated memory for all agents
  - `README.md` - Optional status summary

**Integration with Git:**

To run validation automatically before commits, add this to `.git/hooks/pre-commit`:

```bash
#!/bin/sh
pwsh .github/scripts/validate-feature-files.ps1
if [ $? -ne 0 ]; then
    echo "Feature file validation failed. Please fix naming issues."
    exit 1
fi
```

Make the hook executable:
```bash
chmod +x .git/hooks/pre-commit
```

## Future Scripts

Consider adding:
- `create-feature.ps1` - Scaffold a new feature with proper structure
- `archive-feature.ps1` - Move completed features to an archive folder
- `feature-status.ps1` - Generate a report of all feature statuses
