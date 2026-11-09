# Epic: Promotions & Personalized Recommendations

## Summary

Add a promotions engine (discount rules, coupon codes, flash sales) and
personalized product recommendations to eShop, so the store can run marketing
campaigns and raise average order value. Both features plug into the existing
microservices, the RabbitMQ event bus, the Catalog vector embeddings and the
AI chatbot.

**Timebox:** AI Immersion workshop, 3 days
**Status:** Proposed

## Goals

- Marketers can create time-boxed promotions and coupon codes without a code
  change.
- Customers see the discounts that apply to them in the cart and at checkout,
  and the order records each discount.
- Customers get relevant product suggestions on the item page, in the cart
  and through the chatbot.
- The work shows the event-driven architecture and the AI features working
  together from end to end.

## Non-Goals

- Real email/SMS delivery (use a fake sink or Webhooks.API instead)
- Tax, multi-currency or regional pricing
- A full promotion-approval workflow
- Training a custom ML recommendation model (use embeddings and order history)

---

## How the App Works Today (verified in code)

Read this before you start. Pricing happens in the **WebApp**, not in
Basket.API.

| Step | What happens | Reference |
| --- | --- | --- |
| 1. Add to cart | Basket.API (Redis, gRPC) stores only `product_id` and `quantity`. It has no prices. | `src/Basket.API/Proto/basket.proto` |
| 2. Price the cart | WebApp `BasketState.FetchBasketItemsAsync()` loads the catalog items and builds `BasketItem { ProductId, ProductName, UnitPrice, Quantity }` | `src/WebApp/Services/BasketState.cs:117` |
| 3. Checkout | WebApp `BasketState.CheckoutAsync()` sends HTTP `CreateOrderRequest` to Ordering.API and then deletes the basket | `src/WebApp/Services/BasketState.cs:78` |
| 4. Create the order | Ordering.API maps `BasketItem` to `OrderItemDTO`, then `CreateOrderCommandHandler` calls `order.AddOrderItem(..., item.Discount, ...)` | `src/Ordering.API/Extensions/BasketItemExtensions.cs:13`, `src/Ordering.API/Application/Commands/CreateOrderCommandHandler.cs:44` |
| 5. Payment | Ordering publishes `OrderStatusChangedToPaidIntegrationEvent { OrderId, BuyerIdentityGuid, OrderStockItems }` | `src/Ordering.API/Application/IntegrationEvents/Events/OrderStatusChangedToPaidIntegrationEvent.cs` |

**Known gap:** `OrderItem.Discount` and `SetNewDiscount()` already exist in
the domain, with a check that the discount can't exceed the item total
(`src/Ordering.Domain/AggregatesModel/OrderAggregate/OrderItem.cs:15`).
However, `Ordering.API` `BasketItem` has no `Discount` property, and
`ToOrderItemDTO()` never sets it, **so the discount is always 0 today.**

Other existing pieces this epic builds on:
- `CatalogItem.Embedding` (pgvector `Vector?`): `src/Catalog.API/Model/CatalogItem.cs:40`
- Catalog semantic search: `GET /api/catalog/items/withsemanticrelevance`, in `src/Catalog.API/Apis/CatalogApi.cs:60-67`
- Chatbot tools `GetUserInfo`, `SearchCatalog`, `AddToCart`, `GetCartContents`, registered with `AIFunctionFactory.Create`: `src/WebApp/Components/Chatbot/ChatState.cs:43-46`
- AppHost databases `catalogdb`, `identitydb`, `orderingdb` and `webhooksdb` on one Postgres server, plus the `eventbus` RabbitMQ resource: `src/eShop.AppHost/AppHost.cs:10-20`

---

## Target Architecture

```text
                         ┌──────────────────── Promotions.API ───────────────────┐
                         │  POST /api/promotions/evaluate   (price a cart)       │
                         │  CRUD /api/promotions, /api/coupons (admin)           │
                         │  Postgres: promotionsdb                               │
                         └───────▲──────────────────────────────┬────────────────┘
                                 │ HTTP (evaluate)              │ publishes
                                 │                              ▼
WebApp BasketState ──────────────┘                  PromotionStarted / Ended events
  │  CreateOrderRequest (+Discount, +CouponCode, +PromotionIds)
  ▼
Ordering.API ── re-validates via /evaluate ── Order.AddOrderItem(discount)
  │
  ▼ OrderStatusChangedToPaidIntegrationEvent
  ├──► Promotions.API  (records coupon redemption)
  └──► Catalog.API     (updates co-purchase stats for recommendations)
```

