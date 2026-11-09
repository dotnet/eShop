# Identity and basket flows

Flow map for sign-in, sessions, the Identity.API OIDC surface, and the shopping bag (basket). Every
covered flow runs against the Docker Compose test stack (`testing/teststack`) from a scenario loaded in
the spec's `beforeAll`. Checkout, ordering, catalog browsing and webhooks are mapped elsewhere.

## Scenarios and specs

| Scenario | Contents | Specs |
| --- | --- | --- |
| `default` | alice and bob (`Pass123$`), full catalog, no baskets | `e2e/teststack/auth-identity-api.spec.ts` |
| `auth-lockout` | `default` plus dave (locked out until 2099), erin (4 failed attempts), gina (lockout expired in 2020), frank (lockout disabled); all `Pass123$` | `e2e/teststack/auth-login.spec.ts` |
| `basket-edge` | `default` plus bob's 2-line basket (3 units), quinn (120 units), oscar (one valid line and one for missing product 99999); alice has no basket | `e2e/teststack/basket-cart.spec.ts`, `e2e/teststack/basket-api.spec.ts` |

Shared helpers live in `e2e/teststack/auth-basket-helpers.ts`. They provide a headless OIDC code flow
for the `webapp` client, a raw h2c gRPC client for Basket.API, and Redis and identitydb readers.

## Bugs found

| Bug | Evidence | Test (fixme) |
| --- | --- | --- |
| Open redirect after sign-in: `/user/login?returnUrl=//evil.example` returns `302 Location: http://evil.example/` | `LogIn.razor` only rejects URIs that parse as absolute; protocol-relative URLs pass | `auth-login.spec.ts` › a protocol-relative returnUrl on /user/login must not redirect off-site |
| A basket line whose product is no longer in the catalog breaks every page for that user | `BasketState.FetchBasketItemsAsync` indexes `catalogItems[item.ProductId]`; WebApp logs `KeyNotFoundException: The given key '99999' was not present in the dictionary`; `CartMenu` renders on every page | `basket-cart.spec.ts` › a basket line for a product no longer in the catalog does not break the site |
| Basket.API stores zero and negative quantities | gRPC `UpdateBasket` with quantities -3 and 0 returns OK and writes them to Redis; `BasketItem.Validate` is never invoked | `basket-api.spec.ts` › UpdateBasket rejects zero or negative quantities |
| Basket.API accepts tokens without the `basket` scope | Identity's `ApiResource`s declare no scopes, so tokens have no `aud`; `AddDefaultAuthentication` sets `ValidateAudience = false` | `basket-api.spec.ts` › a token issued without the basket scope cannot update the basket |
| Typing a cart quantity, pressing Enter, then clicking − can leave Redis on the typed value while the UI shows the decrement | `cart-quantity.js` commits on Enter but leaves the input focused; the click blurs it, fires `change` and re-submits the typed value in parallel with the decrement POST (reproduced: UI 4, Redis 5) | `basket-cart.spec.ts` › typing a quantity, pressing Enter, then clicking Decrease reliably stores the decremented quantity |

## Sign in with valid credentials

- **Entry point:** header "Sign in" icon (`/user/login?returnUrl=...`), which challenges to Identity.API `/Account/Login` via OIDC (PAR + code flow).
- **Actors:** anonymous shopper, WebApp, Identity.API.
- **Steps:** click Sign in, enter user name and password, click Login, get redirected back through `/signin-oidc`.
- **Expected outcome:** the catalog home page with the user menu showing the user name; the Sign in icon is gone.
- **Positive cases:** alice signs in; user names are case-insensitive (`ALICE`); a user whose lockout has expired (gina) signs in; a successful sign-in resets `AccessFailedCount`.
- **Negative/edge cases:** see the next two flows.
- **Covered by:** `auth-login.spec.ts` › anonymous header shows Sign in…, user names are case-insensitive, wrong password is rejected… (counter reset), a user whose lockout has expired can sign in.

## Sign in with bad credentials

