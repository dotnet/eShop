# PROMO-4: [BE - Promotions.API] - Coupon codes and redemption tracking

**User Story:**

As a marketer, I want to issue coupon codes with usage limits and have each redemption recorded when an order is paid, so that I can run targeted offers without them being overused.

**Inputs:**

Coupon model:

| Field | Type | Rules |
| --- | --- | --- |
| `Code` | string | Primary key. Stored upper-case, 3–32 characters, `[A-Z0-9-]` |
| `PromotionId` | int | Must point to an existing promotion. Usually one with `RequiresCoupon = true`. |
| `MaxRedemptions` | int? | Null means unlimited |
| `MaxPerUser` | int | Default 1, ≥ 1 |
| `ExpiresAt` | DateTimeOffset? | If null, the coupon follows the promotion's `EndsAt` |
| `RedemptionCount` | int | Concurrency token |

`CouponRedemption`: `{ Id, CouponCode, BuyerId, OrderId (unique), DiscountAmount, RedeemedAt }`

**Endpoints:**

| Method | Route | Auth | Body / Query |
| --- | --- | --- | --- |
| POST | `/api/coupons` | admin | `{ "code": "FALL10", "promotionId": 1, "maxRedemptions": 500, "maxPerUser": 1, "expiresAt": "2026-11-16T23:59:59Z" }` |
| GET | `/api/coupons?promotionId=1` | admin | List coupons with `redemptionCount` |
| DELETE | `/api/coupons/{code}` | admin | Stop new redemptions; existing ones are kept |
| GET | `/api/coupons/{code}/validate` | user | Validate for the current buyer (the same check `/evaluate` uses) |

Consumed event: `OrderStatusChangedToPaidIntegrationEvent`. It needs `CouponCode` and `TotalDiscount`, which [PROMO-5](PROMO-5-persist-discounts-on-orders.md) adds to it.

**Outputs:**

- `POST /api/coupons`:
  - `201 Created`
  - `409 Conflict` if the code already exists
  - `400` if `promotionId` is unknown or the code format is wrong
- `GET /api/coupons/{code}/validate` → `200 OK`:
  ```json
  { "code": "FALL10", "status": "Applied", "promotionId": 1, "reason": null }
  ```
  or
  ```json
  { "code": "FALL10", "status": "Invalid", "promotionId": 1, "reason": "PerUserLimitReached" }
  ```
  `reason` is one of `NotFound`, `Expired`, `UsageLimitReached`, `PerUserLimitReached` or `MinOrderNotMet`.
- An `ICouponValidator` service that [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md)'s `/evaluate` uses to fill in its `coupon` block
- When the paid event arrives with a `CouponCode`:
  - one `CouponRedemption` row is inserted
  - `RedemptionCount` goes up by 1
  - `CouponRedeemedIntegrationEvent { CouponCode, PromotionId, OrderId, BuyerId, DiscountAmount }` is published through the outbox

**Acceptance Criteria:**

* **Given** coupon `FALL10` for promotion 1, with `maxRedemptions = 2` and `maxPerUser = 1`
* **When** a buyer validates `fall10` (lower case)
    * **Then** the result is `Applied`
* **When** the coupon has expired, or its promotion isn't `Active`
    * **Then** the result is `Invalid` with reason `Expired`
* **When** the buyer has already redeemed it once
    * **Then** the result is `Invalid` with reason `PerUserLimitReached`
* **When** a paid event arrives for an order that used `FALL10`
    * **Then** a redemption row is saved, `RedemptionCount` goes up by 1, and `CouponRedeemedIntegrationEvent` is published
    * **When** the same paid event arrives again (redelivery)
        * **Then** no second row is saved and the count doesn't change (unique `OrderId`)
* **When** two paid events for different buyers are handled at the same time and only one redemption remains
    * **Then** exactly one redemption succeeds. The other is logged as an over-redemption warning and still saved for audit, because the order is already paid.
* **When** the paid event has no `CouponCode`
    * **Then** the handler does nothing

**Technical Notes:**

* **Interaction history:** This story is PROMO-4 from the [promotions & recommendations epic](epic-promotions-and-recommendations.md), split by layer: the checkout UI is in [PROMO-9](PROMO-9-webapp-coupon-entry-at-checkout.md). Redemption is recorded on *paid*, not at checkout, so abandoned or failed orders don't use up coupons. The downside is that a coupon can very occasionally be redeemed more times than allowed under heavy contention. We accepted that for the workshop, and the handler logs it. Today the paid event doesn't carry the coupon, so [PROMO-5](PROMO-5-persist-discounts-on-orders.md) adds `CouponCode` and `TotalDiscount` to it.
* **Existing code to reuse:**
    * `src/Webhooks.API/IntegrationEvents/OrderStatusChangedToPaidIntegrationEventHandler.cs` and `src/Webhooks.API/IntegrationEvents/OrderStatusChangedToPaidIntegrationEvent.cs`: the pattern for consuming this exact event in another service. Each service declares its own copy of the event record.
    * `src/Webhooks.API/Extensions/Extensions.cs:19-24`: `AddSubscription<TEvent, THandler>()` registration
    * `src/Ordering.API/Application/IntegrationEvents/Events/OrderStatusChangedToPaidIntegrationEvent.cs`: the event published today (`OrderId`, `OrderStatus`, `BuyerName`, `BuyerIdentityGuid`, `OrderStockItems`)
* **New code (proposal):**
    * `Model/Coupon.cs` and `Model/CouponRedemption.cs`
    * `Apis/CouponsApi.cs`
    * `Services/CouponValidator.cs`
    * `IntegrationEvents/OrderStatusChangedToPaidIntegrationEventHandler.cs` in Promotions.API

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-1](PROMO-1-promotions-service-scaffold.md); [PROMO-5](PROMO-5-persist-discounts-on-orders.md) (only the redemption handler needs it)
* **Blocks:** [PROMO-9](PROMO-9-webapp-coupon-entry-at-checkout.md)
* **Relates to:** [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md)

**Open Questions:**

* Should we add a bulk "generate N unique single-use codes" endpoint for the abandoned-cart idea, or leave it to a later epic?
* Should a deleted coupon still be accepted for an order already in progress?
