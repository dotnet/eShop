# Live Coding Presentation Script
## GitHub Copilot Custom Subagents Demo

---

## PRE-PRESENTATION CHECKLIST
- [ ] Repository cloned and building successfully
- [ ] Custom subagents configured in VS Code
- [ ] Test framework running
- [ ] Screen recording started (backup)
- [ ] Feature specification ready
- [ ] Git repository initialized with initial commit

---

## PHASE 1: INTRODUCTION (5 minutes)

### Opening (2 minutes)
**SAY**: "Welcome! Today I'm going to show you how GitHub Copilot custom subagents can dramatically improve your development workflow using a Test-Driven Development approach. We'll be working with eShop - Microsoft's official .NET reference application - a real-world production-grade codebase built with .NET 9 and microservices architecture."

**SHOW**: 
- Quick tour of the repository structure
- Point out key areas we'll be working with

**SAY**: "The feature we're implementing today is a promotional discount system - a common business requirement with interesting complexity. It has business rules, edge cases, and integration points - perfect for demonstrating how AI agents understand and implement real requirements."

### Introduce Subagents (3 minutes)
**SAY**: "We'll be using 5 specialized custom agents today:"

**SHOW** (on screen or slides):
1. **DomainSpecialist** - "The researcher who explores the codebase and understands feasibility"
2. **Planner** - "The architect who creates detailed implementation plans"
3. **tdd-red** - "The test engineer who writes comprehensive failing tests"
4. **tdd-green** - "The developer who implements code to pass those tests"
5. **Orchestrator** - "The project manager who coordinates the entire workflow"

**SAY**: "This is TDD on steroids. Let's see it in action."

---

## PHASE 2: RESEARCH & PLANNING (10 minutes)

### Step 1: DomainSpecialist Agent (5 minutes)

**SAY**: "First, we need to understand the codebase. Let me ask the DomainSpecialist to research."

**TYPE IN COPILOT CHAT**: 
```
@DomainSpecialist

I need to implement a promotional discount system in this e-commerce codebase. 
Please research:
1. Where order/pricing calculations currently happen
2. What patterns are used for business logic
3. How to integrate a new discount engine
4. What models/entities exist that we can leverage
5. Any existing discount or promotion code

Provide a feasibility assessment and integration points.
```

**WAIT FOR RESPONSE**

**SAY** (while reviewing response): 
"Notice how the agent explores the codebase, identifies patterns, and gives us concrete integration points. This would normally take a developer 30-60 minutes of manual exploration."

**HIGHLIGHT**:
- Files identified
- Patterns discovered
- Integration points suggested
- Potential challenges noted

### Step 2: Planner Agent (5 minutes)

**SAY**: "Now that we understand the landscape, let's create a detailed plan."

**TYPE IN COPILOT CHAT**:
```
@Planner

Based on the DomainSpecialist findings, create a detailed step-by-step TDD plan to implement:

Feature: Promotional Discount System
Requirements:
- Support percentage and fixed-amount discounts
- Volume discounts (buy X items, get Y% off)
- Category-specific discounts
- Time-bound promotions
- Business rule: Max 50% total discount stacking
- Business rule: Minimum order amount requirements
- Track applied discounts for reporting

Create a plan that follows TDD, identifying:
1. Models/entities needed
2. Test cases (with examples)
3. Implementation steps
4. Integration points
```

**WAIT FOR RESPONSE**

**SAY** (while reviewing):
"The Planner breaks this down into actionable steps. See how it identifies test scenarios, edge cases, and sequencing. This is our roadmap."

**HIGHLIGHT**:
- Clear test scenarios
- Step-by-step implementation order
- Edge cases identified
- Integration approach

**COMMIT**: `git commit -m "Add research and planning documentation"`

---

## PHASE 3: RED PHASE - Writing Tests (15 minutes)

**SAY**: "Now for the Red phase of TDD. We'll use the tdd-red agent to generate comprehensive tests based on our plan."

### Step 1: Generate Test Structure (7 minutes)

