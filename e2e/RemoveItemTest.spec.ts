import { test, expect } from '@playwright/test';
import 'dotenv/config';

test('Remove item from cart', async ({ page }) => {
  await page.goto('https://localhost:19888/');
  const page1Promise = page.waitForEvent('popup');
  await page.getByRole('link', { name: 'https://localhost:7298' }).click();
  const page1 = await page1Promise;
  await expect(page1.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
  
  await page1.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await expect(page1.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();
  
  await page1.getByRole('button', { name: 'Add to shopping bag' }).click();
  await page1.getByRole('link', { name: 'shopping bag' }).click();
  await expect(page1.getByRole('heading', { name: 'Shopping bag' })).toBeVisible();
  
  const cartVal = await page1.getByLabel('product quantity').count();
  await expect(cartVal).toBeGreaterThan(0);
  
  await page1.getByLabel('product quantity').fill('0');

  await page1.getByRole('button', { name: 'Update' }).click();

  await expect(page1.getByText('Your shopping bag is empty')).toBeVisible();
});