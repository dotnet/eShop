import { test as setup, expect } from '@playwright/test';
import { STORAGE_STATE } from '../playwright.config';

setup('Login', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('heading', { name: 'Resources' }).click();
    const page1Promise = page.waitForEvent('popup');
    await page.getByRole('link', { name: 'https://localhost:7298' }).click();
    const page1 = await page1Promise;
    await expect(page1.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
    
    await page1.getByLabel('Sign in').click();
    await expect(page1.getByRole('heading', { name: 'Login' })).toBeVisible();
    
    await page1.getByPlaceholder('Username').fill(process.env.USERNAME1);
    await page1.getByPlaceholder('Password').fill(process.env.PASSWORD);
    await page1.getByRole('button', { name: 'Login' }).click();
    await expect(page1.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
    await page.context().storageState({ path: STORAGE_STATE });
})