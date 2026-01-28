# Feature Specification: Promotional Discount System

## Overview
Implement a flexible promotional discount engine for an e-commerce order processing system that supports multiple discount types, business rules, and tracking capabilities.

---

## Business Context
E-commerce platforms need sophisticated discount capabilities to run promotions, reward customers, and compete effectively. The system must handle complex business rules while preventing abuse and maintaining profitability.

---

## Functional Requirements

### FR1: Discount Types
The system SHALL support the following discount types:

#### 1.1 Percentage Discount
- Apply a percentage reduction to eligible items
- Range: 1% to 99%
- Example: "20% off"

#### 1.2 Fixed Amount Discount
- Apply a fixed dollar amount reduction
- Minimum: $0.01
- Example: "$10 off"

#### 1.3 Volume Discount (Tiered)
- Apply discount based on quantity purchased
- Example: "Buy 5+ items, get 10% off"
- Multiple tiers possible: 5-9 items (10% off), 10+ items (15% off)

#### 1.4 Category-Specific Discount
- Apply discount only to items in specific categories
- Example: "25% off Electronics"
- Support multiple categories per promotion

#### 1.5 First-Time Customer Discount
- Apply discount for customers making first purchase
- Typically percentage-based
- Cannot combine with other customer-specific discounts

### FR2: Promotion Configuration
Each promotion SHALL have:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| PromotionId | GUID/String | Yes | Unique identifier |
| Name | String | Yes | Display name |
| DiscountType | Enum | Yes | Type from FR1 |
| DiscountValue | Decimal | Yes | Percentage or amount |
| StartDate | DateTime | Yes | Promotion start |
| EndDate | DateTime | Yes | Promotion end |
| MinimumOrderAmount | Decimal | No | Minimum order total required |
| MaximumDiscount | Decimal | No | Cap on discount amount |
| ApplicableCategories | List<String> | No | Category restrictions |
| ExcludedCategories | List<String> | No | Categories to exclude |
| IsActive | Boolean | Yes | Enable/disable flag |
| Priority | Integer | Yes | Application order (1=highest) |

### FR3: Business Rules

#### Rule 1: Maximum Discount Cap
- Total discount from all promotions SHALL NOT exceed 50% of order subtotal
- If cap would be exceeded, apply highest-priority discounts first until cap reached

#### Rule 2: Discount Stacking
- Multiple promotions CAN be applied to a single order
- Category-specific discounts stack with general discounts
- Customer-specific discounts DO NOT stack with other customer-specific discounts

#### Rule 3: Time-Bound Enforcement
- Promotions only apply if current time is between StartDate and EndDate
- Expired promotions SHALL be ignored

#### Rule 4: Minimum Order Amount
- If promotion has MinimumOrderAmount, order subtotal must meet or exceed it
- Check performed BEFORE applying any discounts

#### Rule 5: Category Exclusions
- Items in ExcludedCategories SHALL NOT receive discount
- Exclusions take precedence over inclusions

#### Rule 6: Maximum Discount per Promotion
- If MaximumDiscount is set, discount from that promotion cannot exceed it
- Example: 20% off with $50 max = On $500 order, discount is $50, not $100

### FR4: Calculation Process
The discount calculation SHALL follow this sequence:

1. **Validate Order**: Ensure order has items and valid pricing
2. **Filter Active Promotions**: Get promotions where:
   - IsActive = true
   - Current date/time within StartDate and EndDate
   - MinimumOrderAmount <= order subtotal (if specified)
3. **Sort by Priority**: Order promotions by Priority (ascending = higher priority)
4. **Apply Discounts**: For each promotion in priority order:
   - Calculate discount amount for eligible items
   - Apply MaximumDiscount cap if specified
   - Check if total discounts would exceed 50% cap
   - If within cap, apply discount; otherwise skip or apply partial
5. **Return Result**: Discount details and final order total

### FR5: Discount Application Tracking
The system SHALL record:

| Field | Description |
|-------|-------------|
| OrderId | Order identifier |
| PromotionId | Applied promotion identifier |
| PromotionName | Promotion display name |
| DiscountAmount | Actual discount applied |
| AppliedAt | Timestamp of application |
| ItemsAffected | Count or list of items |

---

## Non-Functional Requirements

### NFR1: Performance
- Discount calculation SHALL complete in < 100ms for orders with up to 50 items
- Support up to 20 active promotions without performance degradation

### NFR2: Accuracy
- All monetary calculations SHALL use decimal precision (no floating point)
- Rounding: Always round to 2 decimal places, using banker's rounding

### NFR3: Extensibility
- Design SHALL support adding new discount types without modifying core calculation engine
- Use strategy or plugin pattern

### NFR4: Testability
- All business logic SHALL be unit testable
- No direct dependencies on database or external services in calculation engine

### NFR5: Maintainability
- Code SHALL follow SOLID principles
- Business rules SHALL be clearly documented in code comments
- Use domain-driven design concepts where appropriate

---

## Data Models

