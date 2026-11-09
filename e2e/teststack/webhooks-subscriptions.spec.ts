import { randomUUID } from 'crypto';
import { test, expect, APIRequestContext, Page } from '@playwright/test';
import { loadScenario } from './scenario';
import {
  ALICE_ID, BOB_ID, V1, WEBHOOKS_CLIENT_URL, WEBHOOKS_URL, bearer, changeStatus, getOrder, tokenFor,
} from './ordering-helpers';

// testing/scenarios/webhooks-subscriptions: alice owns subscriptions 1 (OrderPaid) and 2 (OrderShipped), bob owns
// 3 (CatalogItemPriceChange). alice's order 1 is Paid; her order 2 reaches Paid 15-50 s after loading.
// The WebhooksClient keeps received messages in memory across scenario loads, so every delivery test
// registers its own subscription with a unique token.
const CLIENT_IN_STACK = 'http://webhooksclient:8080';
const run = randomUUID().slice(0, 8);
const paidToken = `paid-${run}`;

let alice: string;
let bob: string;
let aliceOrdering: string;

test.describe.configure({ mode: 'serial' });
test.setTimeout(150_000);

type Subscription = { id: number; type: number; destUrl: string; token: string; userId: string };

function subscribe(request: APIRequestContext, token: string, body: Record<string, string>) {
  return request.post(`${WEBHOOKS_URL}?${V1}`, { headers: bearer(token), data: body });
}

const hook = (event: string, token: string) => ({
  url: `${CLIENT_IN_STACK}/webhook-received`,
  grantUrl: `${CLIENT_IN_STACK}/check`,
  event,
  token,
});

async function listSubscriptions(request: APIRequestContext, token: string): Promise<Subscription[]> {
  const response = await request.get(`${WEBHOOKS_URL}?${V1}`, { headers: bearer(token) });
  expect(response.status()).toBe(200);
  return response.json();
}

async function loginToClient(page: Page, user: string) {
  await page.goto(WEBHOOKS_CLIENT_URL);
  // The button only works once the interactive circuit is up.
  await expect(async () => {
    await page.getByRole('button', { name: 'Log in' }).click();
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible({ timeout: 2_000 });
  }).toPass();
  await page.getByPlaceholder('Username').fill(user);
  await page.getByPlaceholder('Password').fill('Pass123$');
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page.getByRole('button', { name: 'Log out' })).toBeVisible();
}

/** The registrations grid: the first table, shown only to signed-in users above the received messages. */
function registrations(page: Page) {
  return page.locator('table').first();
}

/** Rows of the "Webhook messages received" grid. Viewed anonymously so the registrations grid is not shown. */
async function receivedRow(page: Page, token: string) {
  await page.goto(WEBHOOKS_CLIENT_URL);
  await expect(page.getByText('Log in to view or edit webhook registrations')).toBeVisible();
  return page.locator('tr', { hasText: token });
}

test.beforeAll(async ({ browser }) => {
  loadScenario('webhooks-subscriptions');
  alice = await tokenFor(browser, 'alice', 'webhooks');
  // Register before order 2 reaches Paid (no earlier than 15 s after loading).
  const context = await browser.newContext();
  expect((await subscribe(context.request, alice, hook('OrderPaid', paidToken))).status()).toBe(201);
  await context.close();
  bob = await tokenFor(browser, 'bob', 'webhooks');
  aliceOrdering = await tokenFor(browser, 'alice', 'ordering');
});

