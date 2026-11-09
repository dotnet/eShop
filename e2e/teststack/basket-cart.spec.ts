import { execFileSync } from 'child_process';
import { test, expect, Browser, Page } from '@playwright/test';
import { loadScenario, login } from './scenario';
import { redisBasket, USER_IDS } from './auth-basket-helpers';

// Scenario basket-edge: alice has no basket; bob has Explorer 45L Backpack x2 + Carbon Fiber Trekking Poles x1;
// quinn has 120 trekking poles; oscar's basket also holds product 99999, which is not in the catalog.
const BACKPACK = { id: 7, name: 'Explorer 45L Backpack', price: 149.99 };
const POLES = { id: 6, name: 'Carbon Fiber Trekking Poles', price: 69.99 };
const BOOTS = { id: 1, name: 'Wanderer Black Hiking Boots', price: 109.99 };

test.describe.configure({ mode: 'serial' });

function deleteBasket(userId: string) {
  execFileSync('docker', ['exec', 'eshop-teststack-redis-1', 'redis-cli', '-a', 'teststack-redis', '--no-auth-warning', 'DEL', `/basket/${userId}`]);
}

function redisQuantities(userId: string): Record<number, number> {
  return Object.fromEntries((redisBasket(userId)?.Items ?? []).map(i => [i.ProductId, i.Quantity]));
}

async function userPage(browser: Browser, user: string): Promise<Page> {
  const page = await (await browser.newContext()).newPage();
  await login(page, user);
  return page;
}

async function addFromItemPage(page: Page, item: { id: number; name: string }, expectedInCart: number) {
  await page.goto(`/item/${item.id}`);
  await expect(page.getByRole('heading', { name: item.name })).toBeVisible();
  await page.getByRole('button', { name: 'Add to shopping bag' }).click();
  await expect(page.locator('.in-cart strong')).toHaveText(String(expectedInCart));
}

async function openCart(page: Page) {
  await page.goto('/cart');
  await expect(page.getByRole('heading', { name: 'Shopping bag', exact: true })).toBeVisible();
}

function line(page: Page, name: string) {
  return page.locator('.cart-item').filter({ has: page.getByText(name, { exact: true }) });
}

async function submitAndWait(page: Page, action: () => Promise<unknown>) {
  const posted = page.waitForResponse(r => r.request().method() === 'POST' && new URL(r.url()).pathname === '/cart');
  await Promise.all([posted, action()]);
}

const money = (n: number) => `$${n.toFixed(2)}`;

