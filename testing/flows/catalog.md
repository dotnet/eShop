# Catalog flows

Flow map for catalog browsing in the WebApp, the Catalog.API REST endpoints, the mobile BFF catalog routes and the catalog admin (write) endpoints. Every flow runs against the Docker Compose test stack (`testing/README.md`). Identity, basket and ordering flows are mapped elsewhere.

## Specs and scenarios

| Spec | Scenario | Scope |
| --- | --- | --- |
| `e2e/teststack/catalog-edge.spec.ts` | `catalog-edge` | Existing: prices, two-page pagination, facet counts, empty states, long names, zero stock |
| `e2e/teststack/catalog-browse.spec.ts` | `catalog-edge` | WebApp filters, sorting, product page, back link, zoom, not-found handling, image proxy |
| `e2e/teststack/catalog-paging.spec.ts` | `default` | 12-page pager with ellipses, Next/Prev, last page |
| `e2e/teststack/catalog-api.spec.ts` | `catalog-pictures` | Catalog.API read endpoints, filters, pictures, versioning and routing errors |
| `e2e/teststack/catalog-admin.spec.ts` | `catalog-edge` (reloaded before every test) | Create, update and delete items, and their validation |
| `e2e/teststack/catalog-mobile-bff.spec.ts` | `catalog-edge` | YARP catalog routes on the mobile BFF |

`catalog-pictures` extends `catalog-edge` with item 15 (no picture file name), item 16 (`missing.webp`, which does not exist) and item 17 (`Edge 17 100% Wool_Sock`, a name with SQL `LIKE` wildcards). It has 17 items.

Run one file at a time, under the shared stack lock:

```bash
lockf -k /tmp/eshop-teststack.lock npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/catalog-api.spec.ts --reporter=line
```

## Bugs found

These tests assert the correct behavior and are marked `test.fixme` until the app is fixed.

| Bug | Observed | Test |
| --- | --- | --- |
| Negative `pageSize` or `pageIndex` reach Postgres | 500, `LIMIT must not be negative` | `catalog-api` "negative pageSize or pageIndex is rejected with 400" |
| WebApp `?page=0` and `?page=-1` request `pageIndex` -1 | Skeleton stays, "An unhandled error has occurred." | `catalog-browse` "?page=0 shows the first page", "?page=-1 ..." |
| Create without `name` | 500 (NOT NULL violation) | `catalog-admin` "a body without a name is rejected with 400" |
| Create with an empty name or a negative price | 201, stored | `catalog-admin` "an empty name or a negative price is rejected with 400" |
| Create with an existing id | 500 (primary key violation) | `catalog-admin` "creating an item with an existing id is rejected with 409" |
| Create or update with an unknown brand or type id | 500 (foreign key violation) | `catalog-admin` "unknown brand or type ids ...", "v2 PUT with an unknown type id is 400" |
| v2 PUT body id differs from the route id | 500 | `catalog-admin` "v2 PUT with a body id different from the route id is 400" |
| v1 PUT without an id | 404 "Item with id 0 not found." (the null check on an `int` is dead code) | `catalog-admin` "v1 PUT without an id in the body is 400" |
| Write endpoints have no authorization | Anonymous POST, PUT and DELETE succeed | `catalog-admin` "anonymous callers cannot create, update or delete items" |

## WebApp flows

### Browse the catalog

- **Entry point:** `GET http://localhost:18080/`
- **Actors:** anonymous or signed-in shopper.
- **Steps:** open the home page. The page streams a loading skeleton, then shows 9 products per page sorted by name, the product count, the Brand and Type filter panel, a Sort control and the pager.
- **Expected outcome:** each card shows the image (`product-images/{id}?api-version=2.0`), brand chip, name and price, and links to `item/{id}`.
- **Positive cases:** count and card contents, formatted prices, loaded images (`catalog-browse` "home page lists products ..."; `catalog-edge` "first page shows 9 of 14 products ...").
- **Negative and edge cases:** an empty catalog is not covered (no products, not a scenario in this area); zero-stock items are listed without a marker (`catalog-edge` "the zero-stock item is listed ...").