- **Entry point:** Identity.API login form.
- **Actors:** shopper, Identity.API, identitydb.
- **Steps:** submit the form with a wrong password, an unknown user, an e-mail address, or empty fields.
- **Expected outcome:** the form is shown again with the generic "Invalid username or password" (or required-field) errors, no WebApp session is created, and failures are counted in `AccessFailedCount`.
- **Positive cases:** a correct retry from the same form signs in.
- **Negative/edge cases:** wrong password; unknown user (same message, so users cannot be enumerated); e-mail address instead of user name (not supported); empty fields (`The Username field is required.`, `The Password field is required.`); a failed login leaves the WebApp anonymous; over HTTP no `.AspNetCore.Identity.Application` cookie is set.
- **Covered by:** `auth-login.spec.ts` › wrong password…, unknown user…, sign-in by e-mail address is not supported, empty user name and password…, a failed login does not create a WebApp session; `auth-identity-api.spec.ts` › a wrong password over HTTP….

## Account lockout

- **Entry point:** Identity.API login form (`PasswordSignInAsync(..., lockoutOnFailure: true)`, default limit of 5 failures).
- **Actors:** shopper, Identity.API, identitydb.
- **Steps:** fail the login until the limit is reached, then try the correct password.
- **Expected outcome:** after the fifth failure `LockoutEnd` moves into the future and `AccessFailedCount` resets to 0; the correct password is then rejected with the same generic message.
- **Positive cases:** an expired lockout (gina) does not block sign-in; a user with `LockoutEnabled = false` (frank) can sign in after six failures.
- **Negative/edge cases:** a seeded locked-out user (dave) is rejected with the correct password and `LockoutEnd` is unchanged; erin's fifth failure locks her out.
- **Covered by:** `auth-login.spec.ts` › a locked-out user cannot sign in…, the fifth failed attempt locks the account…, a user with lockout disabled is never locked out….
- **Note:** for frank, Identity still writes a `LockoutEnd` after five failures, but `LockoutEnabled = false` wins at sign-in.

## Registration

- **Entry point:** none. Identity.API has no registration UI or API (`/Account/Register` returns 404); users only come from seeding or scenarios.
- **Covered by:** `auth-identity-api.spec.ts` › Identity.API has no self-service registration page.

## Protected pages for anonymous users

- **Entry point:** direct navigation to `/cart`, `/checkout` or `/user/orders` (`[Authorize]`).
- **Actors:** anonymous shopper, WebApp, Identity.API.
- **Steps:** open the page without a session.
- **Expected outcome:** a redirect to `http://host.docker.internal:18081/Account/Login?ReturnUrl=...`; after signing in the user lands on the page they asked for.
- **Positive cases:** sign in from `/user/orders` and land on Orders; the cart is reachable afterwards.
- **Negative/edge cases:** each of the three pages redirects; the anonymous item page shows "Log in to purchase" instead of "Add to shopping bag".
- **Covered by:** `auth-login.spec.ts` › anonymous /cart|/checkout|/user/orders redirects…, after signing in from a protected page…, "Log in to purchase" on an item page….
- **Not covered:** completing checkout after the redirect (owned by the checkout/ordering flow map).

## returnUrl handling

- **Entry point:** `/user/login?returnUrl=<url>` in the WebApp and the hidden `ReturnUrl` field of the Identity login form.
- **Actors:** signed-in or signing-in shopper, WebApp `LogIn.razor`, Identity `AccountController`.
- **Steps:** request the URL with a relative, absolute or protocol-relative return URL, or post the login form with a tampered `ReturnUrl`.
- **Expected outcome:** relative URLs are followed; anything off-site goes to the home page or is refused.
- **Positive cases:** `returnUrl=item/7` goes to `/item/7`; "Log in to purchase" returns to the item page without adding it to the bag.
- **Negative/edge cases:** `https://evil.example/` falls back to `/`; a tampered Identity `ReturnUrl` is refused (500 "invalid return URL", no redirect); `//evil.example` redirects off-site (bug, fixme).
- **Covered by:** `auth-login.spec.ts` › a relative returnUrl…, an absolute returnUrl…, a protocol-relative returnUrl… (fixme); `auth-identity-api.spec.ts` › a tampered off-site ReturnUrl….