test.describe('Webhooks API', () => {
  test('anonymous and invalid-token requests are rejected with 401', async ({ request }) => {
    const responses = await Promise.all([
      request.get(`${WEBHOOKS_URL}?${V1}`),
      request.get(`${WEBHOOKS_URL}/1?${V1}`),
      request.post(`${WEBHOOKS_URL}?${V1}`, { data: hook('OrderPaid', 'x') }),
      request.delete(`${WEBHOOKS_URL}/1?${V1}`),
      request.get(`${WEBHOOKS_URL}?${V1}`, { headers: { Authorization: 'Bearer nope' } }),
    ]);
    for (const response of responses) {
      expect(response.status(), response.url()).toBe(401);
    }
    expect((await request.get(WEBHOOKS_URL, { headers: bearer(alice) })).status()).toBe(400); // no api-version
  });

  test('each user lists and reads only their own subscriptions', async ({ request }) => {
    const mine = await listSubscriptions(request, alice);
    expect(mine.every((s) => s.userId === ALICE_ID)).toBe(true);
    expect(mine.map((s) => s.token)).toEqual(expect.arrayContaining(['alice-paid-token', 'alice-shipped-token', paidToken]));
    expect(mine.map((s) => s.token)).not.toContain('bob-price-token');

    expect(await listSubscriptions(request, bob)).toEqual([
      expect.objectContaining({ id: 3, type: 1, token: 'bob-price-token', userId: BOB_ID }),
    ]);

    const own = await request.get(`${WEBHOOKS_URL}/1?${V1}`, { headers: bearer(alice) });
    expect(await own.json()).toMatchObject({ id: 1, type: 3, destUrl: `${CLIENT_IN_STACK}/webhook-received`, token: 'alice-paid-token' });
    const other = await request.get(`${WEBHOOKS_URL}/3?${V1}`, { headers: bearer(alice) });
    expect(other.status()).toBe(404);
    expect((await request.get(`${WEBHOOKS_URL}/999?${V1}`, { headers: bearer(alice) })).status()).toBe(404);
  });

  test('registration validates the URLs, the event name and the grant URL', async ({ request }) => {
    const before = (await listSubscriptions(request, alice)).length;

    const invalid = await subscribe(request, alice, { url: 'not a url', grantUrl: 'nope', event: 'OrderLost', token: 't' });
    expect(invalid.status()).toBe(400);
    expect((await invalid.json()).errors).toEqual({
      GrantUrl: ['GrantUrl is not valid'],
      Url: ['Url is not valid'],
      Event: ['OrderLost is invalid event name'],
    });

    // The grant URL must share the hook URL's origin...
    const crossOrigin = await subscribe(request, alice, { ...hook('OrderPaid', 't'), grantUrl: 'http://webhooks-api:8080/check' });
    expect(crossOrigin.status()).toBe(400);
    expect(await crossOrigin.json()).toBe('Invalid grant URL: http://webhooks-api:8080/check');

    // ...and must answer the OPTIONS grant check.
    const unreachable = await subscribe(request, alice, {
      url: 'http://no-such-host:8080/webhook-received',
      grantUrl: 'http://no-such-host:8080/check',
      event: 'OrderPaid',
      token: 't',
    });
    expect(unreachable.status()).toBe(400);
    const noGrantEndpoint = await subscribe(request, alice, { ...hook('OrderPaid', 't'), grantUrl: `${CLIENT_IN_STACK}/not-a-grant-endpoint` });
    expect(noGrantEndpoint.status()).toBe(400);

    expect(await listSubscriptions(request, alice)).toHaveLength(before);
  });

  test('event names outside WebhookType are rejected, including numeric ones', async ({ request }) => {
    // BUG: WebhookSubscriptionRequest.Validate uses Enum.TryParse, which accepts any integer string, so
    // event "99" is stored as WebhookType 99 (currently 201 Created) and can never fire.
    test.fixme(true, 'numeric event names are accepted');
    const response = await subscribe(request, alice, hook('99', 'numeric'));
    expect(response.status()).toBe(400);
  });

  test('a subscription can be deleted by its owner only', async ({ request }) => {
    const token = `delete-${run}`;
    const created = await subscribe(request, alice, hook('OrderShipped', token));
    expect(created.status()).toBe(201);
    const location = created.headers()['location'];
    expect(location).toMatch(/^\/api\/webhooks\/\d+$/);
    const id = Number(location.split('/').pop());

    // bob cannot delete it, and alice cannot delete bob's.
    expect((await request.delete(`${WEBHOOKS_URL}/${id}?${V1}`, { headers: bearer(bob) })).status()).toBe(404);
    expect((await request.delete(`${WEBHOOKS_URL}/3?${V1}`, { headers: bearer(alice) })).status()).toBe(404);
    expect((await listSubscriptions(request, bob)).map((s) => s.id)).toEqual([3]);

    expect((await request.delete(`${WEBHOOKS_URL}/${id}?${V1}`, { headers: bearer(alice) })).status()).toBe(202);
    expect((await listSubscriptions(request, alice)).map((s) => s.token)).not.toContain(token);
    // Deleting again is a 404.
    expect((await request.delete(`${WEBHOOKS_URL}/${id}?${V1}`, { headers: bearer(alice) })).status()).toBe(404);
  });
});