### Discount (Entity)
```
Discount
- PromotionId: string
- Name: string
- DiscountType: DiscountType enum
- DiscountValue: decimal
- StartDate: DateTime
- EndDate: DateTime
- MinimumOrderAmount: decimal?
- MaximumDiscount: decimal?
- ApplicableCategories: List<string>
- ExcludedCategories: List<string>
- IsActive: bool
- Priority: int
```

### DiscountType (Enum)
```
DiscountType
- PercentageDiscount = 1
- FixedAmountDiscount = 2
- VolumeDiscount = 3
- CategoryDiscount = 4
- FirstTimeCustomerDiscount = 5
```

### AppliedDiscount (Value Object)
```
AppliedDiscount
- PromotionId: string
- PromotionName: string
- DiscountAmount: decimal
- AppliedAt: DateTime
- ItemCount: int
```

### DiscountCalculationResult (Value Object)
```
DiscountCalculationResult
- OriginalAmount: decimal
- TotalDiscount: decimal
- FinalAmount: decimal
- AppliedDiscounts: List<AppliedDiscount>
- SkippedPromotions: List<string> // Promotions not applied due to rules
```

---

## Test Scenarios

### Scenario 1: Single Percentage Discount
**Given**: Order of $100, Active promotion: 20% off
**When**: Calculate discounts
**Then**: Discount = $20, Final = $80

### Scenario 2: Multiple Discounts with Stacking
**Given**: Order of $200
- Promotion A: 15% off (priority 1)
- Promotion B: 10% off (priority 2)
**When**: Calculate discounts
**Then**: 
- Discount A = $30 (15% of $200)
- Discount B = $20 (10% of $200)
- Total = $50 (25% total, within cap)
- Final = $150

### Scenario 3: Discount Cap Enforcement
**Given**: Order of $100
- Promotion A: 30% off (priority 1)
- Promotion B: 25% off (priority 2)
**When**: Calculate discounts
**Then**:
- Discount A = $30 (applied)
- Discount B = $20 (capped at $50 total = 50%)
- Total = $50
- Final = $50

### Scenario 4: Minimum Order Not Met
**Given**: Order of $40, Promotion: 20% off with $50 minimum
**When**: Calculate discounts
**Then**: No discount applied, Final = $40

### Scenario 5: Expired Promotion
**Given**: Order today, Promotion: 20% off (ended yesterday)
**When**: Calculate discounts
**Then**: No discount applied

### Scenario 6: Category-Specific Discount
**Given**: Order with:
- Item A: Electronics, $100
- Item B: Clothing, $50
- Promotion: 20% off Electronics
**When**: Calculate discounts
**Then**: 
- Discount = $20 (only on Item A)
- Final = $130

### Scenario 7: Volume Discount
**Given**: Order with 6 items @ $10 each ($60), Promotion: Buy 5+, get 10% off
**When**: Calculate discounts
**Then**: Discount = $6, Final = $54

### Scenario 8: Maximum Discount Cap
**Given**: Order of $1000, Promotion: 20% off with $100 max discount
**When**: Calculate discounts
**Then**: Discount = $100 (not $200), Final = $900

### Scenario 9: Category Exclusion
**Given**: Order with:
- Item A: Electronics, $100
- Item B: Sale Items, $50
- Promotion: 20% off all, excluding Sale Items
**When**: Calculate discounts
**Then**: Discount = $20 (only on Item A), Final = $130

### Scenario 10: First-Time Customer
**Given**: New customer, Order $100, Promotion: 15% off first purchase
**When**: Calculate discounts
**Then**: Discount = $15, Final = $85

### Scenario 11: No Applicable Discounts
**Given**: Order $50, All promotions require $100 minimum
**When**: Calculate discounts
**Then**: No discount, Final = $50

### Scenario 12: Priority-Based Application
**Given**: Order $100
- Promotion A: $30 off (priority 2)
- Promotion B: 25% off (priority 1)
**When**: Calculate discounts
**Then**:
- Apply B first = $25
- Apply A next = $30
- Check cap: $55 > $50, so cap B at $20
- Final = $50

---

## Integration Points

### Input: Order Object
```
Order
- OrderId: string
- CustomerId: string
- IsFirstPurchase: bool
- Items: List<OrderItem>
  - OrderItem:
    - ProductId: string
    - CategoryId: string
    - Quantity: int
    - UnitPrice: decimal
- Subtotal: decimal (calculated from items)
```

### Output: Enhanced Order Object
```
Order (updated with)
- AppliedDiscounts: List<AppliedDiscount>
- TotalDiscount: decimal
- FinalTotal: decimal
```

---

## Error Handling

### Validation Errors
- Invalid discount percentage (< 0 or > 100)
- Invalid dates (EndDate before StartDate)
- Negative discount values
- Negative order amounts

### Business Rule Violations
- Log but don't fail: Promotions skipped due to caps
- Return information about why promotions weren't applied

---

## Reporting Requirements
Support queries for:
1. Most used promotions (by application count)
2. Total discount amount by promotion
3. Average discount per order
4. Promotion effectiveness (conversion rate)

---

## Future Enhancements (Out of Scope)
- Coupon code system
- Loyalty points integration
- Dynamic pricing based on inventory
- Personalized AI-driven discounts
- A/B testing framework
