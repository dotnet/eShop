import { test, expect } from '@playwright/test';

test('Remove item from cart', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
  
  await page.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await expect(page.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();
  
  await page.getByRole('button', { name: 'Add to shopping bag' }).click();
  await page.getByRole('link', { name: 'shopping bag' }).click();
  await expect(page.getByRole('heading', { name: 'Shopping bag' })).toBeVisible();

  await expect.poll(() => page.getByLabel('product quantity').count()).toBeGreaterThan(0);
  
  await page.getByLabel('product quantity').fill('0');

  await page.getByRole('button', { name: 'Update' }).click();

  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
});