---

## Feature 1: Promotions & Coupon Engine

### PROMO-1: Promotions service scaffold

**As a** developer **I want** a `Promotions.API` service in the Aspire
AppHost **so that** promotion data lives in its own bounded context.

#### Inputs

- New project `src/Promotions.API` that references `eShop.ServiceDefaults`, `EventBusRabbitMQ` and `IntegrationEventLogEF`
- AppHost changes:
  ```csharp
  var promotionsDb = postgres.AddDatabase("promotionsdb");
  var promotionsApi = builder.AddProject<Projects.Promotions_API>("promotions-api")
      .WithReference(rabbitMq).WaitFor(rabbitMq)
      .WithReference(promotionsDb).WaitFor(promotionsDb);
  webApp.WithReference(promotionsApi);
  orderingApi.WithReference(promotionsApi);
  ```

#### Outputs

- The service appears in the Aspire dashboard and passes health checks (`/health`, `/alive`)
- An EF Core migration creates the tables `Promotions`, `PromotionTargets`, `Coupons` and `CouponRedemptions`
- Seed data creates 4 promotions, one of each rule type (see PROMO-2), and the coupons `FALL10` and `WELCOME5`

#### Acceptance criteria

- [ ] `aspire run` starts the service with no manual steps
- [ ] `GET /api/promotions/active` returns the seeded active promotions

---

### PROMO-2: Promotion rule model & admin CRUD

**As a** marketer **I want** to define different kinds of discount rules
**so that** I can run a variety of campaigns.

#### Domain model

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | int | |
| `Name` | string | e.g. "Fall Hiking Sale" |
| `Description` | string | Copy shown on banners |
| `Type` | enum | `PercentOff`, `AmountOffOrder`, `BuyXGetY` |
| `Value` | decimal | Percent (0–100) or amount ($) |
| `MinOrderTotal` | decimal? | Used by `AmountOffOrder` |
| `BuyQuantity` / `GetQuantity` | int? | Used by `BuyXGetY` |
| `Targets` | list | Scope (`All`, `Brand`, `Type` or `Product`) plus a `TargetId` |
| `StartsAt` / `EndsAt` | DateTimeOffset | Required |
| `Priority` | int | Higher wins a tie |
| `IsStackable` | bool | Whether it combines with other promotions |
| `RequiresCoupon` | bool | If true, applies only with a valid coupon |
| `Status` | enum | `Draft`, `Active`, `Expired` (computed from the dates, plus Draft) |

**Inputs: `POST /api/promotions`** (admin only)
```json
{
  "name": "Fall Hiking Sale",
  "description": "20% off all hiking gear",
  "type": "PercentOff",
  "value": 20,
  "targets": [{ "scope": "Type", "targetId": 3 }],
  "startsAt": "2026-11-10T00:00:00Z",
  "endsAt": "2026-11-16T23:59:59Z",
  "priority": 10,
  "isStackable": false,
  "requiresCoupon": false
}
```

#### Outputs

- `201 Created` with the promotion and `Location: /api/promotions/{id}`
- `400 ValidationProblem` when any of these is true:
  - `endsAt <= startsAt`
  - a `PercentOff` value is outside 0–100
  - `BuyXGetY` is missing a quantity
  - there are no targets
- `401` or `403` when the caller isn't an admin

#### Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/promotions?status=Active` | List promotions, filtered by status |
| GET | `/api/promotions/{id}` | Get one promotion |
| POST | `/api/promotions` | Create |
| PUT | `/api/promotions/{id}` | Update |
| DELETE | `/api/promotions/{id}` | Soft delete (set to `Expired`) |
| GET | `/api/promotions/active` | Public; used for storefront banners |

#### Acceptance criteria

- [ ] Each validation rule has a unit test
- [ ] When a promotion's `StartsAt` passes, the service publishes `PromotionStartedIntegrationEvent` (see PROMO-6)

---

### PROMO-3: Cart pricing evaluation

**As a** shopper **I want** discounts applied to my cart automatically **so
that** I always get the best deal.

#### Inputs: `POST /api/promotions/evaluate`

```json
{
  "buyerId": "a1b2c3",
  "couponCode": "FALL10",
  "items": [
    { "productId": 12, "catalogTypeId": 3, "catalogBrandId": 2, "unitPrice": 89.99, "quantity": 1 },
    { "productId": 40, "catalogTypeId": 1, "catalogBrandId": 5, "unitPrice": 15.00, "quantity": 3 }
  ]
}
```

