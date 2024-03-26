import { test, expect } from '@playwright/test';

test('Add item to the cart', async ({ page }) => {
   await page.goto('/');
   
   await expect(page.getByRole('heading', { name: 'Resources' })).toBeVisible();

   const page1Promise = page.waitForEvent('popup');
   await page.getByRole('link', { name: 'https://localhost:7298' }).click();
   const page1 = await page1Promise;
   await expect(page1.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();  
  await page1.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await page1.getByRole('button', { name: 'Add to shopping bag' }).click();
  await page1.getByRole('link', { name: 'shopping bag' }).click();
  await page1.getByRole('heading', { name: 'Shopping bag' }).click();  
 
  await page1.getByText('Total').nth(1).click();
  await page1.getByLabel('product quantity').getByText('1');
  
  await expect.poll(() => page1.getByLabel('product quantity').count()).toBeGreaterThan(0);
});