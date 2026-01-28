# Feature: Promotional Discount System - Findings

## Analysis

### Current Architecture Assessment

The eShop application follows a microservices architecture with the following relevant components:

1. **Basket.API**: Manages shopping cart state in Redis, handles basket items before checkout
   - Current model: `BasketItem` with basic pricing (`UnitPrice`, `OldUnitPrice`)
   - No discount calculation logic currently exists

2. **Ordering.API**: Processes orders using DDD patterns with aggregates and domain events
   - Current model: `OrderItem` already has a `Discount` property (decimal, fixed amount)
   - Order creation flow: `WebApp` → `Basket.API` → `Ordering.API` via integration events
   - Uses Command/Query pattern with MediatR

3. **Catalog.API**: Manages product catalog with categories
   - `CatalogItem` has `CatalogTypeId` (category) and `CatalogBrandId`
   - Price stored as `decimal` (good precision for monetary values)

4. **Integration Events**: Services communicate asynchronously via RabbitMQ
   - Example: `OrderStartedIntegrationEvent` triggers basket cleanup

5. **Database**: PostgreSQL with EF Core, "ordering" schema with migrations

### Key Findings

#### Strengths for Discount Implementation
- **Decimal precision**: Already uses `decimal` type for monetary values (meets NFR2)
- **Existing discount field**: `OrderItem.Discount` exists but is simple (just a fixed amount)
- **DDD patterns**: Well-structured aggregates (Order, Buyer) make business rule enforcement natural
- **Event-driven**: Integration events allow discount application to trigger analytics/reporting
- **Separation of concerns**: Microservices architecture allows isolated discount service
- **Category support**: Catalog already has category taxonomy for category-specific discounts

#### Challenges & Constraints
- **Current discount is order-item-level**: Exists on `OrderItem`, not as separate entities
- **No promotion tracking**: No entity to store promotional rules or application history
- **Buyer tracking limited**: `Buyer` aggregate exists but no "first purchase" flag
- **Basket → Order transformation**: Discount calculation timing is critical (basket vs. order creation)
- **Performance requirement**: <100ms calculation for 50 items × 20 promotions = tight deadline
- **No category data in basket**: `BasketItem` doesn't store category; requires catalog lookup

### Integration Points

#### 1. Order Creation Flow
```
WebApp → BasketState.CheckoutAsync()
  ↓
OrderingService.CreateOrder(CreateOrderRequest)
  ↓
Ordering.API: CreateOrderCommand
  ↓
CreateOrderCommandHandler: order.AddOrderItem(discount)
  ↓
Order saved to DB
```

**Discount Calculation Insertion Point**: Before `order.AddOrderItem()` in `CreateOrderCommandHandler`

#### 2. Data Flow for Discount Calculation
- **Input**: `List<BasketItem>` from checkout
- **Enrichment Needed**: 
  - Category info from Catalog.API
  - Customer purchase history from Ordering.API
  - Active promotions from new Promotion store
- **Output**: Calculated discounts per item + order-level summary

#### 3. Database Schema Changes Required
New tables in "ordering" schema:
- `promotions`: Stores promotion configurations
- `applied_discounts`: Tracks which promotions were applied to which orders
- Possible: `buyer` table extension for first-purchase flag

---

## Feasibility

### Technical Feasibility: **HIGH**

#### Performance Analysis
- **Requirement**: <100ms for 50 items × 20 promotions
- **Calculation complexity**: O(items × promotions) = 1,000 iterations worst case
- **Feasibility**: 
  - In-memory calculation of 1,000 iterations is trivial (<1ms)
  - Database queries for promotions, categories, buyer history = primary bottleneck
  - **Mitigation**: Cache active promotions (Redis), batch category lookups
  - **Estimate**: 10-30ms achievable with caching

#### Accuracy
- **Requirement**: Decimal precision, banker's rounding to 2 places
- **Current state**: Already uses `decimal` throughout
- **Feasibility**: **FULLY FEASIBLE** - C# `decimal` type + `Math.Round(value, 2, MidpointRounding.ToEven)`

