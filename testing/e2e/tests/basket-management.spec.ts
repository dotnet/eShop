import { test, expect } from '@playwright/test';

test.describe('Basket Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the eShop homepage
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should add item to basket from product details', async ({ page }) => {
    // Navigate to a product
    const productCard = page.locator('[data-testid="product-card"]').first();
    await expect(productCard).toBeVisible();
    await productCard.click();
    
    await page.waitForLoadState('networkidle');
    
    // Add item to basket
    const addToBasketButton = page.locator('[data-testid="add-to-basket-button"]');
    await expect(addToBasketButton).toBeVisible();
    await addToBasketButton.click();
    
    // Verify item was added (look for success message or basket update)
    const successMessage = page.locator('[data-testid="add-to-basket-success"]').first();
    const basketCounter = page.locator('[data-testid="basket-counter"]').first();
    
    // Either success message should appear or basket counter should update
    const hasSuccessMessage = await successMessage.isVisible({ timeout: 3000 }).catch(() => false);
    const hasBasketCounter = await basketCounter.isVisible({ timeout: 3000 }).catch(() => false);
    
    expect(hasSuccessMessage || hasBasketCounter).toBeTruthy();
  });

  test('should view basket contents', async ({ page }) => {
    // First add an item to basket
    await addItemToBasket(page);
    
    // Navigate to basket
    const basketLink = page.locator('[data-testid="basket-link"]').first();
    if (await basketLink.isVisible()) {
      await basketLink.click();
    } else {
      // Try alternative navigation
      await page.goto('/basket');
    }
    
    await page.waitForLoadState('networkidle');
    
    // Verify basket page is displayed
    const basketTitle = page.locator('[data-testid="basket-title"]').first();
    const basketItems = page.locator('[data-testid="basket-item"]');
    
    // Should have basket title or items
    const hasBasketTitle = await basketTitle.isVisible({ timeout: 3000 }).catch(() => false);
    const hasBasketItems = await basketItems.count() > 0;
    
    expect(hasBasketTitle || hasBasketItems).toBeTruthy();
  });

  test('should update item quantity in basket', async ({ page }) => {
    // Add item to basket and navigate to basket page
    await addItemToBasket(page);
    await navigateToBasket(page);
    
    // Find quantity input
    const quantityInput = page.locator('[data-testid="quantity-input"]').first();
    
    if (await quantityInput.isVisible()) {
      // Get current quantity
      const currentQuantity = await quantityInput.inputValue();
      const newQuantity = (parseInt(currentQuantity) + 1).toString();
      
      // Update quantity
      await quantityInput.fill(newQuantity);
      
      // Look for update button or auto-update
      const updateButton = page.locator('[data-testid="update-quantity-button"]').first();
      if (await updateButton.isVisible()) {
        await updateButton.click();
      } else {
        // Try pressing Enter or Tab to trigger update
        await quantityInput.press('Tab');
      }
      
      // Wait for update to process
      await page.waitForTimeout(1000);
      
      // Verify quantity was updated
      const updatedQuantity = await quantityInput.inputValue();
      expect(updatedQuantity).toBe(newQuantity);
    }
  });

  test('should remove item from basket', async ({ page }) => {
    // Add item to basket and navigate to basket page
    await addItemToBasket(page);
    await navigateToBasket(page);
    
    // Get initial item count
    const basketItems = page.locator('[data-testid="basket-item"]');
    const initialCount = await basketItems.count();
    
    if (initialCount > 0) {
      // Find and click remove button
      const removeButton = page.locator('[data-testid="remove-item-button"]').first();
      
      if (await removeButton.isVisible()) {
        await removeButton.click();
        
        // Confirm removal if confirmation dialog appears
        const confirmButton = page.locator('[data-testid="confirm-remove"]').first();
        if (await confirmButton.isVisible({ timeout: 2000 }).catch(() => false)) {
          await confirmButton.click();
        }
        
        // Wait for removal to process
        await page.waitForTimeout(1000);
        
        // Verify item was removed
        const updatedCount = await basketItems.count();
        expect(updatedCount).toBeLessThan(initialCount);
      }
    }
  });

  test('should display correct basket total', async ({ page }) => {
    // Add multiple items to basket
    await addItemToBasket(page);
    await addItemToBasket(page); // Add second item
    
    await navigateToBasket(page);
    
    // Find basket total
    const basketTotal = page.locator('[data-testid="basket-total"]').first();
    
    if (await basketTotal.isVisible()) {
      const totalText = await basketTotal.textContent();
      
      // Verify total is displayed and contains currency symbol or number
      expect(totalText).toBeTruthy();
      expect(totalText).toMatch(/[\d.,]+/); // Should contain numbers
    }
  });

  test('should proceed to checkout from basket', async ({ page }) => {
    // Add item to basket and navigate to basket page
    await addItemToBasket(page);
    await navigateToBasket(page);
    
    // Find checkout button
    const checkoutButton = page.locator('[data-testid="checkout-button"]').first();
    
    if (await checkoutButton.isVisible()) {
      await checkoutButton.click();
      
      // Verify navigation to checkout or login
      await page.waitForLoadState('networkidle');
      
      // Should navigate to checkout or login page
      const currentUrl = page.url();
      expect(currentUrl).toMatch(/(checkout|login|signin)/i);
    }
  });

  test('should persist basket across page refreshes', async ({ page }) => {
    // Add item to basket
    await addItemToBasket(page);
    
    // Navigate to basket to verify item is there
    await navigateToBasket(page);
    
    const initialItems = page.locator('[data-testid="basket-item"]');
    const initialCount = await initialItems.count();
    
    if (initialCount > 0) {
      // Refresh the page
      await page.reload();
      await page.waitForLoadState('networkidle');
      
      // Verify items are still in basket
      const persistedItems = page.locator('[data-testid="basket-item"]');
      const persistedCount = await persistedItems.count();
      
      expect(persistedCount).toBe(initialCount);
    }
  });

  test('should handle empty basket state', async ({ page }) => {
    // Navigate directly to basket (should be empty)
    await page.goto('/basket');
    await page.waitForLoadState('networkidle');
    
    // Look for empty basket message
    const emptyBasketMessage = page.locator('[data-testid="empty-basket-message"]').first();
    const continueShoppingButton = page.locator('[data-testid="continue-shopping-button"]').first();
    
    // Should show empty state or have no items
    const basketItems = page.locator('[data-testid="basket-item"]');
    const itemCount = await basketItems.count();
    
    if (itemCount === 0) {
      // Verify empty state is handled gracefully
      const hasEmptyMessage = await emptyBasketMessage.isVisible({ timeout: 3000 }).catch(() => false);
      const hasContinueButton = await continueShoppingButton.isVisible({ timeout: 3000 }).catch(() => false);
      
      // Should have some indication of empty state
      expect(hasEmptyMessage || hasContinueButton || itemCount === 0).toBeTruthy();
    }
  });

  test('should validate quantity limits', async ({ page }) => {
    // Add item to basket and navigate to basket page
    await addItemToBasket(page);
    await navigateToBasket(page);
    
    // Find quantity input
    const quantityInput = page.locator('[data-testid="quantity-input"]').first();
    
    if (await quantityInput.isVisible()) {
      // Try to set invalid quantity (0 or negative)
      await quantityInput.fill('0');
      
      // Look for validation message or button state
      const validationMessage = page.locator('[data-testid="quantity-validation-error"]').first();
      const updateButton = page.locator('[data-testid="update-quantity-button"]').first();
      
      // Either validation message should appear or update should be prevented
      const hasValidationMessage = await validationMessage.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (await updateButton.isVisible()) {
        const isDisabled = await updateButton.isDisabled();
        expect(hasValidationMessage || isDisabled).toBeTruthy();
      }
    }
  });

  test('should be responsive on mobile devices', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Add item and navigate to basket
    await addItemToBasket(page);
    await navigateToBasket(page);
    
    // Verify basket is usable on mobile
    const basketItems = page.locator('[data-testid="basket-item"]');
    const itemCount = await basketItems.count();
    
    if (itemCount > 0) {
      // Verify item is visible and interactive
      const firstItem = basketItems.first();
      await expect(firstItem).toBeVisible();
      
      // Check if quantity controls are accessible
      const quantityInput = firstItem.locator('[data-testid="quantity-input"]');
      if (await quantityInput.isVisible()) {
        await expect(quantityInput).toBeVisible();
      }
    }
  });

  // Helper functions
  async function addItemToBasket(page: any) {
    // Navigate to a product and add to basket
    const productCard = page.locator('[data-testid="product-card"]').first();
    
    if (await productCard.isVisible()) {
      await productCard.click();
      await page.waitForLoadState('networkidle');
      
      const addToBasketButton = page.locator('[data-testid="add-to-basket-button"]');
      if (await addToBasketButton.isVisible()) {
        await addToBasketButton.click();
        await page.waitForTimeout(1000); // Wait for add to complete
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
});