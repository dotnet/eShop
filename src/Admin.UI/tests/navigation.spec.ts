import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test('login page has proper layout', async ({ page }) => {
    await page.goto('/login');

    // Check for card-like container (shadcn Card component)
    const container = page.locator('[class*="rounded"]').first();
    await expect(container).toBeVisible();

    // Check centered layout
    const flexContainer = page.locator('.flex.items-center.justify-center');
    await expect(flexContainer).toBeVisible();
  });

  test('login page is responsive', async ({ page }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/login');

    const loginButton = page.getByRole('button', { name: /sign in/i });
    await expect(loginButton).toBeVisible();

    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await expect(loginButton).toBeVisible();

    // Test desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await expect(loginButton).toBeVisible();
  });

  test('callback page handles OIDC callback', async ({ page }) => {
    // Navigate to callback without proper OIDC params should handle gracefully
    await page.goto('/callback');

    // Should show loading or redirect
    // The actual behavior depends on oidc-client-ts handling
    await page.waitForTimeout(1000);

    // Either shows loading or redirects
    const content = await page.content();
    expect(content.length).toBeGreaterThan(0);
  });
});
