import { Page } from '@playwright/test';
import { test, expect } from '../fixtures';
import { loadScenario, login } from './scenario';

// Totals are the sums of UnitPrice * Units of the items in testing/scenarios/order-lifecycle/scenario.yaml.
const orders = [
  { number: 2, status: 'AwaitingValidation', date: [2025, 3, 5, 14, 15], total: '219.97' },
  { number: 3, status: 'StockConfirmed', date: [2025, 3, 10, 16, 45], total: '459.96' },
  { number: 4, status: 'Paid', date: [2025, 3, 20, 11, 30], total: '289.98' },
  { number: 5, status: 'Shipped', date: [2025, 4, 2, 8, 5], total: '479.96' },
  { number: 6, status: 'Cancelled', date: [2025, 4, 15, 19, 20], total: '199.98' },
];
// Past the grace period at load time: OrderProcessor, Catalog and PaymentProcessor advance it to Paid.
const pipelineOrder = { number: 1, status: 'Paid', date: [2025, 3, 1, 9, 0], total: '319.97' };

test.describe.configure({ mode: 'serial' });
test.setTimeout(240_000);

test.beforeAll(() => {
  loadScenario('order-lifecycle');
});

const orderRow = (page: Page, number: number) =>
  page.locator('.orders-item').filter({ has: page.getByText(String(number), { exact: true }) });

// The WebApp formats the (UTC) date with the server culture: "03/05/2025 14:15:00" or "3/5/2025 2:15:00 PM".
function parseDisplayedDate(text: string): number[] {
  const m = text.trim().match(/^(\d+)\/(\d+)\/(\d+),?\s+(\d+):(\d+)(?::\d+)?\s*(AM|PM)?$/i);
  expect(m, `unrecognised date "${text}"`).not.toBeNull();
  let hour = Number(m![4]);
  if (m![6]) {
    hour = (hour % 12) + (m![6].toUpperCase() === 'PM' ? 12 : 0);
  }
  return [Number(m![3]), Number(m![1]), Number(m![2]), hour, Number(m![5])];
}

async function expectOrder(page: Page, o: { number: number; status: string; date: number[]; total: string }) {
  const row = orderRow(page, o.number);
  await expect(row).toHaveCount(1);
  await expect(row.locator('.order-status .status')).toHaveText(o.status);
  await expect(row.locator('.order-total')).toHaveText(`$${o.total}`);
  expect(parseDisplayedDate((await row.locator('.order-date').textContent())!)).toEqual(o.date);
}

test('bob sees his whole order history with correct status, date and total', async ({ page }) => {
  await login(page, 'bob');
  await page.goto('/user/orders');
  await expect(page.getByRole('heading', { name: 'Orders', exact: true })).toBeVisible();
  await expect(page.locator('.order-number')).toHaveCount(6);

  for (const o of orders) {
    await expectOrder(page, o);
  }

  // The Submitted order may already be advancing by the time the page loads.
  const pipelineRow = orderRow(page, pipelineOrder.number);
  await expect(pipelineRow.locator('.order-total')).toHaveText(`$${pipelineOrder.total}`);
  expect(parseDisplayedDate((await pipelineRow.locator('.order-date').textContent())!)).toEqual(pipelineOrder.date);
  await expect(pipelineRow.locator('.order-status .status')).toHaveText(
    /^(Submitted|AwaitingValidation|StockConfirmed|Paid)$/,
  );
});

test('the seeded Submitted order is advanced to Paid by the background pipeline', async ({ page }) => {
  await login(page, 'bob');

  await expect
    .poll(
      async () => {
        await page.goto('/user/orders');
        await expect(page.getByRole('heading', { name: 'Orders', exact: true })).toBeVisible();
        return orderRow(page, pipelineOrder.number).locator('.order-status .status').textContent();
      },
      { timeout: 150_000, intervals: [5_000] },
    )
    .toBe('Paid');

  await expectOrder(page, pipelineOrder);
  // The orders that were not Submitted are untouched.
  for (const o of orders) {
    await expectOrder(page, o);
  }
});

test('alice has no orders', async ({ page }) => {
  await login(page, 'alice');
  await page.goto('/user/orders');
  await expect(page.getByRole('heading', { name: 'No orders yet' })).toBeVisible();
  await expect(page.locator('.order-number')).toHaveCount(0);
});
