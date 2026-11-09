# PROMO-5: [BE - Ordering.API, Ordering.Domain, Ordering.Infrastructure] - Persist and total discounts on orders

**User Story:**

As the business, I want every order to record the discount applied to each line item and include it in the order total, so that customers are charged correctly and revenue and promotion reporting are accurate.

**Inputs:**

`POST /api/orders` (header `x-requestid: <guid>`): the existing `CreateOrderRequest` with three new fields.

```json
{
  "userId": "a1b2c3",
  "userName": "alice",
  "city": "Seattle", "street": "1 Main St", "state": "WA", "country": "US", "zipCode": "98101",
  "cardNumber": "1111222233334444", "cardHolderName": "TESTUSER",
  "cardExpiration": "2027-11-09T00:00:00Z", "cardSecurityNumber": "111", "cardTypeId": 1,
  "buyer": "a1b2c3",
  "couponCode": "FALL10",
  "appliedPromotionIds": [1, 3],
  "items": [
    { "id": "…", "productId": 12, "productName": "Trail Backpack 30L", "unitPrice": 89.99, "oldUnitPrice": 0, "quantity": 1, "pictureUrl": "", "discount": 17.99 },
    { "id": "…", "productId": 40, "productName": "Wool Hiking Socks", "unitPrice": 15.00, "oldUnitPrice": 0, "quantity": 3, "pictureUrl": "", "discount": 15.00 }
  ]
}
```

| Field | Type | Where | Notes |
| --- | --- | --- | --- |
| `items[].discount` | decimal | `Ordering.API` `BasketItem` | **New.** The discount for the whole line, not per unit. Defaults to `0`. |
| `couponCode` | string? | `CreateOrderRequest` | **New.** Optional. |
| `appliedPromotionIds` | int[] | `CreateOrderRequest` | **New.** Optional; empty list if missing. |

`discount` is an amount for the whole line because the domain already
treats it that way: the `OrderItem` constructor checks
`unitPrice * units < discount`.

**Outputs:**

- **Create:** `200 OK` (unchanged). `400 BadRequest` when:
  - `x-requestid` is missing (existing behavior)
  - any line has a negative discount or a discount above `unitPrice * quantity` (new; see the AC)
- **Persisted data:**
  - `OrderItems.Discount` gets a real value (the column already exists)
  - `Orders.CouponCode` (nullable) and `Orders.AppliedPromotionIds` are new columns, added by an EF migration in `Ordering.Infrastructure`
- **`GET /api/orders/{id}`:**
  ```json
  {
    "ordernumber": 42,
    "status": "Submitted",
    "subtotal": 134.99,
    "totalDiscount": 32.99,
    "total": 102.00,
    "couponCode": "FALL10",
    "orderitems": [
      { "productname": "Trail Backpack 30L", "units": 1, "unitprice": 89.99, "discount": 17.99, "pictureurl": "" },
      { "productname": "Wool Hiking Socks", "units": 3, "unitprice": 15.00, "discount": 15.00, "pictureurl": "" }
    ]
  }
  ```
