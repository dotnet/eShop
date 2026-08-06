import { test, expect } from '@playwright/test';
import { addProductToCart, emptyCart } from './cart-helpers';

test('add, update, and remove an item from the cart', async ({ page }) => {
  await emptyCart(page);
  await addProductToCart(page, 'Adventurer GPS Watch');

  await page.goto('/cart');
  await expect(page.getByRole('heading', { name: 'Shopping bag' })).toBeVisible();
  await expect(page.getByText('Adventurer GPS Watch')).toBeVisible();

  const quantity = page.getByLabel('product quantity');
  await expect(quantity).toHaveValue('1');

  await quantity.fill('2');
  await page.getByRole('button', { name: 'Update' }).click();
  await expect(page.getByLabel('product quantity')).toHaveValue('2');
  await expect(page.locator('.cart-summary-total')).toContainText('$399.98');

  await page.getByLabel('product quantity').fill('0');
  await page.getByRole('button', { name: 'Update' }).click();
  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
});