test.describe('basket-edge scenario: cart UI', () => {
  test.beforeAll(() => {
    loadScenario('basket-edge');
  });

  test('a user without a basket sees the empty-bag view and a header bag without a count', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const page = await userPage(browser, 'alice');
    await expect(page.locator('a.icon-button[aria-label="Shopping bag"]')).toBeVisible();
    await expect(page.locator('.cart-button-count')).toHaveCount(0);

    await openCart(page);
    await expect(page.getByRole('heading', { name: 'Your shopping bag is empty' })).toBeVisible();
    await expect(page.locator('.cart-item')).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Check out' })).toHaveCount(0);
    await page.getByRole('link', { name: 'Start shopping' }).click();
    await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
    await page.context().close();
  });

  test('adding an item from its page stores it in Redis and updates the header count', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const page = await userPage(browser, 'alice');
    await addFromItemPage(page, BACKPACK, 1);
    await expect(page.getByRole('link', { name: 'Shopping bag, 1 item' })).toBeVisible();
    await expect(page.locator('.cart-button-count')).toHaveText('1');
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [BACKPACK.id]: 1 });

    await openCart(page);
    await expect(page.locator('.cart-item')).toHaveCount(1);
    await expect(line(page, BACKPACK.name).locator('.price')).toHaveText(money(BACKPACK.price));
    await expect(line(page, BACKPACK.name).locator('[data-cart-quantity-input]')).toHaveValue('1');
    await page.context().close();
  });

  test('adding the same item twice merges into one line with quantity 2', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const page = await userPage(browser, 'alice');
    await addFromItemPage(page, BACKPACK, 1);
    await page.getByRole('button', { name: 'Add to shopping bag' }).click();
    await expect(page.locator('.in-cart strong')).toHaveText('2');
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [BACKPACK.id]: 2 });

    await addFromItemPage(page, POLES, 1);
    await expect(page.getByRole('link', { name: 'Shopping bag, 3 items' })).toBeVisible();

    await openCart(page);
    await expect(page.locator('.cart-item')).toHaveCount(2);
    await expect(line(page, BACKPACK.name).locator('[data-cart-quantity-input]')).toHaveValue('2');
    await expect(line(page, BACKPACK.name).locator('.catalog-item-total')).toHaveText(money(2 * BACKPACK.price));
    await expect(page.locator('.cart-summary .filter-badge')).toHaveText('3');
    await expect(page.locator('.cart-summary-total')).toContainText(money(2 * BACKPACK.price + POLES.price));
    await page.context().close();
  });

  test('quantity can be increased, decreased and typed; totals and Redis follow', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const page = await userPage(browser, 'alice');
    await addFromItemPage(page, BOOTS, 1);
    await openCart(page);
    const row = line(page, BOOTS.name);
    const qty = row.locator('[data-cart-quantity-input]');

    await expect(row.getByRole('button', { name: `Decrease quantity of ${BOOTS.name}` })).toBeDisabled();
    await submitAndWait(page, () => row.getByRole('button', { name: `Increase quantity of ${BOOTS.name}` }).click());
    await expect(qty).toHaveValue('2');
    await expect(row.locator('.catalog-item-total')).toHaveText(money(2 * BOOTS.price));

    await submitAndWait(page, async () => { await qty.fill('5'); await qty.press('Enter'); });
    await expect(qty).toHaveValue('5');
    await expect(page.locator('.cart-summary-total')).toContainText(money(5 * BOOTS.price));
    await expect(page.locator('.cart-button-count')).toHaveText('5');
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [BOOTS.id]: 5 });

    // Reload so the typed input loses focus without firing a second change-commit (see the fixme below).
    await openCart(page);
    await expect(qty).toHaveValue('5');
    await submitAndWait(page, () => row.getByRole('button', { name: `Decrease quantity of ${BOOTS.name}` }).click());
    await expect(qty).toHaveValue('4');
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [BOOTS.id]: 4 });
    await page.context().close();
  });

  // Bug: cart-quantity.js commits a typed value on Enter but leaves the input focused with its
  // pre-focus value, so the next click elsewhere (e.g. "Decrease") blurs it, fires `change` and
  // re-submits the typed quantity in parallel with the decrement. Which POST lands last is a race,
  // so Redis intermittently keeps 5 while the UI shows 4.
  test.fixme('typing a quantity, pressing Enter, then clicking Decrease reliably stores the decremented quantity', async ({ browser }) => {
    for (let attempt = 0; attempt < 5; attempt++) {
      deleteBasket(USER_IDS.alice);
      const page = await userPage(browser, 'alice');
      await addFromItemPage(page, BOOTS, 1);
      await openCart(page);
      const row = line(page, BOOTS.name);
      const qty = row.locator('[data-cart-quantity-input]');
      await submitAndWait(page, async () => { await qty.fill('5'); await qty.press('Enter'); });
      await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [BOOTS.id]: 5 });
      await row.getByRole('button', { name: `Decrease quantity of ${BOOTS.name}` }).click();
      await expect(qty).toHaveValue('4');
      await page.waitForTimeout(1000);
      expect(redisQuantities(USER_IDS.alice)).toEqual({ [BOOTS.id]: 4 });
      await page.context().close();
    }
  });

  test('typing 0 or a negative quantity is clamped to 1 instead of removing the line', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const page = await userPage(browser, 'alice');
    await addFromItemPage(page, BOOTS, 1);
    await page.getByRole('button', { name: 'Add to shopping bag' }).click();
    await expect(page.locator('.in-cart strong')).toHaveText('2');
    await openCart(page);
    const qty = line(page, BOOTS.name).locator('[data-cart-quantity-input]');

    await submitAndWait(page, async () => { await qty.fill('0'); await qty.press('Enter'); });
    await expect(qty).toHaveValue('1');
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [BOOTS.id]: 1 });

    await submitAndWait(page, async () => { await qty.fill('-4'); await qty.press('Enter'); });
    await expect(qty).toHaveValue('1');
    await expect(page.locator('.cart-item')).toHaveCount(1);
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [BOOTS.id]: 1 });
    await page.context().close();
  });

  test('removing lines updates the bag; removing the last one shows the empty view', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const page = await userPage(browser, 'alice');
    await addFromItemPage(page, BOOTS, 1);
    await addFromItemPage(page, POLES, 1);
    await openCart(page);
    await expect(page.locator('.cart-item')).toHaveCount(2);

    await submitAndWait(page, () => page.getByRole('button', { name: `Remove ${BOOTS.name} from your bag` }).click());
    await expect(page.locator('.cart-item')).toHaveCount(1);
    await expect(line(page, POLES.name)).toBeVisible();
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [POLES.id]: 1 });

    await submitAndWait(page, () => page.getByRole('button', { name: `Remove ${POLES.name} from your bag` }).click());
    await expect(page.getByRole('heading', { name: 'Your shopping bag is empty' })).toBeVisible();
    await expect(page.locator('.cart-button-count')).toHaveCount(0);
    await expect.poll(() => redisBasket(USER_IDS.alice)?.Items ?? []).toEqual([]);
    await page.context().close();
  });

  test('a seeded basket is shown with catalog prices and the header count', async ({ browser }) => {
    const page = await userPage(browser, 'bob');
    await expect(page.getByRole('link', { name: 'Shopping bag, 3 items' })).toBeVisible();
    await openCart(page);
    await expect(page.locator('.cart-item')).toHaveCount(2);
    await expect(line(page, BACKPACK.name).locator('[data-cart-quantity-input]')).toHaveValue('2');
    await expect(line(page, POLES.name).locator('[data-cart-quantity-input]')).toHaveValue('1');
    await expect(page.locator('.cart-summary-total')).toContainText(money(2 * BACKPACK.price + POLES.price));
    await page.context().close();
  });

  test('the header count caps at 99+ while the label keeps the real total', async ({ browser }) => {
    const page = await userPage(browser, 'quinn');
    await expect(page.locator('.cart-button-count')).toHaveText('99+');
    await expect(page.getByRole('link', { name: 'Shopping bag, 120 items' })).toBeVisible();
    await openCart(page);
    await expect(page.locator('.cart-summary .filter-badge')).toHaveText('120');
    await expect(page.locator('.cart-summary-total')).toContainText('$8,398.80');
    await page.context().close();
  });

  test('the basket persists across log out and log in', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const first = await userPage(browser, 'alice');
    await addFromItemPage(first, POLES, 1);
    await first.getByRole('button', { name: 'Account' }).hover();
    await first.getByRole('button', { name: 'Log out' }).click();
    await expect(first.getByLabel('Sign in')).toBeVisible();
    await expect(first.locator('.cart-button-count')).toHaveCount(0);
    await first.context().close();
    await expect.poll(() => redisQuantities(USER_IDS.alice)).toEqual({ [POLES.id]: 1 });

    const second = await userPage(browser, 'alice');
    await expect(second.locator('.cart-button-count')).toHaveText('1');
    await openCart(second);
    await expect(line(second, POLES.name).locator('[data-cart-quantity-input]')).toHaveValue('1');
    await second.context().close();
  });

  test('two sessions of the same user share one basket', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const a = await userPage(browser, 'alice');
    const b = await userPage(browser, 'alice');
    await addFromItemPage(a, BOOTS, 1);
    await openCart(b);
    await expect(line(b, BOOTS.name).locator('[data-cart-quantity-input]')).toHaveValue('1');
    await a.context().close();
    await b.context().close();
  });

  test('baskets are isolated between users', async ({ browser }) => {
    deleteBasket(USER_IDS.alice);
    const bobBefore = redisBasket(USER_IDS.bob);
    const alice = await userPage(browser, 'alice');
    await addFromItemPage(alice, BOOTS, 1);
    await openCart(alice);
    await expect(alice.locator('.cart-item')).toHaveCount(1);
    await expect(line(alice, BACKPACK.name)).toHaveCount(0);

    const bob = await userPage(browser, 'bob');
    await openCart(bob);
    await expect(bob.locator('.cart-item')).toHaveCount(2);
    await expect(line(bob, BOOTS.name)).toHaveCount(0);
    expect(redisBasket(USER_IDS.bob)).toEqual(bobBefore);
    await alice.context().close();
    await bob.context().close();
  });

  test('anonymous visitors cannot add to the bag and no basket is written', async ({ browser }) => {
    const before = execFileSync('docker', ['exec', 'eshop-teststack-redis-1', 'redis-cli', '-a', 'teststack-redis', '--no-auth-warning', '--scan', '--pattern', '/basket/*'], { encoding: 'utf8' });
    const page = await (await browser.newContext()).newPage();
    await page.goto(`/item/${BACKPACK.id}`);
    await expect(page.getByRole('button', { name: 'Add to shopping bag' })).toHaveCount(0);
    await page.getByRole('button', { name: 'Log in to purchase' }).click();
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
    const after = execFileSync('docker', ['exec', 'eshop-teststack-redis-1', 'redis-cli', '-a', 'teststack-redis', '--no-auth-warning', '--scan', '--pattern', '/basket/*'], { encoding: 'utf8' });
    expect(after.split('\n').sort()).toEqual(before.split('\n').sort());
    await page.context().close();
  });

  // BUG: BasketState.FetchBasketItemsAsync does catalogItems[item.ProductId] for every basket line, so a line
  // whose product was removed from the catalog throws KeyNotFoundException ("The given key '99999' was not
  // present in the dictionary") in the WebApp. CartMenu renders on every page, so after signing in oscar can't
  // load any page at all. The cart should skip (or flag) the missing product and still show the valid line.
  test.fixme('a basket line for a product no longer in the catalog does not break the site', async ({ browser }) => {
    const page = await userPage(browser, 'oscar');
    await openCart(page);
    await expect(line(page, BACKPACK.name)).toBeVisible();
    await expect(page.getByText('Discontinued Widget')).toHaveCount(0);
    await page.context().close();
  });
});