## Session persistence

- **Entry point:** any page after signing in.
- **Actors:** shopper, WebApp cookie (`SessionCookieLifetimeMinutes`), Identity cookie.
- **Steps:** reload, open another tab in the same browser context, or use a separate context.
- **Expected outcome:** the session survives reloads and new tabs; another context is anonymous. Without "Remember me" the Identity cookie is a session cookie; with it, it is persistent (the 14-day ASP.NET Identity default).
- **Positive cases:** reload and a new tab stay signed in.
- **Negative/edge cases:** a second context is anonymous; the cookie expiry differs with and without "Remember me" (`AccountOptions.RememberMeLoginDuration` of 30 days is not used).
- **Covered by:** `auth-login.spec.ts` › the session survives reloads and new tabs…, separate browser contexts keep separate sessions, "Remember me" makes the Identity session cookie persistent….
- **Not covered:** cookie expiry after `SessionCookieLifetimeMinutes` (60 minutes; too slow for the suite).

## Log out

- **Entry point:** user menu › Log out (POST `user/logout`, which signs out of the cookie and OIDC schemes).
- **Actors:** shopper, WebApp, Identity.API end-session endpoint.
- **Steps:** open the user menu, click Log out.
- **Expected outcome:** back on the WebApp as an anonymous visitor; the Identity session is gone, so protected pages ask for credentials again instead of signing in silently; the basket is kept.
- **Positive cases:** a different user can sign in afterwards in the same browser.
- **Negative/edge cases:** `/cart` after log out shows the Identity login form; the header bag count disappears.
- **Covered by:** `auth-login.spec.ts` › log out ends both the WebApp and the Identity session, a different user can sign in after log out…; `basket-cart.spec.ts` › the basket persists across log out and log in.

## Identity.API OIDC discovery and tokens

- **Entry point:** `/.well-known/openid-configuration`, `/jwks`, `/connect/authorize`, `/connect/token`, `/connect/userinfo`.
- **Actors:** OIDC clients (`webapp`), APIs validating tokens.
- **Steps:** read discovery and JWKS; run the code flow for `webapp`; call the token and userinfo endpoints with bad input.
- **Expected outcome:** the issuer equals the requested origin (`http://host.docker.internal:18081` for the containers); tokens carry `sub`, `client_id` and the requested scopes; userinfo returns the user.
- **Positive cases:** discovery lists the endpoints, scopes and S256 PKCE; JWKS has an RSA signing key; the code flow issues access and ID tokens for alice.
- **Negative/edge cases:** unknown client and wrong secret (`invalid_client`); password and client_credentials grants for `webapp` (`unauthorized_client`); a bogus code (`invalid_grant`); userinfo without or with a garbage token (401 `invalid_token`); authorize with an unknown client or an unregistered `redirect_uri` goes to the Identity error page, not the redirect URI; login posts without an antiforgery token (400).
- **Covered by:** `auth-identity-api.spec.ts` (all tests).
- **Not covered:** refresh tokens and revocation (the WebApp never uses them); the `maui` client (its `MauiCallback` redirect URI is not configured in the test stack).

## Empty bag view

- **Entry point:** `/cart` for a user without a basket.
- **Actors:** signed-in shopper, WebApp, Basket.API.
- **Expected outcome:** "Your shopping bag is empty", no Check out link, a "Start shopping" link to the catalog, and a header bag icon without a count.
- **Covered by:** `basket-cart.spec.ts` › a user without a basket sees the empty-bag view….

## Add to bag from an item page

