# eShop test stack

A reproducible Docker Compose deployment of the whole app. You put it into a known state from a **scenario file** that seeds the databases and Redis directly. It is built for agents and automated tests: every command blocks until the stack is healthy and prints one JSON result line.

Aspire (`aspire start`) remains the day-to-day dev loop. Use this stack for scenario-driven test runs.

## Quick start

```bash
testing/teststack up                          # build images if sources changed, start, wait, load "default"
testing/teststack load order-history          # clear everything and load another scenario (~0.6 s; ~2 s if ordering-api must restart)
testing/teststack reset                       # clear everything, base lookup data only
npx playwright test -c playwright.teststack.config.ts
testing/teststack down                        # remove containers and all data
```

Requires Docker (or a compatible engine via `TESTSTACK_DOCKER`), the .NET SDK, and the Aspire CLI (`scripts/setup.sh` installs them).

## How the stack is produced

| Piece | Source |
| --- | --- |
| `deploy/teststack/docker-compose.yaml`, `.env` | **Generated** by `testing/teststack publish` (`TestStack=true aspire publish`). Never edit them by hand. |
| Compose customizations (ports, HTTP-only, seeder switch, identity URL) | C# in `src/eShop.AppHost/TestStack.cs`, enabled by `TestStack=true` in `AppHost.cs` |
| Service images | `aspire do prepare-teststack`, which uses the same `dotnet publish` container build as production |
| Resolved image tags, ports, test passwords | `artifacts/teststack/.env.teststack` (generated, git-ignored) |

The only test-only differences from production are:

- **The seeder switch.** `Seeding__Disabled=true` makes `MigrateDbContextExtensions` skip `CatalogContextSeed`, `UsersSeed` and `OrderingContextSeed`. EF migrations still run at service startup, so the services own the schema.
- **The scenario loader** (this folder).

CI runs `testing/teststack publish --check`, which regenerates the files and fails if they differ from the committed ones. After changing the AppHost, run `testing/teststack publish` and commit the result.

`build` (which `up` runs for you) hashes `src/` plus the build props. It rebuilds the images only when that hash changes or an image is missing. Use `--force` to rebuild anyway. A full build takes about 1.5 minutes, and `up` from built images takes about 35 s.

Images are built for the host's Linux architecture (arm64 on Apple silicon), so they run without emulation. Production publishing still targets linux-amd64. Set `TestStackPlatform=linux-amd64` (or `linux-arm64`) to override. Images get a stable local tag, `<service>:teststack`.