- **`GET /api/orders`** (the user's order summaries): `total` is the total after discounts
- **`OrderStatusChangedToPaidIntegrationEvent`:** gains `CouponCode` (string?) and `TotalDiscount` (decimal), so that [PROMO-4](PROMO-4-coupon-codes-and-redemption.md) can record coupon redemptions without querying Ordering. Existing subscribers ignore fields they don't know about.

**Acceptance Criteria:**

* **Given** Ordering.API is running and an authenticated buyer has items in their basket
* **When** an order is created with no `discount` on any item and no coupon (today's client)
    * **Then** the order is created exactly as it is today, with `Discount = 0` on every line
    * **Then** `total` equals `subtotal`, and `totalDiscount` is `0`
* **When** an order is created with a valid `discount` on one or more lines
    * **Then** each `OrderItems.Discount` row stores the discount sent for that line
    * **Then** `Order.GetTotal()` returns `Sum(UnitPrice * Units) - Sum(Discount)`
    * **Then** `GET /api/orders/{id}` returns `subtotal`, `totalDiscount`, `total` and a `discount` on each item, and `total = subtotal - totalDiscount`
    * **Then** `GET /api/orders` returns the discounted `total` for that order
    * **When** the same product appears in two lines
        * **Then** the lines merge into one, keeping the higher discount and adding the units together (existing `Order.AddOrderItem` behavior, unchanged)
* **When** an order is created with an invalid discount
    * **When** a line's discount is negative
        * **Then** the request returns `400 BadRequest` and no order is saved
    * **When** a line's discount is greater than `unitPrice * quantity`
        * **Then** the request returns `400 BadRequest` and no order is saved, and the domain's `OrderingDomainException` is never thrown as an unhandled `500`
* **When** an order is created with `couponCode` and `appliedPromotionIds`
    * **Then** both are saved on the order, and `couponCode` is returned by `GET /api/orders/{id}`
    * **When** that order is later paid
        * **Then** `OrderStatusChangedToPaidIntegrationEvent` carries its `CouponCode` and `TotalDiscount`
* **When** Promotions.API's `/evaluate` endpoint is available and the client's discount for a line differs from the server's result by more than $0.01
    * **Then** the server's discount is saved, not the client's, and a warning is logged with the order's request ID

**Technical Notes:**

* **Interaction history:** This story came from PROMO-5 in the [promotions & recommendations epic](epic-promotions-and-recommendations.md). When we traced checkout, we found the domain already supports discounts but they never reach it. Ordering.API's `BasketItem` has no `Discount` property, `ToOrderItemDTO()` never sets one, and `Order.GetTotal()` and the order summary query both ignore discounts. As a result, every order today has `Discount = 0`. The epic's PROMO-5 also covered changes in the WebApp (sending the discounts, showing savings on the order page). Per this skill's FE/BE rule, those moved to [PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md). An earlier draft said PaymentProcessor charges `Order.GetTotal()`. That turned out to be wrong: PaymentProcessor never reads an amount; it just succeeds or fails depending on its config. So fixing the total changes what's displayed and reported, not a real charge. We also decided to add `CouponCode` and `TotalDiscount` to the paid event, so [PROMO-4](PROMO-4-coupon-codes-and-redemption.md) can record redemptions. Treating `discount` as an amount for the whole line matches the existing domain check, so we didn't introduce a per-unit discount.
* **Existing code to reuse:**
    * `src/Ordering.Domain/AggregatesModel/OrderAggregate/OrderItem.cs:15,23-51`: `Discount` property, constructor validation and `SetNewDiscount()`. Already correct; reuse as is.
    * `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs:71-91`: `AddOrderItem(..., decimal discount, ...)`, including the rule that merges duplicate products and keeps the higher discount.
    * `src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs:185`: `GetTotal()`, today `Sum(Units * UnitPrice)`. This is the single place to subtract discounts.
    * `src/Ordering.API/Application/Models/BasketItem.cs`: needs a `Discount` property added.
    * `src/Ordering.API/Extensions/BasketItemExtensions.cs:13-23`: `ToOrderItemDTO()` needs to copy `Discount`. Called from `src/Ordering.API/Application/Commands/CreateOrderCommand.cs:70`.
    * `src/Ordering.API/Application/Commands/CreateOrderCommandHandler.cs:44`: already passes `item.Discount` into `AddOrderItem`, so no change is needed there once the DTO carries it.
    * `src/Ordering.API/Apis/OrdersApi.cs:118-165,171`: `CreateOrderAsync` and the `CreateOrderRequest` record, where the request validation and the new fields go.
    * `src/Ordering.API/Application/Queries/OrderQueries.cs:26-35,46`: `GetOrderAsync` (uses `GetTotal()`, leaves out item discounts) and `GetOrdersFromUserAsync` (calculates its own total without discounts, so it needs the same subtraction).
    * `src/Ordering.API/Application/Queries/OrderViewModel.cs`: `Orderitem` and `Order` view models that gain `discount`, `subtotal`, `totalDiscount` and `couponCode`.
    * `tests/Ordering.UnitTests/Domain/OrderAggregateTest.cs` and `tests/Ordering.FunctionalTests/OrderingApiTests.cs`: existing test suites to extend.
* **New code (proposal, not yet agreed):**
    * an EF migration in `Ordering.Infrastructure` for `Orders.CouponCode` and `Orders.AppliedPromotionIds` (column type to be decided; see Open Questions)
    * a check in `CreateOrderAsync` that rejects bad discounts with `400` before the command is sent
    * a typed `IPromotionsClient` in Ordering.API for re-checking discounts against `/evaluate`, registered only when Promotions.API is referenced in AppHost

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md). Only the server-side check depends on it. Everything else in this story can ship first.
* **Blocks:** [PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md); [PROMO-4](PROMO-4-coupon-codes-and-redemption.md) (only the redemption handler, which needs `CouponCode` on the paid event)

**Open Questions:**

* How should `AppliedPromotionIds` be stored: as a Postgres `int[]` column, a JSON column, or a separate `OrderPromotions` table? A table is better for analytics; an array is faster to build in the workshop.
* If Promotions.API is down when the order is created, should the order be accepted with the client's discounts (after the bounds check), or rejected? This draft assumes **accept and log**, so checkout never breaks.