#### Extensibility
- **Requirement**: Strategy pattern for new discount types
- **Approach**: Interface-based discount calculators + DI registration
- **Feasibility**: **FULLY FEASIBLE** - Aligns with current DDD/SOLID practices

#### Testability
- **Requirement**: Unit testable, no external dependencies in calculation engine
- **Approach**: Pure domain services + repository abstractions
- **Feasibility**: **FULLY FEASIBLE** - Existing test structure supports this (see `Ordering.UnitTests`)

### Business Feasibility: **HIGH**

- **50% discount cap**: Enforceable in calculation logic
- **Stacking rules**: Complex but manageable with priority sorting
- **Category exclusions**: Requires catalog integration but straightforward
- **First-time customer**: Requires buyer history query (1 DB call per checkout)

### Integration Feasibility: **MEDIUM**

- **Challenge**: Basket doesn't have category info
  - **Solution**: Query Catalog.API during checkout or enrich basket items
- **Challenge**: First-purchase detection requires order history
  - **Solution**: Query `OrderRepository` or add flag to `Buyer` aggregate
- **Challenge**: Real-time availability of promotions
  - **Solution**: Cache in Redis with invalidation

---

## Proposed Solutions

### Solution A: Discount Service within Ordering.API (Domain Service Pattern)

#### Architecture
```
Ordering.API
├── Domain
│   ├── AggregatesModel
│   │   ├── OrderAggregate (existing)
│   │   └── PromotionAggregate (new)
│   │       ├── Promotion.cs (entity)
│   │       ├── IPromotionRepository.cs
│   │       └── DiscountType.cs (enum)
│   └── Services (new)
│       ├── IDiscountCalculationService.cs
│       ├── DiscountCalculationService.cs
│       └── DiscountStrategies
│           ├── IDiscountStrategy.cs
│           ├── PercentageDiscountStrategy.cs
│           ├── FixedAmountDiscountStrategy.cs
│           ├── VolumeDiscountStrategy.cs
│           ├── CategoryDiscountStrategy.cs
│           └── FirstTimeCustomerDiscountStrategy.cs
├── Application
│   └── Commands
│       └── CreateOrderCommandHandler.cs (modified)
└── Infrastructure
    ├── Repositories
    │   └── PromotionRepository.cs
    └── EntityConfigurations
        └── PromotionEntityTypeConfiguration.cs
```

#### Implementation Approach
1. **Promotion Aggregate**: New aggregate root for discount rules
2. **Domain Service**: `DiscountCalculationService` coordinates discount application
3. **Strategy Pattern**: Each discount type = separate strategy class
4. **Integration**: Injected into `CreateOrderCommandHandler`, called before `AddOrderItem()`

#### Pros
- ✅ **Domain-driven**: Promotions are a core business concept (first-class aggregate)
- ✅ **Low latency**: No network calls between services
- ✅ **Transactional**: Discount calculation + order creation in single transaction
- ✅ **Reuses existing patterns**: Fits existing DDD structure perfectly
- ✅ **Testability**: Pure domain logic, easily unit testable
- ✅ **Performance**: Direct database access, no HTTP overhead

#### Cons
- ❌ **Ordering.API grows larger**: More responsibilities (orders + promotions)
- ❌ **Coupled to catalog**: Needs to query Catalog.API for category info
- ❌ **Limited reusability**: If basket preview needs discounts, must duplicate or expose endpoint

#### Risk Assessment
| Risk | Severity | Mitigation |
|------|----------|------------|
| Catalog.API coupling | Medium | Use resilient HTTP client, cache category data |
| Ordering.API complexity | Low | Well-structured domain services keep it maintainable |
| Performance (50 items) | Low | In-process calculation + caching meets <100ms easily |

---

### Solution B: Standalone Discount.API Microservice

