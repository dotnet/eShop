import { test, expect } from '@playwright/test';

test.describe('UI Components', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
  });

  test('button component renders correctly', async ({ page }) => {
    const button = page.getByRole('button', { name: /sign in/i });
    await expect(button).toBeVisible();

    // Check button has proper styling
    const buttonBox = await button.boundingBox();
    expect(buttonBox).not.toBeNull();
    expect(buttonBox!.height).toBeGreaterThan(30);
  });

  test('card component has proper styling', async ({ page }) => {
    // Look for card-like element
    const cardElements = page.locator('.rounded-lg, .border, .shadow');
    await expect(cardElements.first()).toBeVisible();
  });

  test('login page has proper typography', async ({ page }) => {
    // Check for eShop Admin title text
    const title = page.getByText('eShop Admin');
    await expect(title).toBeVisible();

    // Check description text
    const description = page.getByText(/Sign in to access/i);
    await expect(description).toBeVisible();
  });

  test('page has proper background styling', async ({ page }) => {
    // Check the body or main container has background
    const body = page.locator('body');
    await expect(body).toBeVisible();

    // Get computed styles
    const backgroundColor = await body.evaluate((el) =>
      window.getComputedStyle(el).backgroundColor
    );
    expect(backgroundColor).toBeTruthy();
  });
});
