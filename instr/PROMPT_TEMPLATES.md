# Prompt Templates for Live Presentation

Quick-reference prompts for eShop demo. Most paths are pre-filled and ready to use.

---

## 📁 eShop Paths Reference

**Repository Root**: `C:\Users\mlj\Source\AiToolLab\Agents2\eShop`
**Domain Models**: `src/Ordering.Domain/AggregatesModel/`
**API Layer**: `src/Ordering.API/`
**Existing Tests**: `tests/Ordering.UnitTests/`
**Functional Tests**: `tests/Ordering.FunctionalTests/`

**Note**: This is the modern eShop (not the archived eShopOnContainers)

---

## PHASE 2: RESEARCH & PLANNING

### Template 1: DomainSpecialist Research

```
@DomainSpecialist

I need to implement a promotional discount system in this e-commerce codebase. 
Please research:
1. Where order/pricing calculations currently happen
2. What patterns are used for business logic (DDD, CQRS, etc.)
3. How to integrate a new discount engine
4. What models/entities exist that we can leverage
5. Any existing discount or promotion code

Provide a feasibility assessment and integration points.
```

### Template 2: Planner TDD Plan

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

---

## PHASE 3: RED PHASE

### Template 3: Core Unit Tests

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

Use xUnit framework. Follow the existing test patterns in the codebase.
Create test file at: tests/Ordering.UnitTests/Domain/DiscountAggregateTests/DiscountCalculatorTests.cs
```

**Note**: The test project already exists at `tests/Ordering.UnitTests/`. You're adding new test files to the existing structure.

### Template 4: Integration Tests

```
@tdd-red

Now create integration tests for the discount system:
1. Applying discounts during order checkout flow
2. Persisting discount application history to database
3. Retrieving applied discounts for reporting
4. Handling concurrent discount applications

Create test file at: tests/Ordering.FunctionalTests/DiscountIntegrationTests.cs
Use the existing functional test patterns in Ordering.FunctionalTests.
Mock external dependencies where appropriate.
```

**Note**: Adjust path if you prefer creating a separate integration test file

---

## PHASE 4: GREEN PHASE

### Template 5: Core Models

```
@tdd-green

Implement the core models and interfaces needed for the discount system to make the first set of tests pass:
- Discount model (percentage, fixed amount, volume rules)
- DiscountEngine interface
- DiscountCalculationResult value object
- Supporting enums and value objects

Follow the existing coding patterns in the codebase.
Place these in: src/Ordering.Domain/AggregatesModel/DiscountAggregate/
Use the test file at tests/Ordering.UnitTests/Domain/DiscountAggregateTests/DiscountCalculatorTests.cs as the specification.
```

### Template 6: Business Logic

```
@tdd-green

Implement the DiscountCalculator class to handle:
1. Single discount application
2. Multiple discount stacking with 50% cap
3. Category exclusions
4. Minimum order validation
5. Time-bound promotion validation
6. Priority-based discount ordering

Make the remaining unit tests pass.
Test file: tests/Ordering.UnitTests/Domain/DiscountAggregateTests/DiscountCalculatorTests.cs
Implementation location: src/Ordering.Domain/AggregatesModel/DiscountAggregate/
```
### Template 7: Integration Implementation

```
@tdd-green

Now integrate the discount system into the order checkout flow:
1. Hook into order calculation pipeline in Ordering.API/Application/Commands/CreateOrderCommandHandler.cs or similar
2. Implement discount persistence using the existing repository pattern (review OrderRepository pattern)
3. Create discount history repository following existing aggregate patterns  
4. Add discount retrieval methods for reporting

Make the integration tests pass.
Test file: tests/Ordering.FunctionalTests/DiscountIntegrationTests.cs
```

---

## PHASE 5: ORCHESTRATION

### Template 8: Orchestrator Feature Add

```
@Orchestrator

New requirement: Add "Buy One Get One" (BOGO) discount type support.

BOGO rules:
- Buy X items of product A, get Y items of product B at Z% off
- Cannot combine with other product-specific discounts on product B
- Must be same transaction
- Limit one BOGO promotion per order

Use the full TDD workflow:
1. Research feasibility (DomainSpecialist)
2. Plan implementation (Planner)
3. Write tests (tdd-red)
4. Implement (tdd-green)

Please coordinate the subagents and implement this feature.
```

### Template 9: Orchestrator Bug Fix

```
@Orchestrator

I discovered a bug: When multiple discounts are applied, the 50% cap calculation 
is performed on the original price rather than the discounted price.

Please:
1. Research the issue (DomainSpecialist)
2. Plan the fix (Planner)
3. Write a failing test that demonstrates the bug (tdd-red)
4. Fix the implementation (tdd-green)

Use TDD workflow to fix this properly.
```

---

## ADDITIONAL PROMPTS

### Template 10: Code Review Request

```
@Orchestrator