#### Architecture
```
Discount.API (new service)
├── Domain
│   ├── Promotion.cs
│   ├── DiscountCalculationResult.cs
│   └── Services
│       ├── DiscountCalculationService.cs
│       └── DiscountStrategies (same as Solution A)
├── Application
│   ├── Commands
│   │   └── CalculateDiscountsCommand.cs
│   └── Queries
│       ├── GetActivePromotionsQuery.cs
│       └── GetPromotionByIdQuery.cs
├── APIs
│   ├── DiscountApi.cs (calculate, preview, manage promotions)
│   └── PromotionManagementApi.cs
└── Infrastructure
    └── PromotionRepository.cs

Ordering.API (modified)
└── Application
    └── Commands
        └── CreateOrderCommandHandler.cs
            └── Calls Discount.API via HTTP

Basket.API (modified, optional)
└── APIs
    └── BasketApi.cs
        └── Preview discounts for basket (optional feature)
```

#### Implementation Approach
1. **New microservice**: Discount.API owns all promotion logic
2. **HTTP API**: Exposes `POST /api/discounts/calculate` endpoint
3. **Called by Ordering.API**: During order creation, makes HTTP request to calculate discounts
4. **Shared responses**: Returns `DiscountCalculationResult` with applied discounts

#### Pros
- ✅ **Single Responsibility**: Discount.API only manages promotions
- ✅ **Reusability**: Basket.API can preview discounts, WebApp can show savings
- ✅ **Independent scaling**: Can scale discount service separately if needed
- ✅ **Domain isolation**: Promotion domain is fully isolated
- ✅ **Team autonomy**: Different team can own promotion features

#### Cons
- ❌ **Network latency**: HTTP call adds 5-20ms overhead
- ❌ **Distributed transaction complexity**: Discounts calculated in separate service
- ❌ **Failure handling**: What if Discount.API is down? (needs fallback)
- ❌ **Increased infrastructure**: Another service to deploy, monitor, maintain
- ❌ **Eventual consistency**: Promotion changes might not be immediately visible

#### Risk Assessment
| Risk | Severity | Mitigation |
|------|----------|------------|
| Network latency | Medium | Cache frequently used promotions, use gRPC instead of HTTP |
| Service availability | High | Implement circuit breaker, fallback to no discounts |
| Complexity | Medium | Worth it for large-scale systems, overkill for current eShop |
| Performance (100ms) | Medium | Requires aggressive caching + fast network |

---

### Solution C: Hybrid - Shared Library + Service Facade

#### Architecture
```
eShop.Discounts (new shared library - class library project)
└── Domain
    ├── Promotion.cs
    ├── AppliedDiscount.cs
    ├── DiscountCalculationResult.cs
    └── Services
        ├── IDiscountCalculationService.cs
        └── DiscountStrategies (all strategies)

Ordering.API (uses library)
├── Domain
│   └── AggregatesModel
│       └── PromotionAggregate
│           ├── Promotion.cs (EF entity, inherits from shared)
│           └── IPromotionRepository.cs
└── Application
    └── Commands
        └── CreateOrderCommandHandler.cs (uses shared service)

Discount.API (optional, for management UI)
└── APIs
    └── PromotionManagementApi.cs (CRUD for promotions)

Basket.API (optional)
└── Uses shared library for discount preview
```

#### Implementation Approach
1. **Shared library**: Core discount calculation logic in `eShop.Discounts` class library
2. **Ordering.API**: References library, owns promotion data (persists to DB)
3. **Optional Management API**: Separate service for promotion CRUD, reporting
4. **Reuse across services**: Basket can preview, Ordering applies, Analytics reports

#### Pros
- ✅ **Code reuse**: Calculation logic shared across services
- ✅ **Performance**: No network overhead for calculation
- ✅ **Consistency**: Same logic everywhere (no drift)
- ✅ **Flexibility**: Can add Discount.API later for management without refactoring calculation
- ✅ **Testability**: Library is pure logic, highly testable

#### Cons
- ❌ **Versioning complexity**: Library updates affect multiple services
- ❌ **Deployment coupling**: If library changes, all services must redeploy
- ❌ **Shared database access**: Promotions table accessed by multiple services (conflicts)
- ❌ **Anti-pattern risk**: Can lead to "shared database" anti-pattern

