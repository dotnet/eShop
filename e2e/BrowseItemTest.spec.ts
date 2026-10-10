import { test, expect } from '@playwright/test';

test('browse catalog pages and view product details', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
  await page.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await expect(page.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();

  await page.goto('/');
  await page.getByRole('link', { name: '2', exact: true }).click();
  await expect(page).toHaveURL(/\?page=2$/);
  await expect(page.locator('.catalog-product')).toHaveCount(9);
});