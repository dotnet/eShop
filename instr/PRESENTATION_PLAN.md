# GitHub Copilot Custom Subagents - Live Coding Presentation

## Duration: 60 minutes

## Objective
Demonstrate the power of custom GitHub Copilot subagents using a TDD (Test-Driven Development) workflow on a real-world codebase.

## Custom Subagents to Showcase
1. **DomainSpecialist** - Researches feasibility and possible solutions
2. **Planner** - Creates detailed, step-by-step development plans
3. **tdd-red** - Prepares failing tests (Red phase of TDD)
4. **tdd-green** - Writes production code to make tests pass (Green phase)
5. **Orchestrator** - Manages the entire SDLC with TDD approach

## Repository: eShop (Official .NET Reference Application)

- **Type**: Modern microservices-based e-commerce reference application
- **Stars**: ~10k
- **Version**: .NET 9 with .NET Aspire orchestration
- **Language**: .NET C# backend + Blazor frontend
- **Focus**: Business logic heavy (ordering, catalog, payment processing)
- **URL**: https://github.com/dotnet/eShop
- **Demo Feature**: Add promotional discount system with business rules
- **Target Service**: Ordering.API microservice
- **Why Perfect**: 
  - Official Microsoft reference architecture
  - Modern .NET 9 with Aspire
  - Production-quality architecture with clean domain models
  - Actively maintained
  - Has Ordering.Domain with clean aggregate structure
  - Includes existing unit and functional tests

**Note**: This is the successor to the archived eShopOnContainers repository

## Presentation Structure (60 minutes)

### Phase 1: Introduction (5 minutes)
- Brief overview of the codebase
- Explain the feature we'll implement
- Introduce custom subagents concept

### Phase 2: Research & Planning (10 minutes)
**Demonstrate: DomainSpecialist + Planner Agents**
- Use **DomainSpecialist** to research the codebase and identify integration points
- Use **Planner** to create a detailed implementation plan
- Show how agents understand business context

### Phase 3: Red Phase - Writing Tests (15 minutes)
**Demonstrate: tdd-red Agent**
- Use **tdd-red** agent to generate comprehensive test cases
- Show how it understands business requirements
- Explain the tests and expected behaviors
- Run tests to confirm they fail (Red phase)

### Phase 4: Green Phase - Implementation (20 minutes)
**Demonstrate: tdd-green Agent**
- Use **tdd-green** agent to implement the feature
- Show incremental progress as tests pass one by one
- Highlight how the agent follows the plan
- Demonstrate refactoring suggestions

### Phase 5: Integration & Orchestration (8 minutes)
**Demonstrate: Orchestrator Agent**
- Use **Orchestrator** to manage the complete workflow
- Show how it coordinates between different agents
- Demonstrate edge case handling

### Phase 6: Q&A and Wrap-up (2 minutes)
- Key takeaways
- Questions from audience

## Recommended Feature: Promotional Discount System

### Feature Specification
**Context**: E-commerce ordering system
**Feature**: Implement a promotional discount engine with business rules

**Requirements**:
1. Apply percentage-based or fixed-amount discounts
2. Support multiple discount types:
   - Volume discounts (buy X, get Y% off)
   - Category-specific discounts
   - Time-bound promotions
   - First-time customer discounts
3. Business Rules:
   - Discounts cannot be stacked beyond a maximum (e.g., 50%)
   - Certain categories may be excluded
   - Minimum order amount may be required
4. Calculate final price with all applicable discounts
5. Track which discounts were applied for reporting

### Why This Feature?
- **Complex enough**: Multiple business rules and edge cases
- **Not too large**: Can be implemented in 30-35 minutes of coding
- **Business logic focused**: Minimal UI concerns
- **Real-world**: Common e-commerce requirement
- **Testable**: Clear inputs/outputs for TDD

## Technical Setup Requirements

### Before Presentation
1. Clone the chosen repository
2. Ensure it builds and runs locally
3. Identify the exact module/service to extend
4. Prepare the feature specification document
5. Test the custom agents with sample prompts
6. Have a backup plan if live coding encounters issues

### Development Environment
- Visual Studio Code with GitHub Copilot
- Custom subagents configured
- .NET SDK 8.0+ (for C# repos)
- Node.js 20+ (for TypeScript repos)
- Test framework ready (xUnit/NUnit for C#, Jest for TypeScript)

## Presentation Tips

### Do's
✅ Start with a working codebase
✅ Make commits between each phase
✅ Show agent prompts and explain reasoning
✅ Highlight how agents understand context
✅ Demonstrate error handling by agents
✅ Keep the feature scope realistic

### Don'ts
❌ Don't try to implement too complex a feature
❌ Don't skip explaining agent responses
❌ Don't ignore test failures
❌ Don't go off-script too much
❌ Don't assume audience knows TDD deeply

## Backup Plan
- Have pre-prepared checkpoints at each phase
- If an agent doesn't respond well, have fallback prompts
- Keep a "working version" branch for quick recovery

## Key Talking Points
1. **Context Awareness**: How agents understand business domain
2. **Workflow Orchestration**: How agents work together
3. **Test Quality**: How tdd-red generates comprehensive tests
4. **Code Quality**: How tdd-green follows best practices
5. **Time Savings**: What would take hours is done in minutes
6. **Learning Tool**: How juniors can learn from agent-generated code

## Success Metrics
- Feature fully implemented with passing tests
- Clear demonstration of each agent's capabilities
- Audience understands the TDD workflow
- Questions about practical applications

---

## Next Steps
1. Clone eShop repository
2. Setup the development environment (.NET 8.0 SDK)
3. Review the Ordering microservice structure
4. Configure custom GitHub Copilot agents
5. Practice the presentation flow
6. Create backup checkpoints
