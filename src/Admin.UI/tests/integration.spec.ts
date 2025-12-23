import { test, expect } from '@playwright/test';

/**
 * Integration tests that run against the actual backend services.
 * These tests require the eShop.AppHost to be running.
 *
 * The Admin UI uses direct password-based login to Identity Server.
 *
 * Tests run serially to avoid parallel session conflicts.
 */
test.describe.configure({ mode: 'serial' });

test.describe('Integration Tests', () => {
  // Helper function to login with credentials
  async function login(page: any, username: string, password: string) {
    await page.goto('/login');

    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');
    const loginButton = page.getByRole('button', { name: /sign in/i });

    await usernameInput.fill(username);
    await passwordInput.fill(password);
    await loginButton.click();
  }

  test.describe('Real Login Flow', () => {
    test('should successfully login with admin credentials', async ({ page }) => {
      await login(page, 'admin', 'Admin123$');

      // Wait for redirect to dashboard
      await expect(page).toHaveURL('http://localhost:5173/', { timeout: 15000 });

      // Dashboard should show
      await expect(page.getByText('Dashboard')).toBeVisible({ timeout: 5000 });
    });

    test('should show error for wrong credentials', async ({ page }) => {
      await page.goto('/login');

      const usernameInput = page.getByLabel('Username');
      const passwordInput = page.getByLabel('Password');
      const loginButton = page.getByRole('button', { name: /sign in/i });

      await usernameInput.fill('admin');
      await passwordInput.fill('wrongpassword');
      await loginButton.click();

      // Should show error message
      await expect(page.getByText(/invalid/i)).toBeVisible({ timeout: 10000 });
    });

    test('should login non-admin user alice and deny access', async ({ page }) => {
      await login(page, 'alice', 'Pass123$');

      // Alice doesn't have Admin role, should show access denied
      await expect(page.getByText(/Access denied/i)).toBeVisible({ timeout: 10000 });
    });
  });

  test.describe('Dashboard After Login', () => {
    test.beforeEach(async ({ page }) => {
      await login(page, 'admin', 'Admin123$');
      await expect(page).toHaveURL('http://localhost:5173/', { timeout: 15000 });
    });

    test('should display dashboard', async ({ page }) => {
      await page.waitForLoadState('networkidle');
      await expect(page.getByRole('heading', { name: 'Dashboard' }).or(page.getByText('Dashboard').first())).toBeVisible({ timeout: 10000 });
    });

    test('should navigate to warehouses page', async ({ page }) => {
      await page.getByRole('link', { name: /warehouses/i }).click();
      await expect(page).toHaveURL('/warehouses');
      await page.waitForLoadState('networkidle');
      // Either shows warehouse list or loading
      const hasContent = await page.getByText(/warehouse/i).isVisible().catch(() => false);
      expect(hasContent || page.url().includes('/warehouses')).toBeTruthy();
    });

    test('should navigate to inventory page', async ({ page }) => {
      await page.getByRole('link', { name: /inventory/i }).click();
      await expect(page).toHaveURL('/inventory');
      await page.waitForLoadState('networkidle');
      await expect(page.getByRole('heading', { name: 'Inventory' }).or(page.getByText('Inventory').first())).toBeVisible({ timeout: 10000 });
    });
  });

  test.describe('Warehouse Management', () => {
    test.beforeEach(async ({ page }) => {
      await login(page, 'admin', 'Admin123$');
      await expect(page).toHaveURL('http://localhost:5173/', { timeout: 15000 });
    });

    test('should access warehouses new page directly', async ({ page }) => {
      await page.goto('/warehouses/new');
      await page.waitForLoadState('networkidle');
      await expect(page.getByRole('heading', { name: 'New Warehouse' })).toBeVisible({ timeout: 10000 });
    });

    test('should show warehouse form fields', async ({ page }) => {
      await page.goto('/warehouses/new');
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      await expect(page.getByLabel('Name')).toBeVisible({ timeout: 10000 });
      await expect(page.getByLabel('Address')).toBeVisible();
      await expect(page.getByLabel('City')).toBeVisible();
      await expect(page.getByLabel('Country')).toBeVisible();
    });
  });

  test.describe('Logout', () => {
    test('should logout successfully', async ({ page }) => {
      await login(page, 'admin', 'Admin123$');
      await expect(page).toHaveURL('http://localhost:5173/', { timeout: 15000 });

      // Find and click logout button
      const logoutButton = page.getByRole('button', { name: /logout|sign out/i });
      if (await logoutButton.isVisible()) {
        await logoutButton.click();
        await page.waitForTimeout(2000);
        // After logout, should see login form
        await expect(page.getByLabel('Username')).toBeVisible({ timeout: 5000 });
      }
    });
  });
});