**TYPE IN COPILOT CHAT**:
```
@tdd-red

Using the Planner's recommendations, create comprehensive unit tests for the promotional discount system.

Start with core discount calculation tests:
1. Apply single percentage discount
2. Apply single fixed-amount discount
3. Apply volume discount (buy 5, get 10% off)
4. Stack multiple discounts with 50% cap
5. Exclude certain categories
6. Validate minimum order amount
7. Handle expired promotions
8. Handle no applicable discounts

Use [xUnit/Jest] framework. Follow the existing test patterns in the codebase.
Create test file: [path]/DiscountEngineTests.cs or DiscountEngine.test.ts
```

**WAIT FOR RESPONSE**

**SAY**: "Look at the test coverage - we have happy paths, edge cases, and business rule validations. These tests should all fail right now because we haven't implemented anything."

### Step 2: Review and Run Tests (5 minutes)

**SHOW**: 
- Open generated test file
- Walk through 2-3 key test cases
- Explain the test structure

**SAY**: "Notice how the tests are:
- Descriptive with clear names
- Following AAA pattern (Arrange, Act, Assert)
- Testing one thing each
- Including edge cases"

**RUN TESTS**

**SAY**: "Perfect! All tests fail as expected. We're in the Red phase. This is exactly what we want."

**SHOW**: Test runner output with failures

### Step 3: Create Additional Tests (3 minutes)

**TYPE IN COPILOT CHAT**:
```
@tdd-red

Now create integration tests for:
1. Applying discounts during order checkout flow
2. Persisting discount application history
3. Retrieving applied discounts for reporting

Create test file: [path]/DiscountIntegrationTests.cs or DiscountIntegration.test.ts
```

**SAY**: "We also need integration tests to ensure our discount system works with the existing order processing."

**COMMIT**: `git commit -m "Add: Comprehensive test suite for discount system (Red phase)"`

---

## PHASE 4: GREEN PHASE - Implementation (20 minutes)

**SAY**: "Now the exciting part - making these tests pass! The tdd-green agent will implement the production code."

### Step 1: Implement Core Models (5 minutes)

**TYPE IN COPILOT CHAT**:
```
@tdd-green

Implement the core models and interfaces needed for the discount system to make the first set of tests pass:
- Discount model (percentage, fixed amount, volume rules)
- DiscountEngine interface
- Supporting value objects

Follow the existing coding patterns in the codebase.
Use the test file [path] as the specification.
```

**WAIT FOR RESPONSE**

**SAY**: "The agent creates the foundational models. Notice how it follows the existing code style and patterns."

**SHOW**: Generated model files
**RUN TESTS**: Some should start passing

**SAY**: "We're making progress - [X] tests now pass!"

### Step 2: Implement Business Logic (8 minutes)

**TYPE IN COPILOT CHAT**:
```
@tdd-green

Implement the DiscountCalculator class to handle:
1. Single discount application
2. Multiple discount stacking with 50% cap
3. Category exclusions
4. Minimum order validation
5. Time-bound promotion validation

Make the remaining unit tests pass.
Test file: [path]/DiscountEngineTests.[cs/ts]
```

**WAIT FOR RESPONSE**

**SAY**: "Watch as the agent implements the business logic step by step. It's following the TDD approach - simple first, then adding complexity."

**SHOW**: 
- Implementation code
- Explain key business logic (spend 2-3 minutes)

**RUN TESTS**

**SAY**: "All unit tests are passing! Green phase achieved for the core logic."

### Step 3: Integration Implementation (7 minutes)

**TYPE IN COPILOT CHAT**:
```
@tdd-green

Now integrate the discount system into the order checkout flow:
1. Hook into order calculation pipeline
2. Implement discount persistence
3. Create discount history repository
4. Add discount retrieval for reporting

Make the integration tests pass.
Test file: [path]/DiscountIntegrationTests.[cs/ts]
```

**WAIT FOR RESPONSE**

**SAY**: "Final step - integration with the existing system."

**SHOW**: Integration code

**RUN ALL TESTS**

**SAY**: "🎉 All tests passing! We've successfully implemented a complete feature using TDD with AI assistance."

