import { test, expect } from '@playwright/test';

test('Browse Items', async ({ page }) => {
  await page.goto('https://localhost:19888/');
  await expect(page.getByRole('heading', { name: 'Resources' })).toBeVisible();
  await page.getByRole('link', { name: 'eShop dashboard' }).click();
  const page1Promise = page.waitForEvent('popup');
  await page.getByRole('link', { name: 'https://localhost:7298' }).click();
  const page1 = await page1Promise;

  await expect(page1.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();

  await page1.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await page1.getByRole('heading', { name: 'Adventurer GPS Watch' }).click();
  
  //Expect
  await expect(page1.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();
});