test.describe('WebhooksClient', () => {
  test('an anonymous visitor is asked to log in and sees no registrations', async ({ page }) => {
    await page.goto(WEBHOOKS_CLIENT_URL);
    await expect(page.getByRole('heading', { name: 'Registered webhooks' })).toBeVisible();
    await expect(page.getByText('Log in to view or edit webhook registrations')).toBeVisible();
    await expect(page.getByRole('link', { name: 'Add webhook registration' })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Webhook messages received (orders paid)' })).toBeVisible();
  });

  test('alice logs in and sees only her registrations', async ({ page }) => {
    await loginToClient(page, 'alice');
    await expect(page.getByText('alice', { exact: true })).toBeVisible();
    const grid = registrations(page);
    await expect(grid).toContainText('alice-paid-token');
    await expect(grid).toContainText('alice-shipped-token');
    await expect(grid).toContainText(paidToken);
    await expect(grid).not.toContainText('bob-price-token');

    await page.getByRole('button', { name: 'Log out' }).click();
    await expect(page.getByRole('button', { name: 'Log in' })).toBeVisible();
    await expect(page.getByText('Log in to view or edit webhook registrations')).toBeVisible();
  });

  test('bob sees his own registration and none of alice\'s', async ({ page }) => {
    await loginToClient(page, 'bob');
    const grid = registrations(page);
    await expect(grid).toContainText('bob-price-token');
    await expect(grid).not.toContainText('alice-paid-token');
  });

  test('registering without a token does nothing', async ({ page, request }) => {
    const before = (await listSubscriptions(request, alice)).length;
    await loginToClient(page, 'alice');
    await page.getByRole('link', { name: 'Add webhook registration' }).click();
    await expect(page.getByRole('heading', { name: 'Register a new webhook' })).toBeVisible();
    // Pre-filled with WebhookClientOptions:Token from appsettings.json.
    await expect(page.getByPlaceholder('Token')).toHaveValue('6168DB8D-DC58-4094-AF24-483278923590');
    await page.getByPlaceholder('Token').fill('');
    await page.getByRole('button', { name: 'Register' }).click();
    await expect(page).toHaveURL(/\/add-webhook$/);
    await expect(page.locator('.error-message')).toHaveCount(0);
    expect(await listSubscriptions(request, alice)).toHaveLength(before);
  });

  test('registering a webhook from the client UI succeeds', async ({ page, request }) => {
    const token = `ui-${run}`;
    await loginToClient(page, 'alice');
    await page.getByRole('link', { name: 'Add webhook registration' }).click();
    await page.getByPlaceholder('Token').fill(token);
    await page.getByRole('button', { name: 'Register' }).click();
    await expect(page.getByRole('heading', { name: 'Registered webhooks' })).toBeVisible({ timeout: 15_000 });
    await expect(registrations(page)).toContainText(token);
    expect((await listSubscriptions(request, alice)).map((s) => s.token)).toContain(token);
  });
});

test.describe('delivery', () => {
  test('shipping an order delivers an OrderShipped webhook to the client', async ({ page, request }) => {
    const token = `shipped-${run}`;
    expect((await subscribe(request, alice, hook('OrderShipped', token))).status()).toBe(201);

    expect((await changeStatus(request, aliceOrdering, 'ship', 1)).status()).toBe(200);
    expect((await getOrder(request, aliceOrdering, 1)).status).toBe('Shipped');

    await expect(async () => {
      const row = await receivedRow(page, token);
      await expect(row).toHaveCount(1, { timeout: 1_000 });
      await expect(row).toContainText('"OrderId":1');
    }).toPass({ timeout: 30_000 });
  });

  test('an order reaching Paid delivers an OrderPaid webhook to the client', async ({ page, request }) => {
    await expect
      .poll(async () => (await getOrder(request, aliceOrdering, 2)).status, { timeout: 120_000, intervals: [3_000] })
      .toBe('Paid');

    await expect(async () => {
      const row = await receivedRow(page, paidToken);
      await expect(row).toHaveCount(1, { timeout: 1_000 });
      await expect(row).toContainText('"OrderId":2');
    }).toPass({ timeout: 30_000 });
  });

  test('subscriptions of other event types receive nothing', async ({ page }) => {
    // bob's CatalogItemPriceChange subscription never fires for order events.
    const row = await receivedRow(page, 'bob-price-token');
    await expect(row).toHaveCount(0);
  });
});
