# Ordering and webhook flows

This file maps the checkout, ordering, order processing and webhook flows of eShop. Each flow lists the
Playwright test that covers it in the teststack. Spec files are in `e2e/teststack/` and scenarios are in
`testing/scenarios/`. A **fixme** test asserts the correct behavior for a known app bug and is skipped
until the bug is fixed.

| Spec | Scenario | Tests |
| --- | --- | --- |
| `ordering-checkout.spec.ts` | `ordering-two-buyers` | 6 pass |
| `ordering-api.spec.ts` | `ordering-two-buyers` | 13 pass, 6 fixme |
| `ordering-pipeline.spec.ts` | `ordering-stock` | 5 pass |
| `webhooks-subscriptions.spec.ts` | `webhooks-subscriptions` | 11 pass, 2 fixme |

Users are `alice` and `bob`, password `Pass123$`. Direct API calls use bearer tokens. The helpers in
`ordering-helpers.ts` get them from Identity through the implicit Swagger clients `orderingswaggerui` and
`webhooksswaggerui`.

## Scenarios

| Scenario | State |
| --- | --- |
| `ordering-two-buyers` | alice owns orders 1 to 6, one in each status. Orders 1 and 2 are Submitted but dated 2099, so the pipeline never touches them. Order 4 is Paid and goes to a Boston address. bob owns order 7 (Paid) and order 8 (Shipped). alice's cart has 2 Summit Pro Harness and 1 Expedition Backpack ($309.97). |
| `ordering-stock` | Item 11 has 2 units in stock and item 12 has 0. bob has four past-dated Submitted orders: order 1 is in stock, order 2 exceeds the stock, order 3 has one out-of-stock line and order 4 fits the remaining stock. |
| `webhooks-subscriptions` | Subscriptions: 1 is alice/OrderPaid, 2 is alice/OrderShipped, 3 is bob/CatalogItemPriceChange. All point at `webhooksclient`. alice's order 1 is Paid. Her order 2 is Submitted and becomes eligible for the pipeline about 15 s after loading. |

## Checkout from the shopping bag

- **Entry point:** WebApp `/checkout` (the "Check out" button on `/cart`).
- **Actors:** a signed-in shopper, WebApp, Basket.API, Ordering.API.
- **Steps:**
  1. Sign in and add items to the cart.
  2. Open `/checkout`.
  3. Review the address, which is pre-filled from the profile.
  4. Click "Place order".
- **Expected outcome:**
  - An order is created in Submitted status, with the address, items and total from the cart.
  - The cart is emptied and the user is redirected to `/user/orders`.

| Case | Type | Covered by |
| --- | --- | --- |
| Address is pre-filled from the Identity profile | Positive | `ordering-checkout` › the checkout form is pre-filled from the profile address |
| Order to a new address: Submitted, correct total and items, cart emptied | Positive | `ordering-checkout` › alice places an order to a new address; it is Submitted, her cart is emptied and bob cannot see it |
| Anonymous user is redirected to the Identity login | Negative | `ordering-checkout` › checkout requires sign-in |
| Street, zip code or country is blank: field errors and summary shown, no order created | Negative | `ordering-checkout` › missing required address fields block the order |
| Checkout with an empty cart shows "Your cart is empty" and no form | Edge | `ordering-checkout` › checking out an empty cart shows "Your cart is empty" and places no order |
| Payment card details | Edge | Not covered: the WebApp sends a hard-coded test card, and the form has no card fields |

## Order list and order details

- **Entry point:** WebApp `/user/orders`, Ordering.API `GET /api/orders` and `GET /api/orders/{id}`.
- **Actors:** a signed-in buyer.
- **Steps:** open the orders page, or call the API with a bearer token and `api-version=1.0`.
- **Expected outcome:**
  - The buyer sees only their own orders, with number, date, status and total.
  - Details include the address, items and total.