#### Risk Assessment
| Risk | Severity | Mitigation |
|------|----------|------------|
| Tight coupling | Medium | Use semantic versioning, backward compatibility |
| Shared data access | High | Designate Ordering.API as owner, others read-only via cache |
| Deployment coordination | Low | Use package versioning, deploy incrementally |

---

## Recommendation

### **RECOMMENDED SOLUTION: Solution A - Discount Service within Ordering.API**

#### Rationale

1. **Performance**: Meets <100ms requirement easily (no network overhead)
2. **Simplicity**: Fits naturally into existing Ordering.API domain model
3. **DDD Alignment**: Promotion is a core concept in the ordering bounded context
4. **Transactional Integrity**: Discount calculation + order creation in single DB transaction
5. **Current Scale**: eShop doesn't need microservice complexity of Solution B yet
6. **Existing Patterns**: Matches how eShop already structures domain services

#### Why Not Solution B?
- **Overkill**: eShop is a reference architecture, not Netflix-scale
- **Latency risk**: HTTP overhead makes 100ms target risky
- **Complexity**: Not justified by current requirements

#### Why Not Solution C?
- **Premature abstraction**: No current need for shared library
- **Database coupling risk**: Multiple services accessing promotions table violates bounded context
- **Deployment headaches**: Library versioning adds overhead

---

## Implementation Approach (Solution A)

### Phase 1: Domain Model (Week 1)
1. Create `Promotion` aggregate with all properties from spec
2. Create `AppliedDiscount` value object
3. Create `DiscountCalculationResult` value object
4. Create `IPromotionRepository` interface
5. Add EF Core configuration and migration

### Phase 2: Calculation Engine (Week 2)
1. Create `IDiscountStrategy` interface
2. Implement 5 strategy classes (Percentage, Fixed, Volume, Category, FirstTime)
3. Create `DiscountCalculationService` domain service
4. Implement business rules (50% cap, stacking, priority, exclusions)
5. Unit tests for each strategy + service

### Phase 3: Integration (Week 3)
1. Modify `CreateOrderCommandHandler` to inject `IDiscountCalculationService`
2. Add enrichment: query Catalog.API for categories, query Buyer for first-purchase
3. Call discount service before `order.AddOrderItem()`
4. Map results to `OrderItem.Discount`
5. Store applied discounts (new `AppliedDiscounts` table)

### Phase 4: Infrastructure (Week 4)
1. Implement `PromotionRepository`
2. Add caching layer (Redis) for active promotions
3. Create promotion management API endpoints (CRUD)
4. Add integration tests
5. Performance testing (verify <100ms)

### Phase 5: Observability & Reporting (Week 5)
1. Add domain event: `DiscountsAppliedDomainEvent`
2. Create integration event: `OrderDiscountsAppliedIntegrationEvent`
3. Add logging/metrics for discount calculation
4. Create queries for reporting (most used promotions, total discounts, etc.)

---

## Technical Risks and Mitigations

### Risk 1: Performance Degradation
**Scenario**: Catalog API calls for 50 items slow down checkout

**Mitigation**:
- Batch category lookups: single HTTP call for all product IDs
- Cache category mappings in Redis (TTL: 1 hour)
- Fallback: if catalog lookup fails, skip category-specific discounts

### Risk 2: Buyer First-Purchase Detection Race Condition
**Scenario**: Concurrent orders from new buyer both get first-time discount

**Mitigation**:
- Use transaction isolation level `Serializable` for buyer creation
- Add unique constraint on buyer orders table
- Alternative: Check count of *completed* orders, not just created

### Risk 3: Promotion Rule Complexity
**Scenario**: Business rules become too complex for strategy pattern

**Mitigation**:
- Start simple: implement spec requirements only
- Document extensibility points
- Future: Consider rule engine (e.g., NRules) if complexity grows

### Risk 4: Testing Edge Cases
**Scenario**: Combinatorial explosion of discount rules

**Mitigation**:
- Property-based testing for discount calculations
- Test scenarios from spec (12 scenarios)
- Fuzz testing for cap enforcement

---

## Data Model Changes

### New Tables

