# PROMO-9: [FE - WebApp] - Coupon code entry in the cart and checkout

This story was split from the UI part of PROMO-4 in the [epic](epic-promotions-and-recommendations.md); the backend part is [PROMO-4](PROMO-4-coupon-codes-and-redemption.md).

**User Story:**

As a shopper, I want to enter a promo code and see straight away whether it worked and how much it saves me, so that I can use offers I've been sent with confidence.

**Inputs:**

- A "Promo code" text box and an **Apply** button in the cart summary (`/cart`), plus a read-only line showing the applied code on `/checkout`
- On submit, the WebApp trims and upper-cases the code, stores it in `BasketState` for the session, and prices the cart again via `/evaluate` with `couponCode` ([PROMO-3](PROMO-3-cart-pricing-evaluation-api.md))
- A **Remove** link clears the code and prices the cart again

**Outputs:**

| Coupon result (`coupon.status` / `reason`) | What the shopper sees |
| --- | --- |
| `Applied` | Green: "FALL10 applied: you save $9.00", with a summary line "Coupon FALL10 −$9.00" and a **Remove** link |
| `Invalid` / `NotFound` | Red: "We don't recognize that code." |
| `Invalid` / `Expired` | Red: "That code has expired." |
| `Invalid` / `UsageLimitReached` | Red: "That code is no longer available." |
| `Invalid` / `PerUserLimitReached` | Red: "You've already used that code." |
| `Invalid` / `MinOrderNotMet` | Red: "Add $X more to use this code." (from `nextThreshold`) |

- On checkout, `CreateOrderRequest.couponCode` is set only when the status is `Applied` ([PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md) sends it)
- After a successful order the code is cleared from the session, together with the basket

**Acceptance Criteria:**

* **Given** a signed-in shopper with a type-3 item in the cart and a valid coupon `FALL10`
* **When** they enter `fall10` (lower case, trailing space) and click **Apply**
    * **Then** the green message and the coupon line appear, and the Total drops by the coupon amount
    * **When** they go to `/checkout`
        * **Then** the applied code and the discounted total are shown
    * **When** they place the order
        * **Then** the order is created with `couponCode = "FALL10"`, and the code is cleared afterwards
* **When** they enter an unknown code
    * **Then** "We don't recognize that code." is shown, and the totals don't change
* **When** they apply a code and then change the cart so it no longer qualifies
    * **Then** the message changes to the matching red message on the next pricing, and the coupon line disappears
* **When** they click **Remove**
    * **Then** the coupon line and message disappear, and the totals go back to what they were without the coupon
* **When** the form is submitted with JavaScript turned off
    * **Then** it still works, through the same enhanced-form post pattern the cart quantity controls use

**Technical Notes:**

* **Interaction history:** This story was split from the epic's PROMO-4 under the create-story FE/BE rule. The box goes in the cart summary, not only at checkout, so shoppers see the savings before they commit. Checkout just shows the code that's applied. The applied code is held on the WebApp side (with `BasketState`) rather than in Basket.API, because the basket gRPC contract has only `product_id` and `quantity`, and changing that contract is out of scope.
* **Existing code to reuse:**
    * `src/WebApp/Components/Pages/Cart/CartPage.razor:50-76,107-139`: the enhanced `<form method="post" data-enhance>` plus `@formname` and `[SupplyParameterFromForm]` pattern to copy for the coupon form
    * `src/WebApp/Components/Pages/Cart/CartPage.razor:86-103`: the summary box the field goes into
    * `src/WebApp/Components/Pages/Checkout/Checkout.razor:53-58`: where the applied-code line goes, above "Place order"
    * `src/WebApp/Services/BasketState.cs:78-109`: `CheckoutAsync()`, which already clears the basket after ordering; clear the coupon there too
    * `src/Basket.API/Proto/basket.proto`: confirms the basket stores no coupon field
* **New code (proposal):**
    * `BasketState.ApplyCouponAsync(string)` and `RemoveCouponAsync()`
    * a `CouponForm.razor` component
    * the mapping from `reason` to message as a small static helper, so it can be unit tested

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-4](PROMO-4-coupon-codes-and-redemption.md), [PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md)

**Open Questions:**

* `BasketState` is registered as scoped (`src/WebApp/Extensions/Extensions.cs:23`). With static server rendering, that scope is one HTTP request, so the code would be lost between pages. Should the applied code be kept in a cookie or in `ProtectedSessionStorage` instead? This needs a quick spike on Day 2.
* Should the e2e test for this story extend `e2e/CheckoutTest.spec.ts` or be a new spec?