| Case | Type | Covered by |
| --- | --- | --- |
| Each buyer lists only their own orders | Positive | `ordering-api` › each buyer lists only their own orders, with status and total |
| Details return the address, items and total | Positive | `ordering-api` › order details return the address, items and total; an unknown order is 404 |
| Unknown or non-numeric order id returns 404 | Negative | `ordering-api` › order details return the address, items and total; an unknown order is 404 |
| bob's order list in the UI never shows alice's orders | Negative | `ordering-checkout` › bob's order list never shows alice's orders; `ordering-pipeline` › alice cannot see bob's orders |
| A buyer reads another buyer's order by id | Negative | `ordering-api` › a buyer cannot read another buyer's order details (**fixme**, bug 1) |
| Order details page in the WebApp | Edge | Not covered: the WebApp has no order details page |

## Ordering.API authentication and versioning

- **Entry point:** any `/api/orders` endpoint.
- **Actors:** an anonymous client, or a client with an invalid token.
- **Expected outcome:**
  - Every endpoint requires a valid bearer token with the `orders` scope.
  - The API version must be given and must be supported.

| Case | Type | Covered by |
| --- | --- | --- |
| Anonymous or invalid token on list, details, card types, draft, create, cancel and ship returns 401 | Negative | `ordering-api` › anonymous and invalid-token requests are rejected with 401 |
| Missing `api-version` returns 400; `api-version=2.0` returns 400 `UnsupportedApiVersion` | Negative | `ordering-api` › a missing or unsupported api-version is rejected with 400 |

## Card type lookup

- **Entry point:** `GET /api/orders/cardtypes`.
- **Actors:** a signed-in buyer.
- **Expected outcome:** returns Amex (1), Visa (2) and MasterCard (3).

| Case | Type | Covered by |
| --- | --- | --- |
| Lists the three card types | Positive | `ordering-api` › card type lookup returns Amex, Visa and MasterCard |
| Anonymous request returns 401 | Negative | `ordering-api` › anonymous and invalid-token requests are rejected with 401 |

## Draft order

- **Entry point:** `POST /api/orders/draft`.
- **Actors:** a signed-in buyer.
- **Steps:** post the buyer id and basket items.
- **Expected outcome:** returns the computed items and total, and no order is persisted.

| Case | Type | Covered by |
| --- | --- | --- |
| Items and total are computed; the order list is unchanged | Positive | `ordering-api` › a draft computes items and total from the basket without creating an order |
| An empty item list returns no items and a total of 0 | Edge | `ordering-api` › a draft computes items and total from the basket without creating an order |
| A request without a body returns 400 | Negative | `ordering-api` › a draft request without a body is rejected with 400 (**fixme**, bug 5) |

## Create order through the API

- **Entry point:** `POST /api/orders` with an `x-requestid` header.
- **Actors:** a signed-in buyer.
- **Expected outcome:**
  - A Submitted order is created for the signed-in buyer.
  - A repeated request id is applied only once.

| Case | Type | Covered by |
| --- | --- | --- |
| Posting the same request id twice creates exactly one order | Positive | `ordering-api` › posting the same x-requestid twice creates exactly one Submitted order |
| Missing, malformed or empty-GUID `x-requestid` returns 400 | Negative | `ordering-api` › creating an order requires x-requestid |
| An order with no items, a malformed card number, an expired card or a blank city is not created | Negative | `ordering-api` › an order without items or with an invalid card is not created |
| An invalid order returns 400 instead of 200 | Negative | `ordering-api` › an invalid order is rejected with 400 (**fixme**, bug 3) |
| `userId` in the body cannot place an order for another buyer | Negative | `ordering-api` › an order is created for the signed-in buyer, not the userId in the body (**fixme**, bug 4) |

## Cancel and ship

- **Entry point:** `PUT /api/orders/cancel` and `PUT /api/orders/ship` with `{ "orderNumber": n }` and an
  `x-requestid` header.
- **Actors:** a signed-in buyer, or an admin for shipping.
- **Expected outcome:**
  - Cancel is allowed only before Paid; ship is allowed only from Paid.
  - The description becomes "The order was cancelled." or "The order was shipped.".

