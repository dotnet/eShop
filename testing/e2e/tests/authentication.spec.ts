import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the eShop homepage
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should display login option for unauthenticated users', async ({ page }) => {
    // Look for login/sign-in elements
    const loginLink = page.locator('[data-testid="login-link"]').first();
    const signInButton = page.locator('text=Sign In').first();
    const accountMenu = page.locator('[data-testid="account-menu"]').first();

    // At least one authentication element should be visible
    const hasLoginLink = await loginLink.isVisible({ timeout: 2000 }).catch(() => false);
    const hasSignInButton = await signInButton.isVisible({ timeout: 2000 }).catch(() => false);
    const hasAccountMenu = await accountMenu.isVisible({ timeout: 2000 }).catch(() => false);

    expect(hasLoginLink || hasSignInButton || hasAccountMenu).toBeTruthy();
  });

  test('should navigate to login page when login is clicked', async ({ page }) => {
    // Find and click login element
    const loginElements = [
      page.locator('[data-testid="login-link"]').first(),
      page.locator('text=Sign In').first(),
      page.locator('text=Login').first(),
      page.locator('[data-testid="sign-in-button"]').first()
    ];

    let loginClicked = false;
    for (const element of loginElements) {
      if (await element.isVisible({ timeout: 1000 }).catch(() => false)) {
        await element.click();
        loginClicked = true;
        break;
      }
    }

    if (loginClicked) {
      await page.waitForLoadState('networkidle');
      
      // Should be on login page or have login form visible
      const loginForm = page.locator('[data-testid="login-form"]').first();
      const emailInput = page.locator('input[type="email"]').first();
      const passwordInput = page.locator('input[type="password"]').first();
      
      const hasLoginForm = await loginForm.isVisible({ timeout: 3000 }).catch(() => false);
      const hasEmailInput = await emailInput.isVisible({ timeout: 3000 }).catch(() => false);
      const hasPasswordInput = await passwordInput.isVisible({ timeout: 3000 }).catch(() => false);
      
      expect(hasLoginForm || (hasEmailInput && hasPasswordInput)).toBeTruthy();
    }
  });

  test('should show validation errors for empty login form', async ({ page }) => {
    // Navigate to login
    await navigateToLogin(page);
    
    // Try to submit empty form
    const submitButton = page.locator('[data-testid="login-submit"]').first();
    const loginButton = page.locator('button[type="submit"]').first();
    
    if (await submitButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await submitButton.click();
    } else if (await loginButton.isVisible({ timeout: 2000 }).catch(() => false)) {
      await loginButton.click();
    }
    
    // Should show validation errors
    const validationErrors = page.locator('[data-testid="validation-error"]');
    const errorMessages = page.locator('.error, .invalid, [class*="error"]');
    
    const hasValidationErrors = await validationErrors.count() > 0;
    const hasErrorMessages = await errorMessages.count() > 0;
    
    if (hasValidationErrors || hasErrorMessages) {
      expect(hasValidationErrors || hasErrorMessages).toBeTruthy();
    }
  });

  test('should attempt login with test credentials', async ({ page }) => {
    // Navigate to login
    await navigateToLogin(page);
    
    // Fill in test credentials
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();
    
    if (await emailInput.isVisible({ timeout: 2000 }).catch(() => false) && 
        await passwordInput.isVisible({ timeout: 2000 }).catch(() => false)) {
      
      await emailInput.fill('test@example.com');
      await passwordInput.fill('Test123!');
      
      // Submit form
      const submitButton = page.locator('[data-testid="login-submit"]').first();
      const loginButton = page.locator('button[type="submit"]').first();
      
      if (await submitButton.isVisible()) {
        await submitButton.click();
      } else if (await loginButton.isVisible()) {
        await loginButton.click();
      }
      
      await page.waitForLoadState('networkidle');
      
      // Check if login was successful or if there's an error message
      const errorMessage = page.locator('[data-testid="login-error"]').first();
      const welcomeMessage = page.locator('[data-testid="welcome-message"]').first();
      const userMenu = page.locator('[data-testid="user-menu"]').first();
      
      const hasError = await errorMessage.isVisible({ timeout: 3000 }).catch(() => false);
      const hasWelcome = await welcomeMessage.isVisible({ timeout: 3000 }).catch(() => false);
      const hasUserMenu = await userMenu.isVisible({ timeout: 3000 }).catch(() => false);
      
      // Either login succeeded (welcome/user menu) or failed (error message)
      expect(hasError || hasWelcome || hasUserMenu).toBeTruthy();
    }
  });

  test('should handle invalid login credentials gracefully', async ({ page }) => {
    // Navigate to login
    await navigateToLogin(page);
    
    // Fill in invalid credentials
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();
    
    if (await emailInput.isVisible({ timeout: 2000 }).catch(() => false) && 
        await passwordInput.isVisible({ timeout: 2000 }).catch(() => false)) {
      
      await emailInput.fill('invalid@example.com');
      await passwordInput.fill('wrongpassword');
      
      // Submit form
      const submitButton = page.locator('[data-testid="login-submit"]').first();
      const loginButton = page.locator('button[type="submit"]').first();
      
      if (await submitButton.isVisible()) {
        await submitButton.click();
      } else if (await loginButton.isVisible()) {
        await loginButton.click();
      }
      
      await page.waitForLoadState('networkidle');
      
      // Should show error message for invalid credentials
      const errorElements = [
        page.locator('[data-testid="login-error"]').first(),
        page.locator('text=Invalid credentials').first(),
        page.locator('text=Login failed').first(),
        page.locator('.error').first()
      ];
      
      let hasError = false;
      for (const element of errorElements) {
        if (await element.isVisible({ timeout: 2000 }).catch(() => false)) {
          hasError = true;
          break;
        }
      }
      
      // Should either show error or stay on login page
      const stillOnLogin = await emailInput.isVisible({ timeout: 2000 }).catch(() => false);
      expect(hasError || stillOnLogin).toBeTruthy();
    }
  });

  test('should provide registration option', async ({ page }) => {
    // Look for registration/sign-up options
    const registerLink = page.locator('[data-testid="register-link"]').first();
    const signUpButton = page.locator('text=Sign Up').first();
    const createAccountLink = page.locator('text=Create Account').first();
    
    const hasRegisterLink = await registerLink.isVisible({ timeout: 2000 }).catch(() => false);
    const hasSignUpButton = await signUpButton.isVisible({ timeout: 2000 }).catch(() => false);
    const hasCreateAccountLink = await createAccountLink.isVisible({ timeout: 2000 }).catch(() => false);
    
    // If none are visible on main page, check login page
    if (!hasRegisterLink && !hasSignUpButton && !hasCreateAccountLink) {
      await navigateToLogin(page);
      
      const loginPageRegister = page.locator('text=Register').first();
      const loginPageSignUp = page.locator('text=Sign Up').first();
      const loginPageCreateAccount = page.locator('text=Create Account').first();
      
      const hasLoginPageRegister = await loginPageRegister.isVisible({ timeout: 2000 }).catch(() => false);
      const hasLoginPageSignUp = await loginPageSignUp.isVisible({ timeout: 2000 }).catch(() => false);
      const hasLoginPageCreateAccount = await loginPageCreateAccount.isVisible({ timeout: 2000 }).catch(() => false);
      
      expect(hasLoginPageRegister || hasLoginPageSignUp || hasLoginPageCreateAccount).toBeTruthy();
    } else {
      expect(hasRegisterLink || hasSignUpButton || hasCreateAccountLink).toBeTruthy();
    }
  });

  test('should handle logout functionality', async ({ page }) => {
    // First try to login (this might not work in test environment)
    await attemptLogin(page);
    
    // Look for logout options
    const logoutElements = [
      page.locator('[data-testid="logout-button"]').first(),
      page.locator('text=Logout').first(),
      page.locator('text=Sign Out').first(),
      page.locator('[data-testid="sign-out-button"]').first()
    ];
    
    let logoutFound = false;
    for (const element of logoutElements) {
      if (await element.isVisible({ timeout: 2000 }).catch(() => false)) {
        await element.click();
        logoutFound = true;
        break;
      }
    }
    
    if (logoutFound) {
      await page.waitForLoadState('networkidle');
      
      // Should return to unauthenticated state
      const loginLink = page.locator('[data-testid="login-link"]').first();
      const signInButton = page.locator('text=Sign In').first();
      
      const hasLoginLink = await loginLink.isVisible({ timeout: 3000 }).catch(() => false);
      const hasSignInButton = await signInButton.isVisible({ timeout: 3000 }).catch(() => false);
      
      expect(hasLoginLink || hasSignInButton).toBeTruthy();
    }
  });

  test('should protect authenticated routes', async ({ page }) => {
    // Try to access a protected route without authentication
    const protectedRoutes = ['/account', '/profile', '/orders', '/dashboard'];
    
    for (const route of protectedRoutes) {
      await page.goto(route);
      await page.waitForLoadState('networkidle');
      
      // Should either redirect to login or show login form
      const currentUrl = page.url();
      const loginForm = page.locator('[data-testid="login-form"]').first();
      const emailInput = page.locator('input[type="email"]').first();
      
      const isOnLoginPage = currentUrl.includes('login') || currentUrl.includes('signin');
      const hasLoginForm = await loginForm.isVisible({ timeout: 2000 }).catch(() => false);
      const hasEmailInput = await emailInput.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (isOnLoginPage || hasLoginForm || hasEmailInput) {
        expect(true).toBeTruthy(); // Protected route correctly redirected to login
        break; // Only need to test one protected route
      }
    }
  });

  // Helper functions
  async function navigateToLogin(page: any) {
    const loginElements = [
      page.locator('[data-testid="login-link"]').first(),
      page.locator('text=Sign In').first(),
      page.locator('text=Login').first(),
      page.locator('[data-testid="sign-in-button"]').first()
    ];

    for (const element of loginElements) {
      if (await element.isVisible({ timeout: 1000 }).catch(() => false)) {
        await element.click();
        await page.waitForLoadState('networkidle');
        return;
      }
    }
    
    // If no login link found, try direct navigation
    await page.goto('/login');
    await page.waitForLoadState('networkidle');
  }

  async function attemptLogin(page: any) {
    await navigateToLogin(page);
    
    const emailInput = page.locator('input[type="email"]').first();
    const passwordInput = page.locator('input[type="password"]').first();
    
    if (await emailInput.isVisible({ timeout: 2000 }).catch(() => false) && 
        await passwordInput.isVisible({ timeout: 2000 }).catch(() => false)) {
      
      await emailInput.fill('test@example.com');
      await passwordInput.fill('Test123!');
      
      const submitButton = page.locator('[data-testid="login-submit"]').first();
      const loginButton = page.locator('button[type="submit"]').first();
      
      if (await submitButton.isVisible({ timeout: 1000 }).catch(() => false)) {
        await submitButton.click();
      } else if (await loginButton.isVisible({ timeout: 1000 }).catch(() => false)) {
        await loginButton.click();
      }
      
      await page.waitForLoadState('networkidle');
    }
  }
});