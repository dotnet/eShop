import { Browser, Page } from '@playwright/test';
import { test, expect } from '../fixtures';
import { loadScenario, login } from './scenario';
import { accessToken, getOrder } from './ordering-helpers';

// testing/scenarios/ordering-two-buyers: alice has orders 1-6 and a basket of 2 x Summit Pro Harness (89.99)
// + 1 x Expedition Backpack (129.99) = $309.97; bob has orders 7-8 and an empty basket.
const ALICE_BASKET_TOTAL = '$309.97';

test.describe.configure({ mode: 'serial' });

test.beforeAll(() => {
  loadScenario('ordering-two-buyers');
});

async function userPage(browser: Browser, user: string): Promise<Page> {
  const page = await (await browser.newContext()).newPage();
  await login(page, user);
  return page;
}

async function orderNumbers(page: Page): Promise<number[]> {
  await page.goto('/user/orders');
  await expect(page.getByRole('heading', { name: 'Orders', exact: true })).toBeVisible();
  await expect(page.locator('.orders-loading')).toHaveCount(0);
  return (await page.locator('.order-number').allTextContents()).map(Number).sort((a, b) => a - b);
}

async function openCheckout(page: Page) {
  await page.goto('/checkout');
  await expect(page.getByRole('heading', { name: 'Shipping address' })).toBeVisible();
}

test('checkout requires sign-in', async ({ page }) => {
  await page.goto('/checkout');
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  await expect(page).toHaveURL(/host\.docker\.internal:18081/);
});

test('the checkout form is pre-filled from the profile address', async ({ browser }) => {
  const page = await userPage(browser, 'alice');
  await page.goto('/cart');
  await page.getByRole('link', { name: 'Check out' }).click();
  await expect(page.getByRole('heading', { name: 'Shipping address' })).toBeVisible();
  await expect(page.getByLabel('Address')).toHaveValue('15703 NE 61st Ct');
  await expect(page.getByLabel('City')).toHaveValue('Redmond');
  await expect(page.getByLabel('State')).toHaveValue('WA');
  await expect(page.getByLabel('Zip code')).toHaveValue('98052');
  await expect(page.getByLabel('Country')).toHaveValue('U.S.');
  await page.getByRole('link', { name: 'Back to the shopping bag' }).click();
  await expect(page).toHaveURL(/\/cart$/);
  await page.context().close();
});

test('missing required address fields block the order', async ({ browser }) => {
  const page = await userPage(browser, 'alice');
  await openCheckout(page);
  await page.getByLabel('Address').fill('');
  await page.getByLabel('City').fill('');
  await page.getByLabel('Zip code').fill('');
  await page.getByRole('button', { name: 'Place order' }).click();

  await expect(page.locator('div.validation-message', { hasText: 'The Street field is required.' })).toBeVisible();
  await expect(page.locator('div.validation-message', { hasText: 'The City field is required.' })).toBeVisible();
  await expect(page.locator('div.validation-message', { hasText: 'The ZipCode field is required.' })).toBeVisible();
  await expect(page.locator('div.validation-message', { hasText: 'State' })).toHaveCount(0);
  await expect(page.locator('.validation-errors li')).toHaveCount(3);
  await expect(page).toHaveURL(/\/checkout$/);

  // No order was placed and the basket is untouched.
  expect(await orderNumbers(page)).toEqual([1, 2, 3, 4, 5, 6]);
  await page.goto('/cart');
  await expect(page.locator('.cart-item')).toHaveCount(2);
  await page.context().close();
});

test('checking out an empty cart shows "Your cart is empty" and places no order', async ({ browser }) => {
  const page = await userPage(browser, 'bob');
  await openCheckout(page);
  await page.getByRole('button', { name: 'Place order' }).click();
  await expect(page.getByText('Your cart is empty')).toBeVisible();
  await expect(page).toHaveURL(/\/checkout$/);
  expect(await orderNumbers(page)).toEqual([7, 8]);
  await page.context().close();
});

test('bob\'s order list never shows alice\'s orders', async ({ browser }) => {
  const page = await userPage(browser, 'bob');
  expect(await orderNumbers(page)).toEqual([7, 8]);
  const row7 = page.locator('.orders-item').filter({ has: page.getByText('7', { exact: true }) });
  await expect(row7.locator('.order-status .status')).toHaveText('Paid');
  await expect(row7.locator('.order-total')).toHaveText('$599.97');
  await page.context().close();
});

test('alice places an order to a new address; it is Submitted, her cart is emptied and bob cannot see it', async ({ browser }) => {
  const page = await userPage(browser, 'alice');
  await openCheckout(page);
  await page.getByLabel('Address').fill('500 Pine St');
  await page.getByLabel('City').fill('Seattle');
  await page.getByLabel('State').fill('WA');
  await page.getByLabel('Zip code').fill('98101');
  await page.getByLabel('Country').fill('USA');
  await page.getByRole('button', { name: 'Place order' }).click();

  await expect(page).toHaveURL(/\/user\/orders$/);
  await expect(page.locator('.order-number')).toHaveCount(7);
  const numbers = (await page.locator('.order-number').allTextContents()).map(Number);
  const newOrder = Math.max(...numbers);
  expect(newOrder).toBeGreaterThan(8);
  const row = page.locator('.orders-item').filter({ has: page.getByText(String(newOrder), { exact: true }) });
  await expect(row.locator('.order-total')).toHaveText(ALICE_BASKET_TOTAL);
  // Checkout orders sit in the 1 minute grace period first.
  await expect(row.locator('.order-status .status')).toHaveText(/^(Submitted|AwaitingValidation)$/);

  // The order carries the address typed at checkout and the basket items.
  const details = await getOrder(page.request, await accessToken(page, 'ordering'), newOrder);
  expect(details).toMatchObject({ street: '500 Pine St', city: 'Seattle', state: 'WA', zipcode: '98101', country: 'USA', total: 309.97 });
  expect(details.orderItems.map((i) => `${i.units} x ${i.productName}`).sort()).toEqual([
    '1 x Expedition Backpack',
    '2 x Summit Pro Harness',
  ]);

  await page.goto('/cart');
  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
  await page.context().close();

  const bob = await userPage(browser, 'bob');
  expect(await orderNumbers(bob)).toEqual([7, 8]);
  await bob.context().close();
});
