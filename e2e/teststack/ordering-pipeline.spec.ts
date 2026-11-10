import { test, expect, APIRequestContext } from '@playwright/test';
import { loadScenario, login } from './scenario';
import { CATALOG_URL, V1, changeStatus, getOrder, listOrders, tokenFor } from './ordering-helpers';

// testing/scenarios/ordering-stock: four past-dated Submitted orders for bob. Item 11 has 2 units in stock,
// item 12 has none, everything else has 100.
const expected = [
  { orderNumber: 1, status: 'Paid', total: 399.98 },
  { orderNumber: 2, status: 'Cancelled', total: 649.95 },
  { orderNumber: 3, status: 'Cancelled', total: 458.99 },
  { orderNumber: 4, status: 'Paid', total: 259.98 },
];

let bob: string;

test.describe.configure({ mode: 'serial' });
test.setTimeout(240_000);

test.beforeAll(async ({ browser }) => {
  loadScenario('ordering-stock');
  bob = await tokenFor(browser, 'bob', 'ordering');
});

async function stock(request: APIRequestContext, id: number): Promise<number> {
  const response = await request.get(`${CATALOG_URL}/items/${id}?${V1}`);
  expect(response.status()).toBe(200);
  return (await response.json()).availableStock;
}

test('the background pipeline pays in-stock orders and cancels orders without enough stock', async ({ request }) => {
  await expect
    .poll(
      async () => (await listOrders(request, bob))
        .map(({ orderNumber, status, total }) => ({ orderNumber, status, total }))
        .sort((a, b) => a.orderNumber - b.orderNumber),
      { timeout: 180_000, intervals: [3_000] },
    )
    .toEqual(expected);

  // Insufficient stock (5 requested, 2 available).
  expect((await getOrder(request, bob, 2)).description).toBe("The product items don't have stock: (Vertical Journey Climbing Shoes).");
  // Only the out-of-stock line is named; the in-stock helmet does not save the order.
  expect((await getOrder(request, bob, 3)).description).toBe("The product items don't have stock: (Powder Pro Snowboard).");
  // Ordering exactly the units in stock is accepted.
  expect((await getOrder(request, bob, 4)).status).toBe('Paid');
  expect((await getOrder(request, bob, 1)).description).toContain('The payment was performed');
});

test('paid orders decrement catalog stock; cancelled orders leave it untouched', async ({ request }) => {
  await expect.poll(() => stock(request, 9), { timeout: 30_000 }).toBe(98);
  await expect.poll(() => stock(request, 11), { timeout: 30_000 }).toBe(0);
  expect(await stock(request, 10)).toBe(100);
  expect(await stock(request, 12)).toBe(0);
});

test('bob\'s order list shows the final statuses', async ({ page }) => {
  await login(page, 'bob');
  await page.goto('/user/orders');
  await expect(page.locator('.order-number')).toHaveCount(4);
  for (const o of expected) {
    const row = page.locator('.orders-item').filter({ has: page.getByText(String(o.orderNumber), { exact: true }) });
    await expect(row.locator('.order-status .status')).toHaveText(o.status);
    await expect(row.locator('.order-status .status')).toHaveClass(new RegExp(o.status.toLowerCase()));
    await expect(row.locator('.order-total')).toHaveText(`$${o.total.toFixed(2)}`);
  }
});

test('a stock-rejected order cannot be shipped, a pipeline-paid order can', async ({ request }) => {
  expect((await changeStatus(request, bob, 'ship', 2)).ok()).toBe(false);
  expect((await getOrder(request, bob, 2)).status).toBe('Cancelled');

  expect((await changeStatus(request, bob, 'ship', 1)).status()).toBe(200);
  expect((await getOrder(request, bob, 1)).status).toBe('Shipped');
});

test('alice cannot see bob\'s orders', async ({ page }) => {
  await login(page, 'alice');
  await page.goto('/user/orders');
  await expect(page.getByRole('heading', { name: 'No orders yet' })).toBeVisible();
});