#### Outputs: `200 OK`

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
    { "id": 1, "name": "Fall Hiking Sale", "savings": 17.99 },
    { "id": 3, "name": "Buy 2 Get 1 Socks", "savings": 15.00 }
  ],
  "coupon": { "code": "FALL10", "status": "Invalid", "reason": "Expired" }
}
```
`coupon.status` is one of `Applied`, `Invalid` or `NotProvided`. `reason` is
one of `NotFound`, `Expired`, `UsageLimitReached`, `PerUserLimitReached` or
`MinOrderNotMet`.

#### Evaluation rules

1. Keep only promotions that are `Active` right now. Drop any that have `RequiresCoupon` without a matching valid coupon.
2. For each line, find the promotions whose targets match the line's product, brand or type.
3. **Non-stackable:** pick the single promotion that saves the customer the most on that line.
4. **Stackable:** apply percentage discounts first, then fixed amounts.
5. `AmountOffOrder` is applied after line discounts and split across the lines in proportion to their totals.
6. A line's discount can't exceed its total, matching the domain rule in `OrderItem`.
7. Round each line's discount to 2 decimals using banker's rounding.

#### WebApp integration

- `BasketState.FetchBasketItemsAsync()` calls `/evaluate` after it loads the catalog items
- `WebApp.Services.BasketItem` gains `Discount` and `AppliedPromotionIds`
- The cart is evaluated again whenever it changes (on the existing basket-changed notifications)

#### Acceptance criteria

- [ ] Unit tests cover each rule type, the stacking logic, the per-line cap and the order-level split
- [ ] If Promotions.API is unavailable, the cart still loads at full price and logs a warning (prices must never be wrong)

---

### PROMO-4: Coupon codes

**As a** shopper **I want** to enter a coupon code at checkout **so that** I
can redeem offers I've received.

#### Coupon model

| Field | Type | Notes |
| --- | --- | --- |
| `Code` | string | Unique, case-insensitive |
| `PromotionId` | int | The promotion this code unlocks |
| `MaxRedemptions` | int? | Total limit; null means unlimited |
| `MaxPerUser` | int | Default 1 |
| `ExpiresAt` | DateTimeOffset? | |
| `RedemptionCount` | int | Concurrency token |

#### Inputs

- Checkout page (`src/WebApp/Components/Pages/Checkout/Checkout.razor`) gets a "Promo code" input and an **Apply** button
- Clicking **Apply** calls `/evaluate` with `couponCode`
- Admin: `POST /api/coupons { "code": "FALL10", "promotionId": 1, "maxRedemptions": 500, "maxPerUser": 1, "expiresAt": "..." }`

#### Outputs

- UI: a green "FALL10 applied: you saved $9.00" message, or a red message explaining why it failed (from `coupon.reason`)
- Paid event handler: `OrderStatusChangedToPaidIntegrationEventHandler` in Promotions.API
  1. Inserts a `CouponRedemption { CouponCode, BuyerId, OrderId, RedeemedAt }`
  2. Increments `RedemptionCount`
  3. Publishes `CouponRedeemedIntegrationEvent { CouponCode, OrderId, BuyerId, DiscountAmount }`

#### Acceptance criteria

- [ ] When two orders race for the last redemption, exactly one succeeds (optimistic concurrency on `RedemptionCount`)
- [ ] Handling the same paid event twice doesn't count a redemption twice (unique `OrderId` key)

---

### PROMO-5: Persist discounts on orders (fix the existing gap)

**As the** business **I want** each order to record its discounts **so
that** revenue and promotion reporting are accurate.

#### Inputs: changes to `CreateOrderRequest`

```csharp
// Ordering.API/Application/Models/BasketItem.cs
public decimal Discount { get; init; }              // NEW