#### `ordering.promotions`
```sql
CREATE TABLE ordering.promotions (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    discount_type INT NOT NULL,
    discount_value DECIMAL(18,2) NOT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    minimum_order_amount DECIMAL(18,2) NULL,
    maximum_discount DECIMAL(18,2) NULL,
    applicable_categories TEXT[] NULL,
    excluded_categories TEXT[] NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    priority INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

#### `ordering.applied_discounts`
```sql
CREATE TABLE ordering.applied_discounts (
    id UUID PRIMARY KEY,
    order_id INT NOT NULL REFERENCES ordering.orders(id),
    promotion_id UUID NOT NULL REFERENCES ordering.promotions(id),
    promotion_name VARCHAR(200) NOT NULL,
    discount_amount DECIMAL(18,2) NOT NULL,
    applied_at TIMESTAMP NOT NULL DEFAULT NOW(),
    item_count INT NOT NULL
);
```

### Modified Tables

#### `ordering.buyers` (optional enhancement)
Add column for first-purchase tracking:
```sql
ALTER TABLE ordering.buyers 
ADD COLUMN first_purchase_completed BOOLEAN DEFAULT FALSE;
```

---

## Performance Estimates

### Discount Calculation Breakdown (50 items, 20 promotions)

| Operation | Time (ms) | Notes |
|-----------|-----------|-------|
| Fetch active promotions (cached) | 2 | Redis cache hit |
| Fetch buyer order count | 5 | Single DB query with index |
| Fetch categories for 50 items (cached) | 3 | Batch lookup, Redis cache |
| Calculate discounts (in-memory) | 1 | 1,000 iterations, trivial |
| Filter by business rules | 1 | In-memory sorting + filtering |
| Build result object | 1 | Object construction |
| **TOTAL** | **13ms** | ✅ Well under 100ms target |

### Worst-Case Scenario (cache miss, category lookup)
| Operation | Time (ms) |
|-----------|-----------|
| Fetch promotions from DB | 10 |
| Fetch buyer order count | 5 |
| HTTP call to Catalog.API | 25 |
| Calculation | 3 |
| **TOTAL** | **43ms** |

**Conclusion**: Performance requirement is comfortably achievable.

---

## Dependencies on Other Services

### Catalog.API
- **Endpoint needed**: `GET /api/catalog/items/categories?ids=1,2,3...`
- **Purpose**: Batch fetch category IDs for discount eligibility
- **Caching strategy**: Cache category mappings in Redis (1-hour TTL)

### Redis
- **Keys**:
  - `promotions:active` → List of active promotions
  - `categories:product:{productId}` → Category ID
- **Invalidation**: On promotion update, clear `promotions:active`

### No new external dependencies required

---

## Open Questions for Product Owner

1. **Promotion Management UI**: Who manages promotions? Admin panel needed?
2. **Reporting**: What analytics are needed? Real-time or batch?
3. **Discount Preview**: Should basket show discount estimates before checkout?
4. **Expired Promotions**: Soft delete or hard delete?
5. **Audit Trail**: Need full history of promotion changes?
6. **First Purchase**: Count completed orders only, or include cancelled orders?

---

## Next Steps

1. **Review & Approval**: Present findings to technical lead + product owner
2. **Planner Agent**: Break down implementation into detailed tasks
3. **TDD Cycle**: Red-Green-Refactor for domain logic
4. **Integration**: Wire up to Ordering.API command handler
5. **Testing**: Functional tests + performance benchmarks

---

## Conclusion

The Promotional Discount System is **highly feasible** to implement within the existing eShop architecture. Solution A (domain service within Ordering.API) provides the best balance of:
- ✅ Performance (<100ms achievable)
- ✅ Maintainability (follows existing patterns)
- ✅ Extensibility (strategy pattern for new discount types)
- ✅ Testability (pure domain logic)

The existing infrastructure (decimal precision, DDD patterns, event-driven architecture) provides a strong foundation. Primary integration points are well-defined, and performance risks are mitigated through caching strategies.

**Estimated Effort**: 4-5 weeks for full implementation + testing  
**Confidence Level**: HIGH (90%)  
**Technical Debt**: LOW (aligns with current architecture)
