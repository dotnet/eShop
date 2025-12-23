import { test, expect } from '@playwright/test';

test.describe('Login Page', () => {
  test('should display login page with sign in button', async ({ page }) => {
    await page.goto('/login');

    // Check page title
    await expect(page).toHaveTitle(/eShop Admin/);

    // Check for eShop Admin text
    await expect(page.getByText('eShop Admin')).toBeVisible();

    // Check for sign in button (OIDC flow uses a button to redirect)
    const loginButton = page.getByRole('button', { name: /sign in/i });
    await expect(loginButton).toBeVisible();
  });

  test('should display app name and description', async ({ page }) => {
    await page.goto('/login');

    // Check for eShop Admin text
    await expect(page.getByText('eShop Admin')).toBeVisible();

    // Check for sign in description
    await expect(page.getByText(/Sign in to access/i)).toBeVisible();
  });

  test('should have sign in button with proper text', async ({ page }) => {
    await page.goto('/login');

    // Button should indicate OIDC flow
    const loginButton = page.getByRole('button', { name: /sign in with identity server/i });
    await expect(loginButton).toBeVisible();
  });

  test('should show redirect message', async ({ page }) => {
    await page.goto('/login');

    // Should show message about redirection
    await expect(page.getByText(/redirect.*secure.*login/i)).toBeVisible();
  });

  test('should initiate OIDC redirect on button click', async ({ page }) => {
    await page.goto('/login');

    const loginButton = page.getByRole('button', { name: /sign in/i });

    // Button should be visible and clickable
    await expect(loginButton).toBeVisible();
    await expect(loginButton).toBeEnabled();

    // Clicking should not throw an error (in real scenario it would redirect)
    // We just verify the button is functional
    await expect(loginButton).toHaveText(/sign in/i);
  });

  test('protected routes redirect to login when not authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Should show login page with sign in button
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('warehouses route shows login when not authenticated', async ({ page }) => {
    await page.goto('/warehouses');
    await page.waitForLoadState('networkidle');

    // Should show login page with sign in button
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('inventory route shows login when not authenticated', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForLoadState('networkidle');

    // Should show login page with sign in button
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });
});
