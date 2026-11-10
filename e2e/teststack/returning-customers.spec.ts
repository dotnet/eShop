import { Browser, Page } from '@playwright/test';
import { test, expect } from '../fixtures';
import { loadScenario, login } from './scenario';

const CAROL_PASSWORD = 'Carol#2026';

// Catalog prices (testing/scenarios/default/catalog.sql). Carol: 3 x 69.99 + 1 x 179.99 + 2 x 59.99.
const carolLines = [
  { name: 'Carbon Fiber Trekking Poles', quantity: 3, price: '$69.99', total: '$209.97' },
  { name: 'Frostbite Insulated Jacket', quantity: 1, price: '$179.99', total: '$179.99' },
  { name: 'Trailblazer Bike Helmet', quantity: 2, price: '$59.99', total: '$119.98' },
];
const carolGrandTotal = '$509.94';

test.describe.configure({ mode: 'serial' });

async function userPage(browser: Browser, user: string, password?: string): Promise<Page> {
  const context = await browser.newContext();
  const page = await context.newPage();
  await login(page, user, password);
  return page;
}

async function cartLine(page: Page, name: string) {
  await page.goto('/cart');
  await expect(page.getByRole('heading', { name: 'Shopping bag', exact: true })).toBeVisible();
  return page.locator('.cart-item').filter({ has: page.getByText(name, { exact: true }) });
}

test.describe('returning-customers scenario', () => {
  test.beforeAll(() => {
    loadScenario('returning-customers');
  });

  test('carol logs in with her own password; alice\'s password is rejected', async ({ browser }) => {
    const bad = await browser.newContext();
    const badPage = await bad.newPage();
    await badPage.goto('/');
    await badPage.getByLabel('Sign in').click();
    await expect(badPage.getByRole('heading', { name: 'Login' })).toBeVisible();
    await badPage.getByPlaceholder('Username').fill('carol');
    await badPage.getByPlaceholder('Password').fill('Pass123$');
    await badPage.getByRole('button', { name: 'Login' }).click();
    await expect(badPage.getByText('Invalid username or password')).toBeVisible();
    await expect(badPage.getByRole('heading', { name: 'Ready for a new adventure?' })).toHaveCount(0);
    await bad.close();

    const page = await userPage(browser, 'carol', CAROL_PASSWORD);
    await page.context().close();
  });

  test('carol\'s cart shows the three seeded items with line and grand totals', async ({ browser }) => {
    const page = await userPage(browser, 'carol', CAROL_PASSWORD);
    await page.goto('/cart');
    await expect(page.getByRole('heading', { name: 'Shopping bag', exact: true })).toBeVisible();
    await expect(page.locator('.cart-item')).toHaveCount(3);

    for (const line of carolLines) {
      const row = page.locator('.cart-item').filter({ has: page.getByText(line.name, { exact: true }) });
      await expect(row.locator('.price')).toHaveText(line.price);
      await expect(row.locator('[data-cart-quantity-input]')).toHaveValue(String(line.quantity));
      await expect(row.locator('.catalog-item-total')).toHaveText(line.total);
    }
    await expect(page.locator('.cart-summary-total')).toContainText(carolGrandTotal);
    await expect(page.locator('.cart-summary .filter-badge')).toHaveText('6');
    await page.context().close();
  });

  test('bob\'s cart uses the catalog price, not the stale price stored in Redis', async ({ browser }) => {
    const page = await userPage(browser, 'bob');
    const row = await cartLine(page, 'Explorer 45L Backpack');
    await expect(page.locator('.cart-item')).toHaveCount(1);
    await expect(row.locator('.price')).toHaveText('$149.99');
    await expect(row.locator('[data-cart-quantity-input]')).toHaveValue('2');
    await expect(row.locator('.catalog-item-total')).toHaveText('$299.98');
    await expect(page.locator('.cart-summary-total')).toContainText('$299.98');
    await expect(page.getByText('$99.99')).toHaveCount(0);
    await page.context().close();
  });

  test('carol checks out; the order appears with the expected total and her cart is empty', async ({ browser }) => {
    const page = await userPage(browser, 'carol', CAROL_PASSWORD);

    await page.goto('/user/orders');
    await expect(page.getByRole('heading', { name: 'Orders', exact: true })).toBeVisible();
    await expect(page.locator('.order-number')).toHaveCount(0);

    await page.goto('/cart');
    await expect(page.getByRole('heading', { name: 'Shopping bag', exact: true })).toBeVisible();
    await page.getByRole('link', { name: 'Check out' }).click();

    await expect(page.getByRole('heading', { name: 'Checkout' })).toBeVisible();
    await expect(page.getByLabel('Address')).toHaveValue('400 Broad St');
    await expect(page.getByLabel('City')).toHaveValue('Seattle');
    await page.getByRole('button', { name: 'Place order' }).click();

    await expect(page).toHaveURL(/\/user\/orders$/);
    await expect(page.getByRole('heading', { name: 'Orders', exact: true })).toBeVisible();
    await expect(page.locator('.order-number')).toHaveCount(1);
    await expect(page.locator('.orders-item .order-total')).toHaveText(carolGrandTotal);

    await page.goto('/cart');
    await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
    await page.context().close();
  });
});