| Case | Type | Covered by |
| --- | --- | --- |
| Cancel a Submitted order; a repeated request id is ignored | Positive | `ordering-api` › cancelling a Submitted order succeeds; a repeated x-requestid is ignored |
| Ship a Paid order; cancelling it afterwards fails | Positive | `ordering-api` › shipping a Paid order moves it to Shipped, after which it cannot be cancelled |
| Missing, malformed or empty-GUID `x-requestid` returns 400 | Negative | `ordering-api` › cancel and ship require a valid, non-empty x-requestid header |
| Cancel a Paid or Shipped order, ship a Submitted or Cancelled order, or act on an unknown order: fails, nothing changes | Negative | `ordering-api` › invalid transitions and unknown orders fail and leave orders unchanged |
| Invalid transitions return 4xx, not 500 | Negative | `ordering-api` › invalid transitions return a client error, not 500 (**fixme**, bug 6) |
| A buyer cannot cancel or ship another buyer's order | Negative | `ordering-api` › a buyer cannot cancel or ship another buyer's order (**fixme**, bug 2) |
| A stock-rejected (Cancelled) order cannot be shipped | Negative | `ordering-pipeline` › a stock-rejected order cannot be shipped, a pipeline-paid order can |
| Cancel or ship from the WebApp | Edge | Not covered: the WebApp has no cancel or ship buttons |

## Order status pipeline

- **Entry point:** OrderProcessor polls every 30 s for Submitted orders older than the 1 minute grace period.
- **Actors:**
  - Services: OrderProcessor, Ordering.API, Catalog.API and PaymentProcessor, communicating over RabbitMQ.
- **Steps:**
  1. Submitted moves to AwaitingValidation.
  2. Catalog checks the stock. The order then moves to StockConfirmed, or to Cancelled if stock is short.
  3. Payment succeeds and the order moves to Paid; catalog stock is decremented.
- **Expected outcome:**
  - In-stock orders end up Paid.
  - Orders with any short line end up Cancelled, with the description
    `The product items don't have stock: (<items>).`

| Case | Type | Covered by |
| --- | --- | --- |
| In-stock orders reach Paid, short orders are Cancelled with the rejected items named | Positive and negative | `ordering-pipeline` › the background pipeline pays in-stock orders and cancels orders without enough stock |
| Paid orders decrement stock; cancelled orders leave it untouched | Positive | `ordering-pipeline` › paid orders decrement catalog stock; cancelled orders leave it untouched |
| The order list shows Paid and Cancelled | Positive | `ordering-pipeline` › bob's order list shows the final statuses |
| An order inside the grace period (dated in the future) stays Submitted | Edge | `ordering-api` (orders 1 and 2 of `ordering-two-buyers` stay Submitted throughout the spec) |
| A fresh checkout reaches Paid | Positive | Not covered: it takes 90 to 100 s; past-dated seeded orders cover the same pipeline |
| Payment failure | Negative | Not covered: PaymentProcessor always succeeds in the teststack configuration |

## Webhook subscriptions through the API

- **Entry point:** Webhooks.API `GET`, `POST` and `DELETE` on `/api/webhooks`, with `api-version=1.0`.
- **Actors:** a signed-in user with the `webhooks` scope.
- **Steps:**
  1. POST `{ url, grantUrl, event, token }`.
  2. Webhooks.API sends `OPTIONS` to the grant URL and expects the token echoed back.
  3. On success, the subscription is stored.
- **Expected outcome:**
  - Valid registrations return 201 with a `Location` header.
  - Each user sees and deletes only their own subscriptions.