### Paginate

- **Entry point:** `GET /?page={n}`
- **Actors:** shopper.
- **Steps:** use the numbered links, Prev or Next. Page 1 drops the `page` parameter.
- **Expected outcome:** the pager shows the first page, the current page plus or minus one and the last page, with ellipses for gaps, and `current / total`.
- **Positive cases:** 12 pages over 101 items, ellipses, Next/Prev, a partial last page, all 101 products across pages (`catalog-paging`, all tests); two pages (`catalog-edge` "pagination has exactly two pages").
- **Negative and edge cases:**
  - Page beyond the last: the count still shows, with no cards and no empty state (`catalog-browse` "a page beyond the last one lists no products"; `catalog-edge` uses `?page=3`).
  - `?page=abc`: 404 not-found page (`catalog-browse` "non-numeric query ?page=abc ...").
  - `?page=0` and `?page=-1`: unhandled error (bug, `catalog-browse` fixme tests).

### Filter by brand and type

- **Entry point:** `GET /?brand={id}&brand={id}&type={id}` or the filter tags.
- **Actors:** shopper.
- **Steps:** click brand and type tags. A tag toggles its id in the query; "All" clears its facet; "Clear all filters" clears both. Changing a filter resets the page.
- **Expected outcome:** brands combine with OR, types combine with OR, and brand and type intersect. Tag badges show contextual counts from `/api/catalog/items/facets`, and tags with a zero count are marked `is-empty` and `aria-disabled`.
- **Positive cases:**
  - Toggling and combining tags, the active filter badge (`catalog-browse` "clicking brand tags toggles them ...").
  - Brand and type intersection, "All", "Clear all filters" (`catalog-browse` "brand and type tags intersect ...").
  - Page reset (`catalog-browse` "changing a filter resets the page number").
  - Facet counts and intersections by URL (`catalog-edge` "brand and type facets show contextual counts", "filtering by a brand or type ...").
- **Negative and edge cases:**
  - Unused, unknown or negative ids show the "No products match these filters" empty state (`catalog-browse` "unknown brand and type ids show the empty state"; `catalog-edge` "brand without items ...", "type without items ...", "filter combination with no match ...").
  - `?brand=abc` and `?type=1.5`: 404 not-found page (`catalog-browse` "non-numeric query ...").

### Sort the current page

- **Entry point:** the Sort combobox on `/`.
- **Actors:** shopper.
- **Steps:** open the combobox and pick Name A to Z, Name Z to A, Price Low to High or Price High to Low.
- **Expected outcome:** the cards of the current page are reordered in the browser. The choice is stored in `localStorage` and applied to later pages. Sorting does not change which items are on a page.
- **Positive cases:** price and name sorting, persistence to page 2 (`catalog-browse` "sorting by price reorders the current page ...").
- **Negative and edge cases:** sorting across pages is not supported by design (the same test asserts page 2 keeps items 10 to 14). Keyboard operation of the combobox is not covered (no keyboard scenario in this area).

### View a product

- **Entry point:** `GET /item/{id}`
- **Actors:** anonymous or signed-in shopper.
- **Steps:** click a card, or deep link.
- **Expected outcome:** the title is `{name} | AdventureWorks`, with the heading, image, description, brand and price. Anonymous users see "Log in to purchase", which goes to the Identity login page. "Back to catalog" returns to the filtered list the user came from, or to `/` after a deep link. The image opens in a zoom dialog.
- **Positive cases:**
  - Back link with filters, page title, description (`catalog-browse` "opening a product from a filtered list ...").
  - Deep link back to `/` (`catalog-browse` "a deep link to a product goes back to the unfiltered catalog").
  - Zoom dialog (`catalog-browse` "the product image opens in a zoom dialog").
  - Anonymous purchase sends to login (`catalog-browse` "anonymous \"Log in to purchase\" ...").
  - Prices, long names, empty description, images (`catalog-edge`).