Please review the discount system implementation for:
1. Code quality and SOLID principles
2. Test coverage gaps
3. Performance concerns
4. Edge cases we might have missed
5. Documentation completeness

Provide recommendations for improvements.
```

### Template 11: Refactoring Request

```
@tdd-green

The DiscountCalculator class has grown too large. Please refactor it to:
1. Extract strategy pattern for different discount types
2. Separate business rule validation
3. Improve testability
4. Maintain all existing tests passing

Ensure all tests continue to pass after refactoring.
```

### Template 12: Performance Optimization

```
@DomainSpecialist

Research performance optimization opportunities for the discount calculation:
1. Current algorithm complexity
2. Potential caching strategies
3. Database query optimization for promotion retrieval
4. Batch processing for large orders

Recommend specific improvements with code examples.
```

### Template 13: Documentation Request

```
@Planner

Create comprehensive documentation for the discount system:
1. Architecture overview
2. How to add new discount types
3. Business rules explanation
4. API usage examples
5. Testing strategy

Format as markdown suitable for README.md
```

### Template 14: Debugging Help

```
I'm seeing this error when running tests:
[PASTE ERROR MESSAGE]

Test file: [PATH]
Implementation file: [PATH]

What's causing this and how do I fix it?
```

### Template 15: Quick Feature Add

```
@tdd-green

Add a new property to Discount model:
- MaxUsagePerCustomer: int (nullable)
- If set, limit how many times a customer can use this promotion
- Add validation in DiscountCalculator
- Add tests for this new constraint

Make it a quick addition following existing patterns.
```

---

## TROUBLESHOOTING PROMPTS

### When Agent Doesn't Understand Context

```
Let me provide more context about the codebase:
[EXPLAIN STRUCTURE]

File structure:
- Domain models: [PATH]
- Business logic: [PATH]
- Tests: [PATH]
- Integration: [PATH]

Now please [REPEAT ORIGINAL REQUEST]
```

### When Tests Fail Unexpectedly

```
These tests are failing:
[PASTE TEST OUTPUT]

Test file: [PATH]
Implementation file: [PATH]

Help me understand why they're failing and what needs to be fixed.
```

### When Code Doesn't Compile

```
I'm getting compilation errors:
[PASTE ERRORS]

File: [PATH]

What's wrong and how do I fix it?
```

### When Unsure About Architecture

```
@DomainSpecialist

I'm not sure about the best way to integrate the discount system with:
- [COMPONENT A]
- [COMPONENT B]

What are the architectural considerations and recommended approach?
```

---

## CUSTOMIZATION CHECKLIST

Before presentation, replace these placeholders in all templates:

- [ ] `[SPECIFY PATH]` → Actual file paths in eShop
- [ ] `[SPECIFY NAMESPACE/PATH]` → Actual namespace like `Ordering.Domain.DiscountAggregate`
- [ ] `[TEST PATH]` → Path like `tests/Ordering.UnitTests/`
- [ ] `[IMPLEMENTATION PATH]` → Path like `src/Services/Ordering/Ordering.Domain/`
- [ ] `[EXISTING REPOSITORY PATTERN]` → Actual pattern used in Ordering service
Before presentation, verify these paths match your eShop structure:

- [ ] Domain models: `src/Ordering.Domain/AggregatesModel/DiscountAggregate/`
- [ ] Unit tests: `tests/Ordering.UnitTests/Domain/DiscountAggregateTests/`
- [ ] Integration point: `Ordering.API/Application/Commands/` (review command handlers)
- [ ] Repository pattern: Review `Ordering.Infrastructure/Repositories/` for patterns

All paths above are configured for the modern eShop repository!
| Research & Planning | 1-2 | 10 min |
| Red Phase | 3-4 | 15 min |
| Green Phase | 5-7 | 20 min |
| Orchestration | 8 or 9 | 8 min |
| Q&A / Extras | 10-15 as needed | Flexible |

---

## TIPS FOR USING PROMPTS

1. **Don't just copy-paste blindly**: Read and understand each prompt
2. **Customize with specifics**: Replace placeholders with actual paths
3. **Add context if needed**: If agent seems confused, provide more detail
4. **Be patient**: Complex prompts may take 30-60 seconds to process
5. **Follow up**: If response isn't quite right, ask clarifying questions
6. **Show the prompt**: Let audience see what you're asking
7. **Explain the prompt**: Tell audience why you're asking in this way

---

## EXAMPLE CUSTOMIZED PROMPT

**Before** (Template):
```
@tdd-red
Create test file at: [SPECIFY PATH]
```

**After** (Customized for eShop):
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

Use xUnit framework. Follow the existing test patterns in the codebase.
Create test file at: tests/Ordering.UnitTests/Domain/DiscountAggregateTests/DiscountCalculatorTests.cs
```

Notice how specific paths and framework names are included.
