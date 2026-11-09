# PROMO-6: [FE - WebApp, WebAppComponents] - Storefront promotion display

**User Story:**

As a shopper browsing the store, I want to see which products are on sale and what deals are running, so that I notice offers and am encouraged to buy.

**Inputs:**

- `GET https+http://promotions-api/api/promotions/active` (anonymous), called through a new typed `PromotionsService` HttpClient. Response shape is defined in [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md).
- `PromotionStartedIntegrationEvent` and `PromotionEndedIntegrationEvent`, consumed by the WebApp to clear its cache
- Each `CatalogItem` (`Id`, `CatalogTypeId`, `CatalogBrandId`, `Price`) shown on a page

**Outputs:**

| Where | What the shopper sees | Rule |
| --- | --- | --- |
| Catalog grid (`CatalogListItem.razor`) | A badge such as `-20%` or `Buy 2 Get 1`, the original price crossed out and the sale price | Only for `PercentOff` and `BuyXGetY` promotions that match the item; the sale price is shown for `PercentOff` only |
| Item page (`ItemPage.razor`) | A promotion row, e.g. "Fall Hiking Sale: 20% off, ends Sun Nov 16", next to the price | The highest-priority matching promotion |
| Site banner (under `HeaderBar.razor`) | One line with the promotion's description, a countdown ("2d 4h 12m left") and a "Shop the sale" link to the filtered catalog | The active promotion with the highest priority that targets `All`, `Type` or `Brand`. Hidden if none. |
| Anywhere | No change | When Promotions.API is down or there are no active promotions |

Display price rule (`PercentOff` only): `round(price × (1 − value/100), 2)`. It must match what [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md) calculates for a quantity-1 line.

**Acceptance Criteria:**

* **Given** the Fall Hiking Sale (20% off type 3) is active
* **When** a shopper opens the catalog
    * **Then** every type-3 item shows `-20%`, its original price crossed out and the discounted price
    * **Then** items not in type 3 look the same as today
* **When** a shopper opens a type-3 item page
    * **Then** the promotion name and end date show beside the price
* **When** any page loads while the promotion has the highest priority
    * **Then** the banner shows its description and a countdown that updates every second in the browser
    * **When** the countdown reaches zero
        * **Then** the banner hides itself without a page reload
* **When** the WebApp receives `PromotionEndedIntegrationEvent`
    * **Then** the next page load (and every one after it) shows no badge or banner for that promotion
* **When** Promotions.API can't be reached
    * **Then** pages render at normal prices with no badges or banner, a warning is logged, and page load time isn't affected by more than the 2s HttpClient timeout
* **Given** a promotion with `requiresCoupon = true`
* **When** any page renders
    * **Then** it is never shown on the storefront

**Technical Notes:**

* **Interaction history:** This story is PROMO-6 from the [promotions & recommendations epic](epic-promotions-and-recommendations.md). The cart-page discount lines originally listed here moved to [PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md), so all cart pricing lives in one story. Badges come from `/active` and are calculated on the page rather than calling `/evaluate` for each tile, to keep the catalog grid fast. The catch is that stacked or order-level promotions aren't reflected on the badge, which we accepted.
* **Existing code to reuse:**
    * `src/WebAppComponents/Catalog/CatalogListItem.razor:16-19`: the price markup to extend with the badge and crossed-out price
    * `src/WebAppComponents/Catalog/CatalogItem.cs`: already carries `CatalogTypeId` and `CatalogBrandId`, so matching needs no extra calls
    * `src/WebApp/Components/Pages/Item/ItemPage.razor:39-41`: the price inside the add-to-cart form
    * `src/WebApp/Components/Layout/HeaderBar.razor:3-31` and `src/WebApp/Components/Layout/MainLayout.razor:4`: where the banner goes
    * `src/WebApp/Extensions/Extensions.cs:34-40`: how typed HttpClients are registered (`AddHttpClient<T>(o => o.BaseAddress = new("https+http://…"))`)
    * `src/WebApp/Extensions/Extensions.cs:43-51`: how event subscriptions are added
    * `src/WebApp/Services/OrderStatus/IntegrationEvents/EventHandling/`: an existing WebApp event handler to copy
    * `src/WebApp/wwwroot/js/chat-fab.js`: an existing small browser script to model the countdown on
    * Only `Chatbot.razor` and `OrdersRefreshOnStatusChange.razor` declare a render mode, so `HeaderBar` renders statically on the server. That's why the countdown has to run in browser JavaScript.
* **New code (proposal):**
    * `Services/PromotionsService.cs`, which caches `/active` in `IMemoryCache` for 60s, and an event handler that clears that cache
    * a `PromotionBadge.razor` component in WebAppComponents
    * a `PromotionBanner.razor` component
    * `wwwroot/js/promo-countdown.js`
    * AppHost needs `webApp.WithReference(promotionsApi)`, which is covered by [PROMO-1](PROMO-1-promotions-service-scaffold.md)

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md)
* **Relates to:** [PROMO-8](PROMO-8-webapp-cart-pricing-and-checkout-discounts.md) (both register `PromotionsService` in the WebApp)

**Open Questions:**

* `WebAppComponents` is shared with `HybridApp`. Should the badge component take promotion data as a parameter (keeping it free of services) so HybridApp can use it later?
* Should "Shop the sale" link to a filtered catalog URL (`?type=3`), or to a new `/sale` page?