- **Negative and edge cases:**
  - `/item/9999` (`catalog-edge` "unknown product id shows the not-found message").
  - `/item/0`, `/item/-1`, `/item/abc`, `/item/2147483648` and `/item/1/extra` all return 404 with the not-found page (`catalog-browse` "invalid product URL ...").
  - Unknown routes return 404 with a "Back to catalog" link (`catalog-browse` "unknown routes return the not-found page ...").
  - Adding to the cart is covered by the basket flow map, not here.

### Product image proxy

- **Entry point:** `GET /product-images/{id}?api-version=2.0` on the WebApp, forwarded to `/api/catalog/items/{id}/pic`.
- **Actors:** browser.
- **Steps:** the browser loads card and product images.
- **Expected outcome:** 200 `image/webp` for seeded items.
- **Positive cases:** `catalog-browse` "the product-images proxy serves pictures ...", "home page lists products ..."; `catalog-edge` "product images are served ...".
- **Negative and edge cases:** unknown or non-integer ids return 404; without `api-version` the API answers 400 because the query is forwarded unchanged (same test). An item with no picture renders a broken image on the product page: not covered (no placeholder exists to assert on; the API 404 is covered in `catalog-api`).

## Catalog.API read flows

Base URL `http://localhost:18082/api/catalog`. Every request needs `?api-version=1.0` (or `1`) or `2.0`. Responses carry `api-supported-versions: 1.0, 2.0`. There is no authentication.

### List items

- **Entry point:** `GET /items?api-version=1.0|2.0&pageSize=10&pageIndex=0` (v2 adds `name`, `type[]` and `brand[]`).
- **Actors:** WebApp, mobile clients, any HTTP client.
- **Steps:** request a page.
- **Expected outcome:** `{ pageIndex, pageSize, count, data[] }` sorted by name, with `catalogBrand` included and `embedding` omitted.
- **Positive cases:**
  - v1 defaults and `"1"` as the version (`catalog-api` "v1 returns the first page ...", "\"1\" is accepted ...").
  - Paging windows, a page past the end, `pageSize=0` (`catalog-api` "pageSize and pageIndex select a window ...", "a page beyond the end ...").
  - v2 name prefix (case-sensitive), literal `%` and `_`, additive and intersecting type and brand filters (`catalog-api` "v2 filters" tests).
- **Negative and edge cases:**
  - v1 silently ignores `name`, `type` and `brand` (`catalog-api` "v1 ignores the v2-only ...").
  - Unknown, unused or negative filter ids return an empty page (`catalog-api` "unknown or unused brand and type ids ...").
  - Non-integer paging or filter values return 400 (`catalog-api` "non-numeric paging parameters ...", "non-integer filter ids ...").
  - Negative paging returns 500 (bug, `catalog-api` fixme).

### Get one item

- **Entry point:** `GET /items/{id:int}`
- **Actors:** WebApp product page, clients.
- **Steps:** request by id.
- **Expected outcome:** the item with its brand, under v1 and v2.
- **Positive cases:** `catalog-api` "returns the item with its brand under both versions".
- **Negative and edge cases:** unknown id returns 404 problem details; `0` and negative ids return 400 "Id is not valid"; `abc`, `1.5` and `2147483648` do not match the route and return 404 (`catalog-api` "single item" tests).

### Batch get by ids

- **Entry point:** `GET /items/by?ids=1&ids=2`
- **Actors:** Basket and ordering services, clients.
- **Steps:** pass repeated `ids`.
- **Expected outcome:** the matching items, without `catalogBrand`, in no guaranteed order.
- **Positive cases:** `catalog-api` "returns the existing items and skips unknown and duplicate ids".
- **Negative and edge cases:** no ids returns `[]`; a non-integer id returns 400 (`catalog-api` "batch get by ids" tests).

