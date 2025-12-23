import { test, expect } from '@playwright/test';

test.describe('Login Page', () => {
  test('should display login form with username and password fields', async ({ page }) => {
    await page.goto('/login');

    // Check page title
    await expect(page).toHaveTitle(/eShop Admin/);

    // Check form fields exist
    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');
    const loginButton = page.getByRole('button', { name: /sign in/i });

    await expect(usernameInput).toBeVisible();
    await expect(passwordInput).toBeVisible();
    await expect(loginButton).toBeVisible();
  });

  test('should display app name and description', async ({ page }) => {
    await page.goto('/login');

    // Check for eShop Admin text
    await expect(page.getByText('eShop Admin')).toBeVisible();

    // Check for sign in description
    await expect(page.getByText(/Sign in to access/i)).toBeVisible();
  });

  test('should allow typing in form fields', async ({ page }) => {
    await page.goto('/login');

    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');

    await usernameInput.fill('testuser');
    await passwordInput.fill('testpassword');

    await expect(usernameInput).toHaveValue('testuser');
    await expect(passwordInput).toHaveValue('testpassword');
  });

  test('should show error message on failed login', async ({ page }) => {
    // Mock the token endpoint to return an error
    await page.route('**/connect/token', async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          error: 'invalid_grant',
          error_description: 'Invalid username or password',
        }),
      });
    });

    await page.goto('/login');

    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');
    const loginButton = page.getByRole('button', { name: /sign in/i });

    await usernameInput.fill('wronguser');
    await passwordInput.fill('wrongpassword');
    await loginButton.click();

    // Wait for error message
    const errorMessage = page.getByText(/Invalid username or password/i);
    await expect(errorMessage).toBeVisible();
  });

  test('should show loading state during login', async ({ page }) => {
    // Mock slow response
    await page.route('**/connect/token', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'invalid_grant' }),
      });
    });

    await page.goto('/login');

    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');
    const loginButton = page.getByRole('button', { name: /sign in/i });

    await usernameInput.fill('user');
    await passwordInput.fill('pass');
    await loginButton.click();

    // Check for loading state
    await expect(page.getByText(/Signing in/i)).toBeVisible();
  });

  test('should redirect to dashboard on successful login', async ({ page }) => {
    // Mock successful token response
    await page.route('**/connect/token', async (route) => {
      const mockToken = btoa(JSON.stringify({ alg: 'HS256' })) + '.' +
        btoa(JSON.stringify({
          sub: '1',
          name: 'Admin User',
          email: 'admin@test.com',
          role: 'Admin',
          iss: 'test',
          aud: 'admin-ui',
          exp: Math.floor(Date.now() / 1000) + 3600,
          iat: Math.floor(Date.now() / 1000),
        })) + '.signature';

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          access_token: mockToken,
          token_type: 'Bearer',
          expires_in: 3600,
          scope: 'openid profile roles warehouse orders offline_access',
        }),
      });
    });

    await page.goto('/login');

    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');
    const loginButton = page.getByRole('button', { name: /sign in/i });

    await usernameInput.fill('admin');
    await passwordInput.fill('Admin123$');
    await loginButton.click();

    // Should redirect to dashboard
    await expect(page).toHaveURL('/');
  });

  test('should deny access for non-admin users', async ({ page }) => {
    // Mock token response without Admin role
    await page.route('**/connect/token', async (route) => {
      const mockToken = btoa(JSON.stringify({ alg: 'HS256' })) + '.' +
        btoa(JSON.stringify({
          sub: '2',
          name: 'Regular User',
          email: 'user@test.com',
          role: 'User',
          iss: 'test',
          aud: 'admin-ui',
          exp: Math.floor(Date.now() / 1000) + 3600,
          iat: Math.floor(Date.now() / 1000),
        })) + '.signature';

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          access_token: mockToken,
          token_type: 'Bearer',
          expires_in: 3600,
          scope: 'openid profile roles',
        }),
      });
    });

    await page.goto('/login');

    const usernameInput = page.getByLabel('Username');
    const passwordInput = page.getByLabel('Password');
    const loginButton = page.getByRole('button', { name: /sign in/i });

    await usernameInput.fill('alice');
    await passwordInput.fill('Pass123$');
    await loginButton.click();

    // Should show access denied error
    const errorMessage = page.getByText(/Access denied/i);
    await expect(errorMessage).toBeVisible();
  });

  test('protected routes redirect to login when not authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Should show login form
    await expect(page.getByLabel('Username')).toBeVisible();
  });

  test('warehouses route redirects to login when not authenticated', async ({ page }) => {
    await page.goto('/warehouses');
    await page.waitForLoadState('networkidle');

    // Should show login form
    await expect(page.getByLabel('Username')).toBeVisible();
  });

  test('inventory route redirects to login when not authenticated', async ({ page }) => {
    await page.goto('/inventory');
    await page.waitForLoadState('networkidle');

    // Should show login form
    await expect(page.getByLabel('Username')).toBeVisible();
  });
});