On macOS without Docker Desktop, [colima](https://github.com/abiosoft/colima) works: `brew install colima docker docker-compose`, then `colima start --cpu 8 --memory 16 --vm-type vz --mount-type virtiofs`. `host.docker.internal` resolves inside containers with both.

## Commands

| Command | Description |
| --- | --- |
| `up [--scenario <name\|path>] [--timeout s]` | Build if needed, `compose up -d`, wait, then load the scenario (default: `default`). |
| `down` | `compose down -v`: removes containers and all data. |
| `build [--force]` | Build the images and the loader, only when needed. |
| `load <name\|path> [--hard] [--timeout s]` | Clear all state, then load `_base`, then the scenario's `extends` chain, then the scenario itself. |
| `reset [--hard] [--timeout s]` | Same as `load` with only `_base`. |
| `wait [--timeout s]` | Block until the stack is ready (default timeout: 300 s). |
| `status` | One readiness pass, without waiting. |
| `validate [<name\|path>...]` | Parse and resolve scenarios (all by default). Needs no running stack. |
| `hash-password <pw>` | Print an ASP.NET Identity V3 hash for `AspNetUsers.PasswordHash`. |
| `publish [--check]` | Regenerate `deploy/teststack`. `--check` fails on drift. |
| `logs [svc...]`, `compose <args>` | Run `docker compose` against the stack. |

**Output contract:** logs go to stderr, and stdout gets exactly one JSON line, for example:

```json
{"ok":true,"command":"load","scenario":"order-history","layers":["_base","default","order-history"],"mode":"soft","purgedMessages":0,"restarted":["ordering-api"],"elapsedMs":1840}
```

When `ok` is false, the line also contains `error`. `status` and `wait` failures list the checks that are not ready.

**Exit codes:**

| Code | Meaning |
| --- | --- |
| 0 | OK |
| 1 | Error |
| 2 | Not ready (timeout) |
| 3 | Invalid scenario |
| 64 | Usage error |

### Readiness

A service counts as ready when all of the following hold:

- Its container is running.
- Postgres, Redis and RabbitMQ answer.
- Its HTTP probe answers. The services run as Production, where `/health` is not mapped. Kestrel only starts listening once migrations finish, so any response below 500 means migrations are done. Basket uses an h2c probe.
- Its database has an `__EFMigrationsHistory` table.
- Each of its RabbitMQ queues exists and has at least one consumer.

A service that stays unready for 60 s while the infrastructure is healthy is restarted once. This covers the event bus, which never reconnects if its first connection fails.

## Reset semantics

**Soft (default)** takes about 0.6 s, or about 2 s when `ordering-api` has to restart (see below). It never touches `__EFMigrationsHistory`. In order, it:

1. Purges the RabbitMQ queues.
2. Truncates every table in every database except `__EFMigrationsHistory` (`RESTART IDENTITY CASCADE`). This includes the outbox and `IntegrationEventLog` tables.
3. Flushes Redis.
4. Purges the queues again.
5. Loads the scenario layers, one transaction per database.
6. Moves every identity and HiLo sequence to `MAX(Id)+1`.

Ordering.API caches HiLo ID blocks in memory. If any of its HiLo sequences has been used since the last load, the reset restarts `ordering-api`, so new orders never collide with seeded IDs. That restart is reported in `restarted`.

**Hard (`--hard`)** takes about 4-7 s. It stops the app services and recreates each database from `<db>__template`, a snapshot taken right after migrations ran on empty databases. It then flushes Redis, purges the queues, restarts the services and loads the scenario. Templates are created on first use and rebuilt when the migration history changes. Use hard mode when a test changed the schema or left state outside tables.

## Scenario files

Scenarios live in `testing/scenarios/<name>/scenario.yaml`. JSON also works, and you can pass any path instead of a name.

```yaml
description: Alice has one shipped order and a basket.
extends: default                     # optional; a name, a path, or a list. Applied before this file.
postgres:
  orderingdb:                        # catalogdb | identitydb | orderingdb | webhooksdb
    - sql: orders.sql                # a raw SQL file, relative to this file
    - table: ordering.buyers         # or "Table" for the public schema
      rows:                          # inline rows; or rowsFile: buyers.json|yaml
        - { Id: 1, IdentityGuid: a11ce000-0000-4000-8000-000000000001, Name: Alice }
redis:
  - key: /basket/a11ce000-0000-4000-8000-000000000001
    json: { BuyerId: a11ce000-0000-4000-8000-000000000001, Items: [] }   # or value: "raw"; ttl: seconds
```

Rows are inserted with `json_populate_recordset`, so column names must match the database exactly (EF uses PascalCase). Columns you leave out get their database defaults. Use explicit IDs; sequences are fixed up afterwards. YAML plain scalars that look like JSON numbers become numbers; quote values like `"98052"` to keep them as strings. Unknown keys, databases or files cause exit code 3.

### Included scenarios

| Scenario | Contents |
| --- | --- |
| `_base` | Lookup data, always loaded first: `ordering.cardtypes` (1 Amex, 2 Visa, 3 MasterCard). |
| `empty` | Base only. |
| `default` | What the built-in seeders used to produce: 101 catalog items, 13 brands, 8 types, plus users `alice` and `bob` (password `Pass123$`). |
| `order-history` | `default` plus alice as a buyer, with a Visa payment method, a Shipped order, a Cancelled order and two basket items. |
| `catalog-edge` | Its own 14-item "Edge" catalog over 2 pages: a zero-stock item, a 50-character name, prices $0.01 and $9,999.99, an empty description, and a brand and a type with no items. Plus alice and bob. |
| `catalog-pictures` | `catalog-edge` plus item 15 with no picture file, item 16 whose picture file does not exist, and item 17 "Edge 17 100% Wool_Sock" (SQL `LIKE` wildcards in the name). Used by `e2e/teststack/catalog-api.spec.ts`. |
| `order-lifecycle` | `default` plus bob with a Visa and six orders, one per `OrderStatus`. The past-dated Submitted order is advanced to Paid by the background services within about 30 s of loading. alice has no orders. |
| `returning-customers` | `default` plus carol (`c0001000-0000-4000-8000-000000000003`, password `Carol#2026`) with a Visa, a Seattle address and a 3-item basket, and a basket for bob whose stored price is stale (the cart shows the catalog price). |
| `auth-lockout` | `default` plus lockout users (password `Pass123$`): dave locked until 2099, erin one failure from lockout, gina with an expired lockout, frank with lockout disabled. |
| `basket-edge` | `default` plus baskets: bob with 2 lines, quinn with 120 units (badge shows 99+), oscar with a line for a product no longer in the catalog. |
| `ordering-two-buyers` | `default` plus alice with orders 1-6 (statuses the pipeline leaves alone) and a basket, and bob with a Paid order 7 and a Shipped order 8. |
| `ordering-stock` | `default` with low stock plus four past-dated Submitted orders for bob; within about 30 s two reach Paid and two are cancelled by the stock check. |
| `webhooks-subscriptions` | `default` plus webhook subscriptions for alice (OrderPaid, OrderShipped) and bob (CatalogItemPriceChange), and an alice order that reaches Paid shortly after loading. |

Fixed user IDs:

| User | ID |
| --- | --- |
| alice | `a11ce000-0000-4000-8000-000000000001` |
| bob | `b0b00000-0000-4000-8000-000000000002` |

The flows these scenarios exercise, with their positive and negative cases, known app bugs and covering tests, are mapped in `testing/flows/` ([catalog](flows/catalog.md), [identity and basket](flows/identity-basket.md), [ordering and webhooks](flows/ordering.md)). Tests that assert correct behavior the app does not have yet are marked `test.fixme` and listed in those files.

Scenario specs live in `e2e/teststack/`. The spec loads its scenario in `beforeAll` with `loadScenario()` from `e2e/teststack/scenario.ts`, and runs in the `scenarios` project of `playwright.teststack.config.ts`, after the standard suite.

The user ID is also the Redis basket key suffix (`/basket/<id>`) and `ordering.buyers.IdentityGuid`. Passwords are stored pre-hashed; generate new hashes with `testing/teststack hash-password '<pw>'`. Basket JSON matches `CustomerBasket` (`BuyerId`, `Items[]` with `Id`, `ProductId`, `ProductName`, `UnitPrice`, `OldUnitPrice`, `Quantity`, `PictureUrl`).

AI features (embeddings, chat) are off in this stack, so `Catalog.Embedding` stays null.

## Ports and URLs (HTTP only)

| Service | Port |
| --- | --- |
| WebApp | 18080 |
| Identity | 18081 |
| Catalog | 18082 |
| Basket (gRPC) | 18083 |
| Ordering | 18084 |
| Webhooks API | 18085 |
| Webhooks client | 18086 |
| Mobile BFF | 18087 |
| Dashboard | 18888 |
| Postgres | 18432 |
| Redis | 18379 |
| RabbitMQ | 18672 |

The test credentials are in `testing/teststack` (`postgres` / `teststack-postgres`, and so on).

Identity is published as `http://host.docker.internal:18081`, so the browser and the containers use the same issuer URL:

- `playwright.teststack.config.ts` maps that host to 127.0.0.1 for you.
- To log in from a normal browser, add `127.0.0.1 host.docker.internal` to `/etc/hosts`. Docker Desktop usually adds this already.

## Agent workflow

```bash
testing/teststack up --scenario order-history || exit $?   # first run builds the images; later runs reuse them
# ...run tests / probe http://localhost:18080 ...
testing/teststack load default                             # fast reset between tests
testing/teststack status                                   # JSON readiness report when something looks wrong
testing/teststack logs ordering-api
```

To write a new scenario:

1. Copy one from `testing/scenarios/`.
2. Run `testing/teststack validate <path>`.
3. Run `testing/teststack load <path>`.
