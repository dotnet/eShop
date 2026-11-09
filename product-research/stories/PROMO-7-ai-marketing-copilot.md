# PROMO-7: [FE - WebApp] - AI Marketing Copilot (stretch)

**User Story:**

As a marketer, I want to describe a campaign in plain language and have an AI assistant draft the promotion, choose the products and write the marketing copy, so that I can launch campaigns in minutes and only have to review and approve them.

**Inputs:**

- An admin page `/admin/copilot`, with an interactive chat panel and a draft preview panel
- Example prompt: *"Create a fall campaign for hiking gear, 20% off, ends Sunday. Make it feel cozy."*
- Tools available to the LLM (registered with `AIFunctionFactory.Create`):

| Tool | Parameters | Calls | Returns to the LLM |
|---|---|---|---|
| `GetCurrentDate` | – | system clock | `"2026-11-09 (Monday)"` |
| `SearchProducts` | `query: string`, `count: int = 20` | Catalog `GET /api/catalog/items/withsemanticrelevance?text=…` | `[{ id, name, typeId, type, brandId, brand, price }]` |
| `ListCatalogTypesAndBrands` | – | Catalog `/api/catalog/catalogtypes` and `/catalogbrands` | `{ types: [{id, type}], brands: [{id, brand}] }` |
| `ListActivePromotions` | – | Promotions `GET /api/promotions?status=Active` | Used to avoid overlapping campaigns |
| `CreatePromotionDraft` | the [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md) create body | Promotions `POST /api/promotions` | `{ promotionId, status: "Draft" }` or the validation errors |
| `CreateCoupon` | `code`, `promotionId`, `maxRedemptions` | Promotions `POST /api/coupons` | `{ code }` or a conflict |

**Outputs:**

- The promotion is saved with `Status = Draft`, never `Published`
- The Copilot returns a structured campaign summary, shown in the preview panel and editable:
  ```json
  {
    "promotionId": 7,
    "summary": "20% off 14 hiking products (type: Hiking), Mon Nov 9 – Sun Nov 15",
    "resolvedDates": { "startsAt": "2026-11-09T00:00:00Z", "endsAt": "2026-11-15T23:59:59Z" },
    "bannerHeadline": "Cozy Up for the Trail",
    "bannerSubtext": "20% off hiking gear through Sunday",
    "emailSubject": "Your fall adventure starts here",
    "emailBody": "…",
    "suggestedCoupon": "TRAIL20"
  }
  ```
- A **Publish** button calls Promotions `POST /api/promotions/{id}/publish`, which leads to `PromotionStartedIntegrationEvent`, and the [PROMO-6](PROMO-6-storefront-promotion-display.md) banner appears
- A **Discard** button calls `POST /api/promotions/{id}/end`
- A message saying "AI isn't configured" when no `IChatClient` is registered

**Acceptance Criteria:**

* **Given** an admin is signed in and a chat model is configured (Foundry or Ollama)
* **When** the admin enters the example prompt
    * **Then** the Copilot calls `GetCurrentDate`, works out that "Sunday" means 2026-11-15, and shows the dates in the preview for confirmation
    * **Then** it creates exactly one promotion with `Status = Draft`, targeting the type or products it found
    * **Then** the preview shows the banner and email copy and the list of products the promotion targets
    * **When** the admin clicks **Publish**
        * **Then** the promotion becomes `Published`, and the storefront banner appears within 60s
    * **When** the admin asks for a change ("make it 25%")
        * **Then** the existing draft is updated (not duplicated) and the preview refreshes
* **When** the prompt leaves out the discount or the end date
    * **Then** the Copilot asks a follow-up question instead of guessing
* **When** `CreatePromotionDraft` returns validation errors
    * **Then** the Copilot explains them in plain language and offers a fix
* **Given** no chat model is configured
* **When** the admin opens `/admin/copilot`
    * **Then** a clear "AI isn't configured" message is shown and the page doesn't crash
* **Given** a signed-in user who isn't an admin
* **When** they open `/admin/copilot`
    * **Then** they get `403`

**Technical Notes:**

* **Interaction history:** This story is PROMO-7 (stretch) from the [promotions & recommendations epic](epic-promotions-and-recommendations.md). We chose to have a person approve every campaign: the AI can only create drafts, and publishing is a person's click. That's why [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md) has the `Draft`/`Published` statuses. The page lives in the WebApp to reuse the existing `IChatClient` setup and auth, rather than adding a separate admin app. It's tagged FE because all its server-side needs are covered by [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md) and [PROMO-4](PROMO-4-coupon-codes-and-redemption.md).
* **Existing code to reuse:**
    * `src/WebApp/Components/Chatbot/ChatState.cs:39-47`: tool registration with `AIFunctionFactory.Create` and `[Description]` attributes
    * `src/WebApp/Components/Chatbot/ChatState.cs:50-60`: the system-prompt pattern
    * `src/WebApp/Components/Chatbot/ChatState.cs:69-95`: `GetResponseAsync` with error handling
    * `src/WebApp/Components/Chatbot/Chatbot.razor`: an interactive component to model the chat panel on
    * `src/WebApp/Extensions/Extensions.cs:94-109`: `AddAIServices()`, with an Ollama or OpenAI `IChatClient` and `UseFunctionInvocation()`
    * `src/eShop.AppHost/Extensions.cs:55-56`: the Foundry `chatModel` (gpt-4.1-mini) and `textEmbeddingModel` deployments
    * `src/Catalog.API/Apis/CatalogApi.cs:60-67`: the semantic search endpoints that `SearchProducts` wraps
    * `src/Catalog.API/Services/CatalogAI.cs:25`: `IsEnabled`, which tells you whether semantic search is available
* **New code (proposal):**
    * `Components/Pages/Admin/Copilot.razor` (interactive server)
    * `Services/MarketingCopilotState.cs`, holding the tools and a system prompt that says "only create Draft promotions; always confirm dates; never invent product IDs"
    * an admin authorization policy, which depends on [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md)'s open question about how admins are identified
    * Email copy is only shown on screen; sending it is out of scope

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-2](PROMO-2-promotion-rule-model-and-admin-api.md)
* **Relates to:** [PROMO-4](PROMO-4-coupon-codes-and-redemption.md) (optional coupon creation), [PROMO-6](PROMO-6-storefront-promotion-display.md) (the banner shows the result)

**Open Questions:**

* What should `SearchProducts` fall back to when catalog embeddings are off: name search (`/items/by/{name}`), or type/brand matching only?
* Should every Copilot tool call be logged to an audit table for the demo, or are the Aspire traces enough?