### v1-only lookups by name, type and brand

- **Entry point:** `GET /items/by/{name}`, `/items/type/{typeId}/brand/{brandId?}`, `/items/type/all/brand/{brandId?}` (v1 only).
- **Actors:** legacy clients.
- **Steps:** request with path parameters.
- **Expected outcome:** paginated results; the name is a prefix match.
- **Positive cases:** `catalog-api` "by name returns a paginated prefix match", "by type and brand, by type only, by brand only and all".
- **Negative and edge cases:** non-integer type or brand ids are rejected; v2 requests return 400 `UnsupportedApiVersion` (`catalog-api` "non-integer type or brand ids are rejected", "are not available in v2").

### Semantic search with AI disabled

- **Entry point:** v1 `GET /items/withsemanticrelevance/{text}`, v2 `GET /items/withsemanticrelevance?text=`.
- **Actors:** clients.
- **Steps:** search by text.
- **Expected outcome:** with AI off the API falls back to the name prefix search.
- **Positive cases:** `catalog-api` "falls back to a name prefix match in v1 and v2".
- **Negative and edge cases:** v2 without `text` returns 400 (`catalog-api` "v2 requires the text parameter"). Real vector search is not covered: AI features are off in the test stack.

### Brands, types and facets

- **Entry point:** `GET /catalogbrands`, `GET /catalogtypes`, `GET /items/facets?type=&brand=`
- **Actors:** WebApp filter panel, clients.
- **Steps:** request the lists and counts.
- **Expected outcome:** brands and types sorted by name, including unused ones. Facets return per-brand counts scoped by the type filter, per-type counts scoped by the brand filter, and totals. Ids with no items are absent.
- **Positive cases:** `catalog-api` "brands and types are listed alphabetically ...", "facets count every brand and type ...", "brand counts are scoped by type ...".
- **Negative and edge cases:** unknown ids return empty counts and zero totals; non-integer filters return 400 (`catalog-api` "brand counts are scoped ...", "non-integer facet filters ...").

### Item pictures

- **Entry point:** `GET /items/{id}/pic`
- **Actors:** WebApp proxy, mobile BFF, clients.
- **Steps:** request the picture.
- **Expected outcome:** the file from `Pics/`, with a content type from its extension and `Last-Modified`.
- **Positive cases:** `catalog-api` "serves the picture with its image content type and Last-Modified".
- **Negative and edge cases:** a null `PictureFileName`, a missing file and an unknown item all return 404 (`catalog-api` "an item without a picture, a missing file and an unknown item are 404").

### API versioning and routing errors

- **Entry point:** any catalog route.
- **Actors:** clients.
- **Steps:** send a request with a missing, unsupported, invalid or repeated `api-version`, an unknown path or an unsupported method.
- **Expected outcome:** 400 problem details with `ApiVersionUnspecified`, `UnsupportedApiVersion`, `InvalidApiVersion` or `AmbiguousApiVersion`; 404 for unknown paths; 405 for unsupported methods. The version is read only from the query string. OpenAPI documents exist at `/openapi/v1.json` and `/openapi/v2.json`.
- **Positive cases:** `catalog-api` "OpenAPI documents are published per version".
- **Negative and edge cases:** `catalog-api` "API versioning and routing errors" tests.

## Catalog admin (write) flows

There is no authentication on these endpoints (bug, see above). A price change on update saves a `ProductPriceChangedIntegrationEvent` and publishes it; its effect on baskets belongs to the basket flow map.

### Create an item

