# PROMO-2: [BE - Promotions.API] - Promotion rule model and admin API

**User Story:**

As a marketer, I want to create, edit, publish and end promotions of several kinds through an API, so that I can run campaigns without a code change.

**Inputs:**

Domain model (`Promotion` aggregate):

| Field | Type | Rules |
|---|---|---|
| `Id` | int | Identity |
| `Name` | string | Required, max 100 |
| `Description` | string | Required, max 500. Shown on banners. |
| `Type` | enum | `PercentOff`, `AmountOffOrder`, `BuyXGetY` |
| `Value` | decimal | `PercentOff`: 0 < v ≤ 100. `AmountOffOrder`: v > 0. `BuyXGetY`: ignored. |
| `MinOrderTotal` | decimal? | Required for `AmountOffOrder` |
| `BuyQuantity` / `GetQuantity` | int? | Both required and ≥ 1 for `BuyXGetY` |
| `Targets` | `PromotionTarget[]` | At least 1. `{ Scope: All, Brand, Type or Product, TargetId: int? }`. `TargetId` is null only when `Scope = All`. |
| `StartsAt` / `EndsAt` | DateTimeOffset | `EndsAt > StartsAt` |
| `Priority` | int | Default 0; the higher value wins a tie |
| `IsStackable` | bool | Default false |
| `RequiresCoupon` | bool | Default false |
| `Status` | enum | `Draft` or `Published` (stored), plus `Ended` (set by an admin) |
| `EffectiveStatus` | enum (computed) | `Draft`, `Scheduled`, `Active` or `Expired`, worked out from `Status` and the current time |

`POST /api/promotions` (requires an admin):
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

**Endpoints:**

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | `/api/promotions?status=Active&page=0&pageSize=20` | admin | List promotions, paged, filtered by `EffectiveStatus` |
| GET | `/api/promotions/{id}` | admin | Get one promotion |
| POST | `/api/promotions` | admin | Create (always as `Draft`) |
| PUT | `/api/promotions/{id}` | admin | Update. Only allowed while the promotion is `Draft` or `Scheduled`. |
| POST | `/api/promotions/{id}/publish` | admin | Change `Draft` to `Published` |
| POST | `/api/promotions/{id}/end` | admin | Set to `Ended` immediately |
| GET | `/api/promotions/active` | anonymous | The storefront feed (fields below) |

**Outputs:**

- `POST`: `201 Created` with `Location: /api/promotions/{id}` and the full promotion, including `status` and `effectiveStatus`
- `PUT` and `publish`: `200 OK` with the full promotion
- `end`: `204 No Content`
- Errors:
  - `400 ValidationProblem` with field-level errors for every rule in the table above
  - `404` if the promotion is unknown
  - `409 Conflict` when editing an `Active` or `Expired` promotion
  - `401` or `403` when the caller isn't an admin
- `GET /api/promotions/active`:
  ```json
  [
    {
      "id": 1, "name": "Fall Hiking Sale", "description": "20% off all hiking gear",
      "type": "PercentOff", "value": 20, "badgeLabel": "-20%",
      "targets": [{ "scope": "Type", "targetId": 3 }],
      "endsAt": "2026-11-16T23:59:59Z", "priority": 10, "requiresCoupon": false
    }
  ]
  ```
  Coupon-only promotions (`requiresCoupon: true`) are left out of this public feed.
- Integration events, published through the outbox (`IntegrationEventLogEF`):
  - `PromotionStartedIntegrationEvent { PromotionId, Name, Description, BadgeLabel, EndsAt, Targets[] }`: sent when a published promotion's `StartsAt` passes (or at once if it's already started when published)
  - `PromotionEndedIntegrationEvent { PromotionId }`: sent when `EndsAt` passes or when `/end` is called

**Acceptance Criteria:**

* **Given** Promotions.API is running and the caller is an admin
* **When** a valid promotion is created
    * **Then** it is saved as `Draft` and returns `201`, and no event is published
* **When** an invalid promotion is created
    * **When** `endsAt <= startsAt`, a `PercentOff` value is outside (0, 100], `AmountOffOrder` has no `minOrderTotal`, `BuyXGetY` is missing a quantity, or `targets` is empty
        * **Then** the request returns `400` with an error naming each failing field, and nothing is saved
* **When** a `Draft` promotion is published
    * **When** its `startsAt` is in the past
        * **Then** `effectiveStatus` is `Active`, and `PromotionStartedIntegrationEvent` is published once
    * **When** its `startsAt` is in the future
        * **Then** `effectiveStatus` is `Scheduled`, and the event is published within 60s of `startsAt`
* **When** an `Active` promotion reaches `endsAt` or `/end` is called
    * **Then** `effectiveStatus` becomes `Expired`, `PromotionEndedIntegrationEvent` is published once, and the promotion drops out of `/active`
* **When** an admin tries to edit an `Active` promotion
    * **Then** the request returns `409 Conflict`
* **Given** an anonymous caller
* **When** they call `GET /api/promotions/active`
    * **Then** they get only active promotions that don't need a coupon, in the public shape above
* **When** they call any admin endpoint
    * **Then** it returns `401`

**Technical Notes:**

* **Interaction history:** This story is PROMO-2 from the [promotions & recommendations epic](epic-promotions-and-recommendations.md). We added `Draft`/`Published` and a `/publish` endpoint so the AI Copilot in [PROMO-7](PROMO-7-ai-marketing-copilot.md) can only create drafts, and a person must publish them. `EffectiveStatus` is calculated rather than stored, so there's no job that has to flip promotions between statuses. A timer is only needed to publish the start and end events.
* **Existing code to reuse:**
    * `src/Webhooks.API/Extensions/Extensions.cs:3-24`: how a service registers the event bus and `AddMigration`
    * `src/IntegrationEventLogEF`: the outbox. Ordering.API is the reference user, via `IOrderingIntegrationEventService`.
    * `src/Catalog.API/Apis/CatalogApi.cs`: minimal API layout with versioned groups, `TypedResults` and paging (`PaginationRequest`, `PaginatedItems` in `src/Catalog.API/Model/`)
* **New code (proposal):**
    * `Model/Promotion.cs` and `Model/PromotionTarget.cs`
    * `Apis/PromotionsApi.cs`
    * a `PromotionLifecycleWorker` (`BackgroundService`, runs every 30s) that publishes the start and end events and records which ones it has sent so they aren't repeated
    * **Admin authorization:** Identity.API has no roles today, so how admins are identified is still open (see below)

**Related Stories:**

* **Part of:** [Promotions & Recommendations epic](epic-promotions-and-recommendations.md)
* **Blocked by:** [PROMO-1](PROMO-1-promotions-service-scaffold.md)
* **Blocks:** [PROMO-3](PROMO-3-cart-pricing-evaluation-api.md), [PROMO-6](PROMO-6-storefront-promotion-display.md), [PROMO-7](PROMO-7-ai-marketing-copilot.md)

**Open Questions:**

* How should admins be identified? Options:
    * add an `admin` role claim in Identity.API and seed it for `alice`
    * allow a configured list of admin user IDs
    * require the `promotions` scope only, for the workshop
* Should the scheduled-start check run every 30s, or should we use a more precise per-promotion scheduler?
* Should `badgeLabel` be calculated (e.g. `-20%`, `Buy 2 Get 1`) or written by the marketer?