// Ordering.API/Apis/OrdersApi.cs: CreateOrderRequest
string? CouponCode,                                  // NEW
List<int> AppliedPromotionIds                        // NEW
```
- `BasketItemExtensions.ToOrderItemDTO()` sets `Discount = item.Discount`
- WebApp `BasketState.CheckoutAsync()` sends the discounts, the coupon code and the promotion IDs

#### Server-side validation

- Before creating the order, Ordering.API calls `/evaluate` again with the same items and coupon
- If the client's discount for any line differs from the server's by more than $0.01, it uses the server's value and logs a warning
- Never trust client-supplied discounts

#### Outputs

- `OrderItem.Discount` is filled in
- The `Order` gains `CouponCode` and `AppliedPromotionIds`, which need an EF migration in `Ordering.Infrastructure`
- `GET /api/orders/{id}` returns `discount` for each item and `totalDiscount` for the order
- `Order.GetTotal()` subtracts the discounts. Today it is `Sum(Units * UnitPrice)` and ignores `Discount` (`src/Ordering.Domain/AggregatesModel/OrderAggregate/Order.cs:185`)
- The order detail page shows the savings

#### Acceptance criteria

- [ ] A functional test in `tests/Ordering.FunctionalTests` checks that an order with a $10 discount stores the discount and totals correctly
- [ ] The existing domain check (discount can't exceed the line total) still holds

---

### PROMO-6: Storefront promotion display

**As a** shopper **I want** to see active deals **so that** I'm encouraged to
buy.

#### Inputs

- `GET /api/promotions/active`, cached in the WebApp for 60s
- `PromotionStartedIntegrationEvent { PromotionId, Name, Description, EndsAt, Targets }` and `PromotionEndedIntegrationEvent { PromotionId }`, which clear the WebApp cache

#### Outputs

| Where | What the shopper sees |
| --- | --- |
| Catalog grid (`CatalogListItem`) | A "-20%" sale badge, the original price crossed out and the discounted price |
| Item page (`ItemPage.razor`) | The promotion name and end date: "Fall Hiking Sale, ends Sun" |
| Cart page (`CartPage.razor`) | A line for each discount, "You saved $X", and the subtotal, discount and total |
| Header banner | Flash-sale banner with a live countdown to `EndsAt`, for the highest-priority active promotion |

#### Acceptance criteria

- [ ] The banner disappears within 60s of a promotion ending, with no page reload
- [ ] Prices shown in the grid match what `/evaluate` returns for a single unit

---

### PROMO-7 (stretch): AI Marketing Copilot

**As a** marketer **I want** to describe a campaign in plain language **so
that** AI drafts the promotion, the product list and the copy for me.

#### Inputs

- An admin page `/admin/copilot` with a chat box
- Example prompt: *"Create a fall campaign for hiking gear, 20% off, ends Sunday. Make it feel cozy."*
- LLM tools (via `AIFunctionFactory.Create`, following the pattern in `ChatState.cs`):

| Tool | Parameters | Returns |
| --- | --- | --- |
| `SearchProducts` | `query: string, count: int` | `[{ id, name, typeId, brandId, price }]` (semantic search) |
| `ListCatalogTypesAndBrands` | – | `{ types: [...], brands: [...] }` |
| `CreatePromotionDraft` | the PROMO-2 request body | `{ promotionId, status: "Draft" }` |
| `CreateCoupon` | `code, promotionId, maxRedemptions` | `{ code }` |

#### Outputs

- The promotion is saved with `Status = Draft`
- The Copilot's response:
  ```json
  {
    "promotionId": 7,
    "summary": "20% off 14 hiking products, Nov 10–16",
    "bannerHeadline": "Cozy Up for the Trail",
    "bannerSubtext": "20% off hiking gear through Sunday",
    "emailSubject": "Your fall adventure starts here 🍂",
    "emailBody": "…",
    "suggestedCoupon": "TRAIL20"
  }
  ```
- The marketer clicks **Publish**, which sets `Status` to `Active` and publishes `PromotionStartedIntegrationEvent`

#### Acceptance criteria

- [ ] The Copilot never creates an `Active` promotion directly; a person must approve it
- [ ] Dates like "ends Sunday" are resolved against the current date and shown back to the marketer for confirmation
- [ ] If AI isn't configured in AppHost, the page shows a clear "AI not configured" message

---

## Feature 2: Personalized Recommendations

### REC-1: Similar items

**As a** shopper viewing a product **I want** to see similar products **so
that** I can compare options.

#### Inputs: `GET /api/catalog/items/{id}/similar?count=4`

#### Logic

- If AI is enabled: order other items by `Embedding.CosineDistance(target.Embedding)` using pgvector, excluding the item itself
- Otherwise: items of the same `CatalogTypeId`, with items of the same `CatalogBrandId` first, then by price closest to the target's

#### Outputs: `200 OK`

```json
[
  { "id": 18, "name": "Trail Backpack 30L", "price": 74.99, "pictureUrl": "/api/catalog/items/18/pic", "score": 0.91, "reason": "Similar" }
]
```
- `404` if the item doesn't exist

**UI:** a "You may also like" carousel on `ItemPage.razor` with 4 cards, each with an **Add to cart** button

#### Acceptance criteria

- [ ] The response never includes the product itself or out-of-stock items (`AvailableStock = 0`)
- [ ] The fallback path has its own test (AI disabled)

---

### REC-2: Frequently bought together

**As a** shopper **I want** to see products others bought with this one **so
that** I discover things that go well together.

#### Inputs

- A new handler in Catalog.API for `OrderStatusChangedToPaidIntegrationEvent`, which uses `OrderStockItems`
- New table `ProductCoPurchase { ProductIdA, ProductIdB, Count, LastPurchasedAt }`, with `A < B` so each pair is stored once
- `GET /api/catalog/items/{id}/also-bought?count=4`

#### Logic

- Every pair of distinct products in a paid order adds 1 to that pair's `Count`
- The endpoint returns the top `count` partners ordered by `Count`, with ties broken by `LastPurchasedAt`
- With fewer than 2 partners, the rest is filled from REC-1 results

#### Outputs

```json
[
  { "id": 33, "name": "Wool Hiking Socks", "price": 12.99, "timesBoughtTogether": 27, "reason": "AlsoBought" }
]
```

**UI:** a "Frequently bought together" block on the item page with an **Add both to cart** button

#### Acceptance criteria

- [ ] Handling the same event twice doesn't double-count (log processed `OrderId`s)
- [ ] A seed script simulates 50 orders so the demo has data

---

### REC-3: Personalized "For You"

**As a** returning shopper **I want** recommendations based on my history
**so that** the store feels tailored to me.

#### Inputs

- `GET /api/catalog/recommendations/for-user?count=8` (requires authentication; buyer ID from the token)
- Signals:
  - purchased product IDs (from paid events, stored as `UserProductSignal { BuyerId, ProductId, Weight=3 }`)
  - recently viewed items (`ProductViewedIntegrationEvent { BuyerId, ProductId }` from the item page, `Weight=1`)

#### Logic

1. User vector = the weighted average of the embeddings of the 20 most recent signal products
2. Return the nearest items by cosine distance, excluding products already purchased
3. Users with no history (and anonymous users) get the 8 most-purchased items from REC-2 data

#### Outputs

```json
{
  "strategy": "Personalized",
  "items": [ { "id": 51, "name": "Insulated Jacket", "price": 129.00, "score": 0.87 } ]
}
```
`strategy` is `Personalized` or `Popular`.

**UI:** a "Recommended for you" row at the top of the catalog home page

#### Acceptance criteria

- [ ] A user who bought tents sees camping items before unrelated ones (test with a fixed seed)
- [ ] Anonymous users get `strategy: "Popular"`

---

### REC-4: Cart cross-sell with promotions

**As the** business **I want** the cart to suggest add-ons tied to active
promotions **so that** average order value goes up.

#### Inputs

- The current cart's product IDs
- REC-1 and REC-2 results for those products
- Active promotions (PROMO-3)
- `POST /api/catalog/recommendations/cross-sell { "productIds": [12, 40], "count": 3 }`

#### Logic

1. Gather the REC-2 partners and REC-1 neighbors of every cart item, excluding items already in the cart
2. Move items covered by an active promotion to the top
3. If an `AmountOffOrder` promotion's threshold is close, prefer items that would push the cart over it. Example: the cart is $85 and there's $10 off orders over $100, so suggest items in the $15–40 range.

#### Outputs

```json
[
  {
    "id": 51, "name": "Insulated Jacket", "price": 129.00,
    "promotion": { "id": 1, "label": "20% off" },
    "message": "Add an Insulated Jacket and save $25.80"
  },
  {
    "id": 33, "name": "Wool Hiking Socks", "price": 12.99,
    "promotion": null,
    "message": "Add $15.01 more to unlock $10 off your order"
  }
]
```

**UI:** a "Complete your order" section on `CartPage.razor`, with one-click add

#### Acceptance criteria

- [ ] Adding a suggested item re-prices the cart immediately with the new discount applied

---

### REC-5: Chatbot recommendations & deals

**As a** shopper chatting with the assistant **I want** it to suggest products
and mention current deals **so that** I get useful shopping help.

#### Inputs: new tools in `src/WebApp/Components/Chatbot/ChatState.cs`

| Tool | Parameters | Calls | Returns to the LLM |
| --- | --- | --- | --- |
| `GetSimilarProducts` | `productId: int` | REC-1 | JSON list of products |
| `GetRecommendationsForMe` | – | REC-3 | JSON list plus the strategy |
| `GetActivePromotions` | `category?: string` | `/api/promotions/active` | `[{ name, description, endsAt, appliesTo }]` |
| `CheckCartSavings` | – | PROMO-3 `/evaluate` | `{ subtotal, totalDiscount, appliedPromotions }` |

**System prompt addition:** *"When recommending products, mention any active
promotion that applies to them. Never invent discounts. Only quote what
GetActivePromotions or CheckCartSavings returns."*

#### Example conversation

- User: *"Any deals on tents?"*
- The assistant calls `GetActivePromotions("tents")`, then `SearchCatalog("tent")`
- It replies with 3 tents and their discounted prices, and offers to add one to the cart using the existing `AddToCart` tool

#### Acceptance criteria

- [ ] Every discount the bot quotes matches the API output (an LLM-as-judge or scripted eval over 10 prompts)
- [ ] The bot asks for confirmation before calling `AddToCart`

---

## New Integration Events

| Event | Payload | Publisher | Subscribers |
| --- | --- | --- | --- |
| `PromotionStartedIntegrationEvent` | `PromotionId, Name, Description, EndsAt, Targets[]` | Promotions.API | WebApp (banner/cache) |
| `PromotionEndedIntegrationEvent` | `PromotionId` | Promotions.API | WebApp |
| `CouponRedeemedIntegrationEvent` | `CouponCode, OrderId, BuyerId, DiscountAmount` | Promotions.API | Analytics (optional) |
| `ProductViewedIntegrationEvent` | `BuyerId, ProductId, ViewedAt` | WebApp | Catalog.API (REC-3) |
| *(existing)* `OrderStatusChangedToPaidIntegrationEvent` | `OrderId, BuyerIdentityGuid, OrderStockItems[]` | Ordering.API | **New:** Promotions.API (PROMO-4) and Catalog.API (REC-2, REC-3) |

## 3-Day Plan

| Day | Focus | Stories |
| --- | --- | --- |
| 1 | Promotions core | PROMO-1, PROMO-2, PROMO-3, PROMO-5 |
| 2 | Coupons, UI and recommendations | PROMO-4, PROMO-6, REC-1, REC-2 |
| 3 | Personalization, AI and demo | REC-3, REC-4, REC-5, PROMO-7 (stretch), demo polish |

### Team split suggestion

- Team A: Promotions.API (PROMO-1 to 4)
- Team B: changes to Ordering and the WebApp (PROMO-5, PROMO-6)
- Team C: recommendations (REC-1 to 5)

They agree on the `/evaluate` request and response shapes on Day 1 morning.

## Demo Script

1. The marketer uses the Copilot to create a "Fall Hiking" promotion (PROMO-7), then publishes it.
2. The storefront shows sale badges and a countdown banner (PROMO-6).
3. The shopper views a backpack and sees similar items and frequently bought together products (REC-1, REC-2).
4. The cart suggests a jacket, applies the discount and accepts the coupon `FALL10` (REC-4, PROMO-3, PROMO-4).
5. The order completes with discounts on each line (PROMO-5). The Aspire dashboard shows the events flowing.
6. The chatbot answers "Any deals on tents?" with real promotions and recommendations (REC-5).

## Risks & Open Questions

- **Never trust client discounts:** Ordering must check the discounts again through `/evaluate` (PROMO-5).
- **Coupon race conditions:** concurrent redemptions can exceed usage limits. Use optimistic concurrency and an idempotent `OrderId` key.
- **Embeddings are optional:** AI is optional in AppHost, so REC-1 and REC-3 need versions that work without embeddings.
- **Cold start:** recommendations need order history, so the REC-2 seed script is required for the demo.
- **Admin auth:** decide whether Identity.API gets an `admin` role or the workshop uses a hard-coded admin user.
- **Scope:** PROMO-7 and REC-3 are the first stories to cut if time runs out.

## Definition of Done

- All services start via Aspire with no manual setup
- Unit tests cover promotion evaluation (each rule type, stacking, caps)
- A functional test covers saving discounts on an order
- A Playwright E2E test (`e2e/`) applies a coupon at checkout and checks the order total
- `graphify update .` has been run after the code changes
