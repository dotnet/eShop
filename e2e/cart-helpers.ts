import { expect, Page } from '@playwright/test';

export async function addProductToCart(page: Page, productName: string) {
  await page.goto('/');
  await page.getByRole('link', { name: productName }).click();
  await expect(page.getByRole('heading', { name: productName })).toBeVisible();
  await page.getByRole('button', { name: 'Add to shopping bag' }).click();
  await expect(page.locator('.in-cart').getByRole('link', { name: 'shopping bag' })).toBeVisible();
}

export async function submitCartUpdate(page: Page) {
  const updateRequest = page.waitForRequest(request =>
    request.method() === 'POST' && new URL(request.url()).pathname === '/cart');

  await Promise.all([
    updateRequest,
    page.getByRole('button', { name: 'Update' }).first().click(),
  ]);
}

export async function emptyCart(page: Page) {
  await page.goto('/cart');
  await expect(page.getByRole('heading', { name: 'Shopping bag' })).toBeVisible();

  const quantities = page.getByLabel('product quantity');
  while (await quantities.count() > 0) {
    const previousCount = await quantities.count();
    await quantities.first().fill('0');
    await submitCartUpdate(page);
    await expect(quantities).toHaveCount(previousCount - 1, { timeout: 15_000 });
  }

  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
}
