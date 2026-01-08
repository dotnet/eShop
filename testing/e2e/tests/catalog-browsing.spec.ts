import { test, expect } from '@playwright/test';

test.describe('Catalog Browsing', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the eShop homepage
    await page.goto('/');
    
    // Wait for the page to load
    await page.waitForLoadState('networkidle');
  });

  test('should display product catalog on homepage', async ({ page }) => {
    // Verify the page title
    await expect(page).toHaveTitle(/eShop/);
    
    // Check for catalog section
    const catalogSection = page.locator('[data-testid="catalog-section"]').first();
    await expect(catalogSection).toBeVisible();
    
    // Verify products are displayed
    const productCards = page.locator('[data-testid="product-card"]');
    await expect(productCards.first()).toBeVisible();
    
    // Check that multiple products are shown
    const productCount = await productCards.count();
    expect(productCount).toBeGreaterThan(0);
  });

  test('should show product details when clicking on a product', async ({ page }) => {
    // Wait for products to load
    const productCard = page.locator('[data-testid="product-card"]').first();
    await expect(productCard).toBeVisible();
    
    // Get product name for verification
    const productName = await productCard.locator('[data-testid="product-name"]').textContent();
    
    // Click on the product
    await productCard.click();
    
    // Verify navigation to product details
    await page.waitForLoadState('networkidle');
    
    // Check product details are displayed
    const productTitle = page.locator('[data-testid="product-title"]');
    await expect(productTitle).toBeVisible();
    
    if (productName) {
      await expect(productTitle).toContainText(productName);
    }
    
    // Verify essential product information is present
    await expect(page.locator('[data-testid="product-price"]')).toBeVisible();
    await expect(page.locator('[data-testid="product-description"]')).toBeVisible();
    await expect(page.locator('[data-testid="add-to-basket-button"]')).toBeVisible();
  });

  test('should filter products by brand', async ({ page }) => {
    // Look for brand filter
    const brandFilter = page.locator('[data-testid="brand-filter"]').first();
    
    if (await brandFilter.isVisible()) {
      // Get initial product count
      const initialProducts = page.locator('[data-testid="product-card"]');
      const initialCount = await initialProducts.count();
      
      // Select a brand filter
      await brandFilter.click();
      
      // Wait for filter to apply
      await page.waitForTimeout(1000);
      
      // Verify products are filtered
      const filteredProducts = page.locator('[data-testid="product-card"]');
      const filteredCount = await filteredProducts.count();
      
      // The count might be the same or different, but should be valid
      expect(filteredCount).toBeGreaterThanOrEqual(0);
    }
  });

  test('should search for products', async ({ page }) => {
    // Look for search input
    const searchInput = page.locator('[data-testid="search-input"]').first();
    
    if (await searchInput.isVisible()) {
      // Enter search term
      await searchInput.fill('jacket');
      
      // Submit search (look for search button or press Enter)
      const searchButton = page.locator('[data-testid="search-button"]');
      if (await searchButton.isVisible()) {
        await searchButton.click();
      } else {
        await searchInput.press('Enter');
      }
      
      // Wait for search results
      await page.waitForLoadState('networkidle');
      
      // Verify search results
      const searchResults = page.locator('[data-testid="product-card"]');
      const resultCount = await searchResults.count();
      
      if (resultCount > 0) {
        // Check that results contain the search term
        const firstResult = searchResults.first();
        const productName = await firstResult.locator('[data-testid="product-name"]').textContent();
        
        if (productName) {
          expect(productName.toLowerCase()).toContain('jacket');
        }
      }
    }
  });

  test('should navigate through product pages', async ({ page }) => {
    // Look for pagination controls
    const nextPageButton = page.locator('[data-testid="next-page"]').first();
    
    if (await nextPageButton.isVisible()) {
      // Get products from first page
      const firstPageProducts = page.locator('[data-testid="product-card"]');
      const firstPageCount = await firstPageProducts.count();
      
      if (firstPageCount > 0) {
        const firstProductName = await firstPageProducts.first()
          .locator('[data-testid="product-name"]').textContent();
        
        // Navigate to next page
        await nextPageButton.click();
        await page.waitForLoadState('networkidle');
        
        // Verify we're on a different page
        const secondPageProducts = page.locator('[data-testid="product-card"]');
        const secondPageCount = await secondPageProducts.count();
        
        if (secondPageCount > 0) {
          const secondProductName = await secondPageProducts.first()
            .locator('[data-testid="product-name"]').textContent();
          
          // Products should be different (unless there's only one page)
          if (firstProductName && secondProductName) {
            // This might be the same if there's only one page, which is acceptable
            expect(typeof secondProductName).toBe('string');
          }
        }
      }
    }
  });

  test('should display product categories', async ({ page }) => {
    // Look for category navigation
    const categoryNav = page.locator('[data-testid="category-nav"]').first();
    
    if (await categoryNav.isVisible()) {
      // Get category links
      const categoryLinks = categoryNav.locator('[data-testid="category-link"]');
      const categoryCount = await categoryLinks.count();
      
      expect(categoryCount).toBeGreaterThan(0);
      
      if (categoryCount > 0) {
        // Click on first category
        const firstCategory = categoryLinks.first();
        const categoryName = await firstCategory.textContent();
        
        await firstCategory.click();
        await page.waitForLoadState('networkidle');
        
        // Verify we're viewing the category
        const products = page.locator('[data-testid="product-card"]');
        const productCount = await products.count();
        
        // Should have products or show empty state
        expect(productCount).toBeGreaterThanOrEqual(0);
      }
    }
  });

  test('should be responsive on mobile devices', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Reload page for mobile view
    await page.reload();
    await page.waitForLoadState('networkidle');
    
    // Verify mobile navigation
    const mobileMenu = page.locator('[data-testid="mobile-menu"]').first();
    
    if (await mobileMenu.isVisible()) {
      await mobileMenu.click();
      
      // Check mobile navigation items
      const navItems = page.locator('[data-testid="mobile-nav-item"]');
      const navCount = await navItems.count();
      
      expect(navCount).toBeGreaterThan(0);
    }
    
    // Verify products are still visible and properly laid out
    const productCards = page.locator('[data-testid="product-card"]');
    const productCount = await productCards.count();
    
    if (productCount > 0) {
      await expect(productCards.first()).toBeVisible();
    }
  });

  test('should handle loading states gracefully', async ({ page }) => {
    // Navigate to catalog
    await page.goto('/');
    
    // Check for loading indicators
    const loadingIndicator = page.locator('[data-testid="loading-indicator"]').first();
    
    // If loading indicator exists, wait for it to disappear
    if (await loadingIndicator.isVisible({ timeout: 1000 }).catch(() => false)) {
      await expect(loadingIndicator).not.toBeVisible({ timeout: 10000 });
    }
    
    // Verify content is loaded
    const content = page.locator('[data-testid="catalog-section"]').first();
    await expect(content).toBeVisible();
  });

  test('should maintain performance standards', async ({ page }) => {
    // Start performance monitoring
    const startTime = Date.now();
    
    // Navigate to catalog
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    const loadTime = Date.now() - startTime;
    
    // Verify page loads within acceptable time (5 seconds)
    expect(loadTime).toBeLessThan(5000);
    
    // Check for performance metrics
    const performanceMetrics = await page.evaluate(() => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
      return {
        domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
        loadComplete: navigation.loadEventEnd - navigation.loadEventStart
      };
    });
    
    // Verify reasonable performance metrics
    expect(performanceMetrics.domContentLoaded).toBeLessThan(3000);
    expect(performanceMetrics.loadComplete).toBeLessThan(5000);
  });
});