- **Entry point:** `POST /items?api-version=1.0|2.0` with a JSON `CatalogItem`.
- **Actors:** catalog administrator (any caller today).
- **Steps:** post the item, then read it back.
- **Expected outcome:** 201 with `Location: /api/catalog/items/{id}`. The item appears in the API, the catalog count and its product page.
- **Positive cases:** `catalog-admin` "POST creates an item that the API, the catalog page and the product page show", "POST works under v2 and honours an explicit id".
- **Negative and edge cases:**
  - Malformed, empty, array and wrongly typed bodies return 400 with nothing stored (`catalog-admin` "malformed, empty, wrongly typed ...").
  - `text/plain` returns 415 (`catalog-admin` "a non-JSON content type is 415").
  - A missing or unsupported version returns 400 (`catalog-admin` "a missing or unsupported API version is 400").
  - Missing name, empty name, negative price, duplicate id, unknown brand or type, anonymous caller: bugs, `catalog-admin` fixme tests.

### Update an item

- **Entry point:** v1 `PUT /items` (id in the body), v2 `PUT /items/{id}`.
- **Actors:** catalog administrator.
- **Steps:** send the full item.
- **Expected outcome:** 201 with `Location`. The whole item is replaced: fields left out are reset to their defaults. The WebApp shows the new values.
- **Positive cases:** `catalog-admin` "v2 PUT /items/{id} replaces the item ...", "v1 PUT /items takes the id from the body and replaces every field".
- **Negative and edge cases:**
  - Unknown id returns 404 "Item with id N not found." (`catalog-admin` "updating an unknown id is 404 ...").
  - Each route rejects the other version with 400 (`catalog-admin` "each PUT route only exists in its own version").
  - A malformed body returns 400 and a non-integer id returns 404, with no change (`catalog-admin` "a malformed body or a non-integer id ...").
  - v1 without an id, v2 with mismatched ids, unknown type id: bugs, `catalog-admin` fixme tests.

### Delete an item

- **Entry point:** `DELETE /items/{id}?api-version=1.0|2.0`
- **Actors:** catalog administrator.
- **Steps:** delete, then read back.
- **Expected outcome:** 204 with an empty body. The item is gone from the API and the WebApp (`/item/{id}` returns 404, the count drops).
- **Positive cases:** `catalog-admin` "DELETE removes the item from the API and the WebApp; a second delete is 404".
- **Negative and edge cases:** unknown, `0`, negative and non-integer ids return 404 with nothing changed; a missing version returns 400 (`catalog-admin` "deleting unknown, zero or non-integer ids ...", "DELETE without an API version ..."). Deleting an item that is in a basket or an order is not covered here (basket and ordering flow maps).

## Mobile BFF catalog flows

### Catalog routes through YARP

- **Entry point:** `http://localhost:18087/catalog-api/api/catalog/...` (prefix stripped) and the catch-all `http://localhost:18087/api/catalog/{**any}`.
- **Actors:** the mobile client app.
- **Steps:** call a catalog route with an `api-version` value listed for that route.
- **Expected outcome:** the request is forwarded to Catalog.API and the response, including errors and images, passes through unchanged.
- **Positive cases:** `catalog-mobile-bff` "the /catalog-api prefix is stripped ...", "every documented catalog route answers through the BFF", "pictures are proxied ...", "the generic /api/catalog catch-all ...".
- **Negative and edge cases:**
  - Catalog.API 400 and 404 responses pass through (`catalog-mobile-bff` "Catalog.API errors pass through unchanged").
  - A missing or unlisted `api-version` (including `2`) matches no route and returns 404 instead of the API's 400 (`catalog-mobile-bff` "a missing or unlisted api-version matches no BFF route ...").
  - v1-only routes return 404 for 2.0. The v2 search path with 1.0 is caught by the unconstrained `/items/{id}` route and Catalog.API returns 400 (`catalog-mobile-bff` "v1-only routes are not routed for version 2.0").
  - Unknown paths return 404 (`catalog-mobile-bff` "unknown paths under the BFF are 404").
  - Write methods through the BFF are not covered: the routes allow every method, so they behave like the direct API writes covered in `catalog-admin`.
