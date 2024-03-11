import { test, expect } from '@playwright/test';

test('Add itrm to the cart', async ({ page }) => {
  await page.goto('https://localhost:19888/');
  await page.getByRole('heading', { name: 'Resources' }).click();
  await expect(page.getByRole('heading', { name: 'Resources' })).toBeVisible();

  const page1Promise = page.waitForEvent('popup');
  await page.getByRole('link', { name: 'https://localhost:7298' }).click();
  const page1 = await page1Promise;
  await expect(page1.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
  
  await page1.getByLabel('Sign in').click();
  await expect(page1.getByRole('heading', { name: 'Login' })).toBeVisible();

  await page1.getByPlaceholder('Username').fill('bob');
  await page1.getByPlaceholder('Password').fill('Pass123$');
  await page1.getByRole('button', { name: 'Login' }).click();
  await page1.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await page1.getByRole('button', { name: 'Add to shopping bag' }).click();
  await page1.getByRole('link', { name: 'shopping bag' }).click();
  await page1.getByRole('heading', { name: 'Shopping bag' }).click();  
 
  await page1.getByText('Total').nth(1).click();
  await page1.getByLabel('product quantity').getByText('1');
  const cartVal = await page1.getByLabel('product quantity').count();
  await expect(cartVal).toBeGreaterThan(0);

});