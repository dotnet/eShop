# PROMO-8: [FE - WebApp] - Cart pricing with promotions and discounts sent at checkout

This story was split from the WebApp part of PROMO-5 in the [epic](epic-promotions-and-recommendations.md); the backend part is [PROMO-5](PROMO-5-persist-discounts-on-orders.md).

**User Story:**

As a shopper, I want my shopping bag to show each discount, how much I'm saving and the final total, and to be charged that same total when I check out, so that I trust the price I see.

**Inputs:**

- `BasketState.FetchBasketItemsAsync()` already loads quantities from Basket.API and product details from Catalog. New step: it then calls `POST https+http://promotions-api/api/promotions/evaluate` with `{ couponCode, items: [{ productId, catalogTypeId, catalogBrandId, unitPrice, quantity }] }`. The contract is in [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md).
- `couponCode`: whatever the shopper applied during this session (from [PROMO-9](PROMO-9-webapp-coupon-entry-at-checkout.md)); `null` until then
- New fields on `WebApp.Services.BasketItem`:

| Field | Type | Source |
|---|---|---|
| `Discount` | decimal | `lines[].discount` |
| `AppliedPromotionIds` | int[] | `lines[].appliedPromotionIds` |
| `CatalogTypeId` / `CatalogBrandId` | int | the catalog item (needed to build the request) |

- A new `BasketPricing` summary exposed by `BasketState`: `{ Subtotal, TotalDiscount, Total, AppliedPromotions[], NextThreshold, Coupon }`

**Outputs:**

- **Cart page (`/cart`):**
  - each discounted line shows the original line total crossed out, the discounted total, and a small label with the promotion name (e.g. "Fall Hiking Sale")
  - the summary box shows Subtotal, a line per promotion ("Fall Hiking Sale −$17.99"), **You save $X** and the **Total**
  - when `nextThreshold` is set: "Add $48.00 more to get $10 off"
- **Checkout:** `BasketState.CheckoutAsync()` sends `CreateOrderRequest` with `items[].discount`, `couponCode` and `appliedPromotionIds` (the contract is in [PROMO-5](PROMO-5-persist-discounts-on-orders.md))
- **Orders page (`/user/orders`):** no markup change. `Total` already comes from the API, and once [PROMO-5](PROMO-5-persist-discounts-on-orders.md) is in place it's the discounted total.
- **If Promotions.API fails:** the cart shows normal prices, no discount lines, and a small note: "Promotions are temporarily unavailable"

**Acceptance Criteria:**

* **Given** a signed-in shopper and the Fall Hiking Sale (20% off type 3) is active
* **When** they add a type-3 item at $89.99 and open `/cart`
    * **Then** that line shows $89.99 crossed out, $72.00 and the label "Fall Hiking Sale"
    * **Then** the summary shows Subtotal $89.99, "Fall Hiking Sale −$17.99", "You save $17.99" and Total $72.00
* **When** they change a quantity or remove a line
    * **Then** the cart is priced again and the summary updates on the same page reload as the quantity change
* **When** the cart is $48 below an order-threshold promotion
    * **Then** the summary shows "Add $48.00 more to get $10 off"
* **When** they place the order
    * **Then** `CreateOrderRequest` contains the same discount for each line that the cart showed, plus `appliedPromotionIds`
    * **Then** `/user/orders` lists the order with a total equal to the cart's Total
* **When** the cart has no matching promotions
    * **Then** the cart looks exactly as it does today (no "You save $0.00")
* **When** Promotions.API is unreachable or slower than 2s
    * **Then** the cart loads at normal prices with the "temporarily unavailable" note, and checkout still works with `discount = 0`

**Technical Notes:**

* **Interaction history:** We created this story when the create-story skill's FE/BE rule split [PROMO-5](PROMO-5-persist-discounts-on-orders.md). It also takes over the "cart page discount lines" from the epic's PROMO-6, so all cart pricing lives in one place. When we traced the code, we confirmed that pricing happens in the WebApp's `BasketState` (Basket.API stores only IDs and quantities), so this is the one place to call `/evaluate`. There's no order detail page in the WebApp today (only the `/user/orders` list), so showing savings for each order is left as an open question rather than added.
* **Existing code to reuse:**
    * `src/WebApp/Services/BasketState.cs:117-150`: `FetchBasketItemsAsync()` builds `BasketItem`s from catalog data. Add the `/evaluate` call here; `_cachedBasket` already prevents repeated calls.
    * `src/WebApp/Services/BasketState.cs:78-109`: `CheckoutAsync()` builds `CreateOrderRequest`. Pass the discounts, coupon and promotion IDs here.
    * `src/WebApp/Services/BasketState.cs:158` onward: the WebApp's own `CreateOrderRequest` record, which needs the new fields
    * `src/WebApp/Services/BasketItem.cs`: the model to extend
    * `src/WebApp/Components/Pages/Cart/CartPage.razor:46,79`: the line price and total markup
    * `src/WebApp/Components/Pages/Cart/CartPage.razor:86-103`: the summary box
    * `src/WebApp/Components/Pages/Cart/CartPage.razor:123`: `TotalPrice`, which should switch to `Pricing.Total`
    * `src/WebApp/Components/Pages/User/Orders.razor:47-49`: shows `order.Total` (no change needed)
    * `src/WebApp/Extensions/Extensions.cs:34-40`: the typed HttpClient registration pattern
* **New code (proposal):**
    * a `PromotionsService.EvaluateAsync()` client, shared with [PROMO-6](PROMO-6-storefront-promotion-display.md)'s `PromotionsService`
    * a `BasketPricing` record
    * a `CartSummary.razor` partial component to keep `CartPage.razor` small

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md), [PROMO-5](PROMO-5-persist-discounts-on-orders.md)
* **Blocks:** [PROMO-9](PROMO-9-webapp-coupon-entry-at-checkout.md)
* **Relates to:** [PROMO-6](PROMO-6-storefront-promotion-display.md)

**Open Questions:**

* Should "You saved $X" also appear on `/user/orders`? That needs `totalDiscount` in the order summary response from [PROMO-5](PROMO-5-persist-discounts-on-orders.md), and a new column in the list.
* Should the cart header badge (`CartMenu.razor`) show the discounted total, or only the item count as today?