**COMMIT**: `git commit -m "Implement: Discount system with full test coverage (Green phase)"`

---

## PHASE 5: ORCHESTRATION DEMO (8 minutes)

**SAY**: "Now let me show you something powerful - the Orchestrator agent can manage this entire workflow autonomously."

### Demo Orchestrator (8 minutes)

**SAY**: "Let's add a new requirement and let the Orchestrator handle everything."

**TYPE IN COPILOT CHAT**:
```
@Orchestrator

New requirement: Add "Buy One Get One" (BOGO) discount type support.

BOGO rules:
- Buy X items of product A, get Y items of product B at Z% off
- Cannot combine with other product-specific discounts on product B
- Must be same transaction

Use the full TDD workflow:
1. Research feasibility (DomainSpecialist)
2. Plan implementation (Planner)
3. Write tests (tdd-red)
4. Implement (tdd-green)

Please coordinate the subagents and implement this feature.
```

**WAIT AND NARRATE**

**SAY** (as Orchestrator works):
"The Orchestrator is now:
- Delegating research to DomainSpecialist
- Having Planner create the approach
- Coordinating tdd-red to write tests
- Directing tdd-green to implement
- Verifying everything works together"

**SHOW**: 
- Each agent's contribution
- Tests being created
- Implementation happening
- All tests passing

**SAY**: "This is the future - AI agents working together to deliver features while you focus on business requirements and high-level decisions."

**COMMIT**: `git commit -m "Add: BOGO discount support coordinated by Orchestrator"`

---

## PHASE 6: WRAP UP (2 minutes)

### Summary

**SAY**: 
"In the last hour, we:
1. ✅ Researched an unfamiliar codebase
2. ✅ Planned a complex feature
3. ✅ Wrote comprehensive tests (Red phase)
4. ✅ Implemented production-ready code (Green phase)
5. ✅ Integrated with existing systems
6. ✅ Extended functionality with orchestration

All following TDD best practices, with full test coverage."

### Key Takeaways

**SAY**:
"What makes this powerful:

1. **Speed**: Tasks that would take days happened in an hour
2. **Quality**: Full test coverage from the start
3. **Learning**: Junior devs can learn patterns from agent code
4. **Focus**: You stay focused on 'what', agents handle 'how'
5. **Consistency**: Agents follow established patterns

These aren't just code generators - they're AI collaborators that understand context, business logic, and software engineering practices."

### Questions

**SAY**: "I'd love to hear your questions. Who wants to try this in their workflow?"

---

## TROUBLESHOOTING GUIDE

### If Agent Doesn't Respond Well
- **Rephrase**: Be more specific about what you want
- **Provide Context**: Reference specific files or patterns
- **Break It Down**: Ask for smaller chunks
- **Fallback**: Have pre-written prompts ready

### If Tests Fail Unexpectedly
- **Stay Calm**: This is reality, not a perfect demo
- **Debug Together**: Use Copilot to help debug
- **Narrate**: "Let's see how agents help with debugging"
- **Fallback**: Switch to backup checkpoint

### If Running Behind
- **Skip**: Skip BOGO orchestrator demo, summarize instead
- **Speed Up**: Run tests in background while talking
- **Abbreviate**: Show rather than explain every detail

### If Ahead of Schedule
- **Deep Dive**: Explain test patterns or business logic more
- **Refactor**: Show refactoring phase with agents
- **Q&A Early**: Open up questions earlier
- **Add Feature**: Show another quick feature add

---

## POST-PRESENTATION

### Follow-up Materials to Share
- Link to recording
- GitHub repository with commit history
- Agent prompt templates
- Setup guide for custom agents
- Additional resources

### Feedback Collection
- What resonated most?
- What would they try first?
- Concerns or blockers?
- Interest in deep-dive sessions?

---

## NOTES
- Stay enthusiastic but authentic
- If something fails, it's an opportunity to show real-world problem solving
- Engage audience with questions periodically
- Make it conversational, not scripted
- Show your genuine excitement about the technology
