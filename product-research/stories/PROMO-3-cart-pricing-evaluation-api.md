# PROMO-3: [BE - Promotions.API] - Cart pricing evaluation API

**User Story:**

As a shopper, I want the best available discounts worked out for my cart automatically, so that I always pay the lowest correct price without having to search for deals.

**Inputs:**

`POST /api/promotions/evaluate` (authenticated; `buyerId` is taken from the token's `sub` claim, not the body):
```json
{
  "couponCode": "FALL10",
  "items": [
    { "productId": 12, "catalogTypeId": 3, "catalogBrandId": 2, "unitPrice": 89.99, "quantity": 1 },
    { "productId": 40, "catalogTypeId": 1, "catalogBrandId": 5, "unitPrice": 15.00, "quantity": 3 }
  ]
}
```

| Field | Rules |
|---|---|
| `items` | 1–100 lines, each with a distinct `productId` |
| `unitPrice` | ≥ 0. The caller supplies it from the catalog; this service doesn't call Catalog.API. |
| `quantity` | ≥ 1 |
| `couponCode` | Optional; case-insensitive |

**Outputs:**

`200 OK`:
```json
{
  "subtotal": 134.99,
  "totalDiscount": 32.99,
  "total": 102.00,
  "lines": [
    { "productId": 12, "lineTotal": 89.99, "discount": 17.99, "appliedPromotionIds": [1] },
    { "productId": 40, "lineTotal": 45.00, "discount": 15.00, "appliedPromotionIds": [3] }
  ],
  "appliedPromotions": [
    { "id": 1, "name": "Fall Hiking Sale", "badgeLabel": "-20%", "savings": 17.99 },
    { "id": 3, "name": "Buy 2 Get 1 Socks", "badgeLabel": "Buy 2 Get 1", "savings": 15.00 }
  ],
  "coupon": { "code": "FALL10", "status": "Invalid", "reason": "Expired" },
  "nextThreshold": { "promotionId": 4, "minOrderTotal": 150.00, "amountRemaining": 48.00, "reward": 10.00 }
}
```

- `coupon.status` is `Applied`, `Invalid` or `NotProvided`
- `coupon.reason` is set only when the status is `Invalid`: `NotFound`, `Expired`, `UsageLimitReached`, `PerUserLimitReached` or `MinOrderNotMet`
- `nextThreshold` is the closest `AmountOffOrder` promotion the cart hasn't reached yet, or `null`. It's used by the cart's "add $X more" message.
- `400 ValidationProblem` for invalid items
- `401` without a token

The coupon check itself (limits, expiry) is defined in [PROMO-4](PROMO-4-coupon-codes-and-redemption.md). This endpoint only reports the result.

**Evaluation algorithm:**

1. **Candidates:** promotions whose `EffectiveStatus = Active`. A promotion that requires a coupon is included only if its coupon was `Applied`.
2. **Matching:** a line matches a promotion if any of its targets match:
    * `All`, or
    * `Product == productId`, or
    * `Brand == catalogBrandId`, or
    * `Type == catalogTypeId`
3. **Line discounts:**
    * `PercentOff`: `lineTotal * value / 100`
    * `BuyXGetY`: free units = `floor(quantity / (buy + get)) * get`, and the discount = free units × `unitPrice`
4. **Resolving conflicts on a line:**
    * Among non-stackable matches, pick the one with the largest discount; if two are equal, the higher `Priority` wins, then the lower `Id`
    * Stackable matches are then added on top: percentages are applied to the remaining amount first, then fixed amounts
5. **Order-level:**
    * Apply the best `AmountOffOrder` whose `MinOrderTotal` is at most the subtotal after line discounts
    * Split it across the lines in proportion to each line's remaining total
    * Any rounding remainder goes to the largest line
6. **Caps:** a line's discount can never be more than its total (the same rule as Ordering's `OrderItem`)
7. **Rounding:** round each line's discount to 2 decimals using `MidpointRounding.ToEven`, then `totalDiscount = Σ line discounts` and `total = subtotal − totalDiscount`

**Acceptance Criteria:**

* **Given** the seeded promotions (one `PercentOff` on type 3, one `BuyXGetY` 2+1 on product 40, one `AmountOffOrder` of $10 over $150) and an authenticated caller
* **When** the cart has no matching items and no coupon
    * **Then** every line's `discount = 0`, `total = subtotal`, `appliedPromotions` is empty and `coupon.status = NotProvided`
* **When** one line matches a single `PercentOff`
    * **Then** that line's discount equals `round(lineTotal × 0.20, 2)`
* **When** product 40 has quantity 3
    * **Then** 1 unit is free; with quantity 5, still 1 is free; with quantity 6, 2 are free
* **When** two non-stackable promotions match the same line
    * **Then** only the one that saves more is applied
    * **When** they save the same amount
        * **Then** the higher `Priority` is applied
* **When** the subtotal after line discounts is at least $150
    * **Then** $10 is split across the lines, the parts add up to exactly $10.00, and no line goes below 0
* **When** the subtotal is below the threshold
    * **Then** `nextThreshold.amountRemaining = 150 − subtotal after line discounts`
* **When** a promotion has expired or is `Draft`
    * **Then** it is never applied
* **When** the same request is sent twice
    * **Then** the responses are identical (no side effects; coupons aren't used up here)

**Technical Notes:**

* **Interaction history:** This story is PROMO-3 from the [promotions & recommendations epic](epic-promotions-and-recommendations.md). When we traced the code, we found that pricing happens in the WebApp's `BasketState`, not in Basket.API, which stores only `product_id` and `quantity`. So the caller sends `unitPrice`, `catalogTypeId` and `catalogBrandId` (which the WebApp already has from Catalog). This keeps Promotions.API free of any call to Catalog. Ordering checks the result again on the server ([PROMO-5](PROMO-5-persist-discounts-on-orders.md)), so a client sending fake prices doesn't create a hole. The WebApp work was split into [PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md).
* **Existing code to reuse:**
    * `src/WebAppComponents/Catalog/CatalogItem.cs`: the WebApp's `CatalogItem` already carries `CatalogBrandId` and `CatalogTypeId`, so the caller can fill in the request without an extra lookup
    * `src/Ordering.Domain/AggregatesModel/OrderAggregate/OrderItem.cs:30-32`: the cap rule (a discount can't exceed the line total) to match exactly
    * `tests/Basket.UnitTests` and `tests/Ordering.UnitTests`: the xUnit project layout to copy for `tests/Promotions.UnitTests`
* **New code (proposal):**
    * a `PricingEngine` class that only calculates, with no database access, so it can be unit tested on its own
    * a `PromotionCache` holding active promotions in memory for 30s
    * an `EvaluateEndpoint` that ties them together
    * The coupon lookup is behind an `ICouponValidator`, built in [PROMO-4](PROMO-4-coupon-codes-and-redemption.md); stub it to `NotProvided` until then

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-1](PROMO-1-promotions-service-scaffold.md), [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md)
* **Blocks:** [PROMO-5](PROMO-5-persist-discounts-on-orders.md) (server-side check only), [PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md)
* **Relates to:** [PROMO-4](PROMO-4-coupon-codes-and-redemption.md)

**Open Questions:**

* Should anonymous shoppers be able to call `/evaluate` (for sale prices before sign-in), or only signed-in users? The cart page requires sign-in today (`[Authorize]` on `CartPage.razor`), so this draft requires authentication.
* For `BuyXGetY` on a stackable line, does the free unit come off before or after percentage discounts? This draft applies `BuyXGetY` as a non-stackable line promotion only.