| Case | Type | Covered by |
| --- | --- | --- |
| A user lists and reads only their own subscriptions | Positive | `webhooks-subscriptions` › each user lists and reads only their own subscriptions |
| Register then delete your own subscription (201, then 202) | Positive | `webhooks-subscriptions` › a subscription can be deleted by its owner only |
| Anonymous or invalid token on list, get, create and delete returns 401; a missing `api-version` returns 400 | Negative | `webhooks-subscriptions` › anonymous and invalid-token requests are rejected with 401 |
| Reading another user's subscription or an unknown id returns 404 | Negative | `webhooks-subscriptions` › each user lists and reads only their own subscriptions |
| Invalid URL, invalid grant URL or unknown event returns 400 with field errors | Negative | `webhooks-subscriptions` › registration validates the URLs, the event name and the grant URL |
| Grant URL on another origin, or a grant URL that is unreachable or does not echo the token, returns 400 and stores nothing | Negative | `webhooks-subscriptions` › registration validates the URLs, the event name and the grant URL |
| A numeric event name outside the enum returns 400 | Negative | `webhooks-subscriptions` › event names outside WebhookType are rejected, including numeric ones (**fixme**, bug 7) |
| Deleting another user's subscription, or one already deleted, returns 404 | Negative | `webhooks-subscriptions` › a subscription can be deleted by its owner only |

## WebhooksClient app

- **Entry point:** WebhooksClient at `http://localhost:18086`.
- **Actors:** a signed-in user, WebhooksClient, Webhooks.API.
- **Steps:**
  1. Log in.
  2. View your registrations.
  3. Open "Add webhook registration", enter a token and click "Register".
- **Expected outcome:**
  - Registrations are listed per user.
  - A new registration is stored and appears in the list.

| Case | Type | Covered by |
| --- | --- | --- |
| Anonymous visitor sees the log-in placeholder and no add link | Negative | `webhooks-subscriptions` › an anonymous visitor is asked to log in and sees no registrations |
| alice sees only her registrations; logging out hides them | Positive | `webhooks-subscriptions` › alice logs in and sees only her registrations |
| bob sees only his registration | Negative | `webhooks-subscriptions` › bob sees his own registration and none of alice's |
| An empty token does nothing | Negative | `webhooks-subscriptions` › registering without a token does nothing |
| Registering from the UI succeeds | Positive | `webhooks-subscriptions` › registering a webhook from the client UI succeeds |
| Delete a registration from the UI | Edge | Not covered: the client has no delete action |

## Webhook delivery

- **Entry point:** integration events `OrderStatusChangedToShipped` and `OrderStatusChangedToPaid` consumed
  by Webhooks.API.
- **Actors:** Ordering.API, Webhooks.API, WebhooksClient.
- **Steps:**
  1. An order changes status.
  2. Webhooks.API posts to every subscription of the matching type, with the `X-eshop-whtoken` header.
  3. WebhooksClient stores the message and lists it under "Webhook messages received".
- **Expected outcome:** each matching subscription receives one message containing the order id.

| Case | Type | Covered by |
| --- | --- | --- |
| Shipping an order delivers OrderShipped | Positive | `webhooks-subscriptions` › shipping an order delivers an OrderShipped webhook to the client |
| The pipeline paying an order delivers OrderPaid | Positive | `webhooks-subscriptions` › an order reaching Paid delivers an OrderPaid webhook to the client |
| Subscriptions of other event types receive nothing | Negative | `webhooks-subscriptions` › subscriptions of other event types receive nothing |
| Catalog price change delivery | Edge | Not covered: the `ProductPriceChangedIntegrationEventHandler` in Webhooks.API is a no-op, so nothing is sent |

## Known issues

| # | Issue | Evidence |
| --- | --- | --- |
| 1 | `GET /api/orders/{id}` has no ownership check | alice's token reads bob's order 7 (200) |
| 2 | Cancel and ship have no ownership or role check | alice ships bob's Paid order 7 (200, status becomes Shipped) |
| 3 | `POST /api/orders` returns 200 when the command fails validation | An order with no items returns 200, and no order is created |
| 4 | `POST /api/orders` trusts `userId` from the body | bob's token creates an order on alice's account |
| 5 | `POST /api/orders/draft` without a body returns 500 | The response is 500, where 400 is expected |
| 6 | Domain rule violations return 500 | Cancelling a Shipped order returns 500 "Cancel order failed to process." |
| 7 | Webhooks.API accepts numeric event names | `event: "99"` returns 201 and stores subscription type 99 |
