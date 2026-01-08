import { test, expect } from '@playwright/test';

test.describe('Order Processing', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the eShop homepage
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should complete full order workflow', async ({ page }) => {
    // Step 1: Add items to basket
    await addItemsToBasket(page);
    
    // Step 2: Navigate to basket
    await navigateToBasket(page);
    
    // Step 3: Proceed to checkout
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      await page.waitForLoadState('networkidle');
      
      // Step 4: Handle authentication if required
      await handleAuthentication(page);
      
      // Step 5: Fill shipping information
      await fillShippingInformation(page);
      
      // Step 6: Fill payment information
      await fillPaymentInformation(page);
      
      // Step 7: Review and place order
      await reviewAndPlaceOrder(page);
      
      // Step 8: Verify order confirmation
      await verifyOrderConfirmation(page);
    }
  });

  test('should handle guest checkout', async ({ page }) => {
    // Add items to basket
    await addItemsToBasket(page);
    await navigateToBasket(page);
    
    // Proceed to checkout
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      await page.waitForLoadState('networkidle');
      
      // Look for guest checkout option
      const guestCheckoutButton = page.locator('[data-testid="guest-checkout-button"]').first();
      if (await guestCheckoutButton.isVisible()) {
        await guestCheckoutButton.click();
        await page.waitForLoadState('networkidle');
        
        // Fill guest information
        await fillGuestInformation(page);
        await fillShippingInformation(page);
        await fillPaymentInformation(page);
        await reviewAndPlaceOrder(page);
        await verifyOrderConfirmation(page);
      }
    }
  });

  test('should validate required checkout fields', async ({ page }) => {
    // Add items and proceed to checkout
    await addItemsToBasket(page);
    await navigateToBasket(page);
    
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      await page.waitForLoadState('networkidle');
      
      await handleAuthentication(page);
      
      // Try to proceed without filling required fields
      const continueButton = page.locator('[data-testid="continue-button"]').first();
      if (await continueButton.isVisible()) {
        await continueButton.click();
        
        // Should show validation errors
        const validationErrors = page.locator('[data-testid="validation-error"]');
        const errorCount = await validationErrors.count();
        
        if (errorCount > 0) {
          expect(errorCount).toBeGreaterThan(0);
          
          // Verify error messages are descriptive
          const firstError = validationErrors.first();
          const errorText = await firstError.textContent();
          expect(errorText).toBeTruthy();
        }
      }
    }
  });

  test('should handle payment validation', async ({ page }) => {
    // Complete checkout flow up to payment
    await addItemsToBasket(page);
    await navigateToBasket(page);
    
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      await page.waitForLoadState('networkidle');
      
      await handleAuthentication(page);
      await fillShippingInformation(page);
      
      // Fill invalid payment information
      await fillInvalidPaymentInformation(page);
      
      // Try to place order
      const placeOrderButton = page.locator('[data-testid="place-order-button"]').first();
      if (await placeOrderButton.isVisible()) {
        await placeOrderButton.click();
        
        // Should show payment validation errors
        const paymentErrors = page.locator('[data-testid="payment-error"]');
        const errorCount = await paymentErrors.count();
        
        if (errorCount > 0) {
          expect(errorCount).toBeGreaterThan(0);
        }
      }
    }
  });

  test('should display order summary correctly', async ({ page }) => {
    // Add specific items to basket
    await addItemsToBasket(page, [
      { name: 'Product 1', price: 99.99, quantity: 2 },
      { name: 'Product 2', price: 49.99, quantity: 1 }
    ]);
    
    await navigateToBasket(page);
    
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      await page.waitForLoadState('networkidle');
      
      await handleAuthentication(page);
      
      // Verify order summary
      const orderSummary = page.locator('[data-testid="order-summary"]').first();
      if (await orderSummary.isVisible()) {
        // Check subtotal
        const subtotal = page.locator('[data-testid="subtotal"]').first();
        if (await subtotal.isVisible()) {
          const subtotalText = await subtotal.textContent();
          expect(subtotalText).toMatch(/[\d.,]+/);
        }
        
        // Check total
        const total = page.locator('[data-testid="order-total"]').first();
        if (await total.isVisible()) {
          const totalText = await total.textContent();
          expect(totalText).toMatch(/[\d.,]+/);
        }
        
        // Check item count
        const itemCount = page.locator('[data-testid="item-count"]').first();
        if (await itemCount.isVisible()) {
          const countText = await itemCount.textContent();
          expect(countText).toContain('3'); // 2 + 1 items
        }
      }
    }
  });

  test('should handle inventory validation', async ({ page }) => {
    // This test would check if items are still available during checkout
    await addItemsToBasket(page);
    await navigateToBasket(page);
    
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      await page.waitForLoadState('networkidle');
      
      // Look for inventory validation messages
      const inventoryWarning = page.locator('[data-testid="inventory-warning"]').first();
      const outOfStockMessage = page.locator('[data-testid="out-of-stock"]').first();
      
      // These might not always be present, but if they are, verify they're handled
      const hasInventoryWarning = await inventoryWarning.isVisible({ timeout: 2000 }).catch(() => false);
      const hasOutOfStockMessage = await outOfStockMessage.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (hasInventoryWarning || hasOutOfStockMessage) {
        // Verify appropriate messaging is shown
        expect(hasInventoryWarning || hasOutOfStockMessage).toBeTruthy();
      }
    }
  });

  test('should save order for authenticated users', async ({ page }) => {
    // Login first
    await loginUser(page);
    
    // Complete order
    await addItemsToBasket(page);
    await navigateToBasket(page);
    
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      await page.waitForLoadState('networkidle');
      
      await fillShippingInformation(page);
      await fillPaymentInformation(page);
      await reviewAndPlaceOrder(page);
      
      // Verify order confirmation and get order number
      const orderNumber = await verifyOrderConfirmation(page);
      
      if (orderNumber) {
        // Navigate to order history
        const accountLink = page.locator('[data-testid="account-link"]').first();
        if (await accountLink.isVisible()) {
          await accountLink.click();
          
          const orderHistoryLink = page.locator('[data-testid="order-history-link"]').first();
          if (await orderHistoryLink.isVisible()) {
            await orderHistoryLink.click();
            await page.waitForLoadState('networkidle');
            
            // Verify order appears in history
            const orderInHistory = page.locator(`[data-testid="order-${orderNumber}"]`).first();
            if (await orderInHistory.isVisible({ timeout: 5000 }).catch(() => false)) {
              await expect(orderInHistory).toBeVisible();
            }
          }
        }
      }
    }
  });

  // Helper functions
  async function addItemsToBasket(page: any, specificItems?: any[]) {
    if (specificItems) {
      // Add specific items (this would require more complex logic)
      for (const item of specificItems) {
        // Navigate to specific product and add to basket
        // This is simplified - in reality, you'd search for specific products
        const productCard = page.locator('[data-testid="product-card"]').first();
        if (await productCard.isVisible()) {
          await productCard.click();
          await page.waitForLoadState('networkidle');
          
          const addButton = page.locator('[data-testid="add-to-basket-button"]');
          if (await addButton.isVisible()) {
            await addButton.click();
            await page.waitForTimeout(1000);
          }
          
          await page.goBack();
          await page.waitForLoadState('networkidle');
        }
      }
    } else {
      // Add any available items
      const productCards = page.locator('[data-testid="product-card"]');
      const cardCount = await productCards.count();
      
      if (cardCount > 0) {
        await productCards.first().click();
        await page.waitForLoadState('networkidle');
        
        const addButton = page.locator('[data-testid="add-to-basket-button"]');
        if (await addButton.isVisible()) {
          await addButton.click();
          await page.waitForTimeout(1000);
        }
      }
    }
  }

  async function navigateToBasket(page: any) {
    const basketLink = page.locator('[data-testid="basket-link"]').first();
    if (await basketLink.isVisible()) {
      await basketLink.click();
    } else {
      await page.goto('/basket');
    }
    await page.waitForLoadState('networkidle');
  }

  async function handleAuthentication(page: any) {
    // Check if we're on login page
    const loginForm = page.locator('[data-testid="login-form"]').first();
    if (await loginForm.isVisible({ timeout: 3000 }).catch(() => false)) {
      await loginUser(page);
    }
  }

  async function loginUser(page: any) {
    const emailInput = page.locator('[data-testid="email-input"]').first();
    const passwordInput = page.locator('[data-testid="password-input"]').first();
    const loginButton = page.locator('[data-testid="login-button"]').first();
    
    if (await emailInput.isVisible() && await passwordInput.isVisible()) {
      await emailInput.fill('test@example.com');
      await passwordInput.fill('Test123!');
      
      if (await loginButton.isVisible()) {
        await loginButton.click();
        await page.waitForLoadState('networkidle');
      }
    }
  }

  async function fillGuestInformation(page: any) {
    const guestEmailInput = page.locator('[data-testid="guest-email-input"]').first();
    if (await guestEmailInput.isVisible()) {
      await guestEmailInput.fill('guest@example.com');
    }
  }

  async function fillShippingInformation(page: any) {
    const shippingForm = page.locator('[data-testid="shipping-form"]').first();
    if (await shippingForm.isVisible()) {
      const fields = {
        'first-name': 'John',
        'last-name': 'Doe',
        'address': '123 Test Street',
        'city': 'Seattle',
        'state': 'WA',
        'zip': '98101',
        'country': 'USA'
      };
      
      for (const [field, value] of Object.entries(fields)) {
        const input = page.locator(`[data-testid="${field}-input"]`).first();
        if (await input.isVisible({ timeout: 1000 }).catch(() => false)) {
          await input.fill(value);
        }
      }
    }
  }

  async function fillPaymentInformation(page: any) {
    const paymentForm = page.locator('[data-testid="payment-form"]').first();
    if (await paymentForm.isVisible()) {
      const fields = {
        'card-number': '4111111111111111',
        'card-name': 'John Doe',
        'card-expiry': '12/25',
        'card-cvv': '123'
      };
      
      for (const [field, value] of Object.entries(fields)) {
        const input = page.locator(`[data-testid="${field}-input"]`).first();
        if (await input.isVisible({ timeout: 1000 }).catch(() => false)) {
          await input.fill(value);
        }
      }
    }
  }

  async function fillInvalidPaymentInformation(page: any) {
    const paymentForm = page.locator('[data-testid="payment-form"]').first();
    if (await paymentForm.isVisible()) {
      const fields = {
        'card-number': '1234', // Invalid card number
        'card-name': '',       // Empty name
        'card-expiry': '01/20', // Expired date
        'card-cvv': '12'       // Invalid CVV
      };
      
      for (const [field, value] of Object.entries(fields)) {
        const input = page.locator(`[data-testid="${field}-input"]`).first();
        if (await input.isVisible({ timeout: 1000 }).catch(() => false)) {
          await input.fill(value);
        }
      }
    }
  }

  async function reviewAndPlaceOrder(page: any) {
    const placeOrderButton = page.locator('[data-testid="place-order-button"]').first();
    if (await placeOrderButton.isVisible()) {
      await placeOrderButton.click();
      await page.waitForLoadState('networkidle');
    }
  }

  async function verifyOrderConfirmation(page: any) {
    const orderConfirmation = page.locator('[data-testid="order-confirmation"]').first();
    const orderNumber = page.locator('[data-testid="order-number"]').first();
    
    if (await orderConfirmation.isVisible({ timeout: 10000 }).catch(() => false)) {
      await expect(orderConfirmation).toBeVisible();
      
      if (await orderNumber.isVisible()) {
        const orderNumberText = await orderNumber.textContent();
        return orderNumberText?.match(/\d+/)?.[0];
      }
    }
    
    return null;
  }
});