- **Entry point:** `/item/{id}` › "Add to shopping bag" (signed in).
- **Actors:** shopper, WebApp `BasketState`, Basket.API, Redis `/basket/<userId>`.
- **Steps:** open an item, click Add to shopping bag (repeat for quantity merge, add a second product).
- **Expected outcome:** "N in your shopping bag" appears; the header shows the total count with a matching aria-label; Redis holds one line per product.
- **Positive cases:** first add (quantity 1); adding the same item twice merges into one line with quantity 2; a second product adds a line and the totals add up.
- **Negative/edge cases:** anonymous visitors only get "Log in to purchase" and no basket key is written.
- **Covered by:** `basket-cart.spec.ts` › adding an item from its page…, adding the same item twice…, anonymous visitors cannot add to the bag….

## Update quantity and remove lines

- **Entry point:** `/cart` quantity stepper, typed quantity (Enter or change), Remove button (enhanced form posts to `/cart`).
- **Actors:** shopper, WebApp, Basket.API, Redis.
- **Steps:** increase, decrease, type a value, type 0 or a negative number, remove lines.
- **Expected outcome:** line totals, the bag total, the header count and Redis follow each change; removing the last line shows the empty view and leaves an empty basket.
- **Positive cases:** + goes from 1 to 2; typing 5 sets 5; − goes from 5 to 4; removing one of two lines keeps the other.
- **Negative/edge cases:** − is disabled at quantity 1; typing 0 or −4 is clamped to 1 by `cart-quantity.js` (it does not remove the line); removing the last line; type + Enter followed by − races two POSTs (bug, fixme).
- **Covered by:** `basket-cart.spec.ts` › quantity can be increased, decreased and typed…, typing 0 or a negative quantity is clamped to 1…, removing lines updates the bag…, typing a quantity, pressing Enter, then clicking Decrease… (fixme).

## Seeded baskets and the header badge

- **Entry point:** any page after signing in (the `CartMenu` header component).
- **Actors:** shopper with a pre-existing Redis basket.
- **Expected outcome:** the header shows the total unit count (capped at `99+`, while the aria-label keeps the real number); the cart uses catalog prices.
- **Positive cases:** bob sees 2 lines and 3 units; quinn sees `99+`, an aria-label of 120 items and a $8,398.80 total.
- **Negative/edge cases:** a line for a product that is no longer in the catalog (oscar) crashes every page (bug, fixme).
- **Covered by:** `basket-cart.spec.ts` › a seeded basket is shown…, the header count caps at 99+…, a basket line for a product no longer in the catalog… (fixme). The stale stored price case is covered by `returning-customers.spec.ts`.

## Basket persistence and isolation

- **Entry point:** add to bag, then log out and back in, use a second session, or sign in as another user.
- **Actors:** alice, bob, Redis.
- **Expected outcome:** the basket survives log out and log in (it lives in Redis under the user ID); two sessions of one user share one basket; users never see each other's lines, and bob's Redis value is unchanged by alice's actions.
- **Covered by:** `basket-cart.spec.ts` › the basket persists across log out and log in, two sessions of the same user share one basket, baskets are isolated between users.

## Basket gRPC API

- **Entry point:** `BasketApi.Basket/GetBasket|UpdateBasket|DeleteBasket` over h2c on `localhost:18083`.
- **Actors:** API clients with or without a bearer token from Identity.API.
- **Steps:** call each method anonymously, with malformed or forged tokens, and with alice's token from the `webapp` code flow.
- **Expected outcome:** the user always comes from the token's `sub`, so a caller can only touch their own basket; writes need a valid token.
- **Positive cases:** with alice's token: Get (empty), Update (stored in `/basket/<alice>`), Get (same items), Delete (key removed); an empty update leaves an empty basket.
- **Negative/edge cases:** anonymous Get returns an empty basket; anonymous Update and Delete return `UNAUTHENTICATED`; malformed and tampered-signature tokens return `UNAUTHENTICATED`; bob's basket is checked unchanged after every test; zero or negative quantities are accepted (bug, fixme); tokens without the `basket` scope are accepted (bug, fixme).
- **Covered by:** `basket-api.spec.ts`.
- **Not covered:** the Mobile BFF basket routes (no client in the stack has a usable token flow: the `maui` client has no redirect URI configured).
