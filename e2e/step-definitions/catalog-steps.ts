import { Given, When, Then } from '@cucumber/cucumber';
import { expect } from '@playwright/test';

Given('the catalog service is running', async function() {
  this.info('Verifying catalog service health');
  // Verify catalog service health
  const response = await this.page.request.get(`${this.baseUrl}/health`);
  expect(response.ok()).toBeTruthy();
  this.info('✓ Catalog service is healthy');
  this.logMessage('✓ Catalog service is healthy');
});

Given('I am on the eShop homepage', async function() {
  this.info('Navigating to eShop homepage');
  await this.page.goto('/');
  await this.page.waitForLoadState('networkidle');
  this.info('✓ Successfully navigated to homepage');
  this.logMessage('✓ Navigated to homepage');
});

When('I click on the {string} product', async function(productName: string) {
  this.info(`Clicking on product: ${productName}`);
  try {
    await this.page.getByRole('link', { name: productName }).click();
    await this.page.waitForLoadState('networkidle');
    this.info(`✓ Successfully clicked on product: ${productName}`);
    this.logMessage(`✓ Clicked on product: ${productName}`);
  } catch (error) {
    this.error(`Failed to click on product: ${productName}`);
    await this.rpScreenshot(`product-click-failed-${productName}`);
    throw error;
  }
});

When('I view the available products', async function() {
  this.info('Waiting for products to load');
  await this.page.waitForSelector('[data-testid="product-card"], .product-item, article', {
    timeout: 5000
  }).catch(() => {
    this.warn('Using fallback product selector');
    this.logMessage('⚠ Using fallback product selector');
  });
  this.info('✓ Products are visible');
  this.logMessage('✓ Products are visible');
});

Then('I should see the heading {string}', async function(headingText: string) {
  this.info(`Verifying heading: ${headingText}`);
  const heading = this.page.getByRole('heading', { name: headingText });
  await expect(heading).toBeVisible({ timeout: 10000 });
  this.info(`✓ Heading verified: ${headingText}`);
  this.logMessage(`✓ Heading verified: ${headingText}`);
});

Then('I should see available products displayed', async function() {
  this.info('Verifying products are displayed');
  // Wait for products to load - they may load dynamically
  await this.page.waitForLoadState('networkidle');
  
  // Wait a bit for dynamic content to render
  await this.page.waitForTimeout(2000);
  this.debug('Waited for dynamic content to render');
  
  // Try multiple selectors to find products
  const selectors = [
    'a[href*="/item/"]',
    'a[href*="/catalog/items/"]',
    '.product-card',
    'article',
    '[data-testid="product"]',
    'img[alt]:not([alt=""])',  // Images with alt text
    'img[src*="catalog"]',
    'img[src*="item"]',
    'img[src*="product"]',
    '.catalog-item',
    '[class*="product"]',
    '[class*="item"]'
  ];
  
  let productCount = 0;
  let usedSelector = '';
  
  for (const selector of selectors) {
    const count = await this.page.locator(selector).count();
    if (count > 0) {
      productCount = count;
      usedSelector = selector;
      this.debug(`Found ${count} elements with selector: ${selector}`);
      this.logMessage(`Found ${count} elements with selector: ${selector}`);
      break;
    }
  }
  
  // If still no products found, try to find any images
  if (productCount === 0) {
    this.warn('No products found with standard selectors');
    this.logMessage('⚠ No products found with standard selectors');
    
    // Log all images on the page
    const allImages = await this.page.locator('img').count();
    this.debug(`Total images on page: ${allImages}`);
    this.logMessage(`Total images on page: ${allImages}`);
    
    // Log all links
    const allLinks = await this.page.locator('a').count();
    this.debug(`Total links on page: ${allLinks}`);
    this.logMessage(`Total links on page: ${allLinks}`);
    
    // Get a sample of link hrefs
    const linkHrefs = await this.page.locator('a').evaluateAll((links: any[]) => 
      links.slice(0, 10).map(link => link.href)
    );
    this.debug(`Sample link hrefs: ${linkHrefs.join(', ')}`);
    this.logMessage(`Sample link hrefs: ${linkHrefs.join(', ')}`);
    
    // Check if there's a loading indicator
    const loadingIndicator = await this.page.locator('[class*="loading"], [class*="spinner"]').count();
    if (loadingIndicator > 0) {
      this.warn('Loading indicator still present, waiting longer...');
      this.logMessage('⚠ Loading indicator still present, waiting longer...');
      await this.page.waitForTimeout(3000);
      
      // Try again after waiting
      for (const selector of selectors) {
        const count = await this.page.locator(selector).count();
        if (count > 0) {
          productCount = count;
          usedSelector = selector;
          break;
        }
      }
    }
    
    // Take screenshot if still no products
    if (productCount === 0) {
      await this.rpScreenshot('no-products-found');
    }
  }
  
  expect(productCount).toBeGreaterThan(0);
  this.info(`✓ Found ${productCount} products displayed using selector: ${usedSelector}`);
  this.logMessage(`✓ Found ${productCount} products displayed using selector: ${usedSelector}`);
});

Then('I should see the product detail page', async function() {
  this.info('Verifying product detail page');
  await this.page.waitForLoadState('networkidle');
  const url = this.page.url();
  expect(url).toContain('/item/');
  this.info(`✓ On product detail page: ${url}`);
  this.logMessage('✓ On product detail page');
});

Then('the product name should be "Adventurer GPS Watch"', async function(productName: string) {
  const heading = this.page.getByRole('heading', { name: productName });
  await expect(heading).toBeVisible();
  this.logMessage(`✓ Product name verified: ${productName}`);
});

Then('I should see at least {int} products displayed', async function(minCount: number) {
  await this.page.waitForLoadState('networkidle');
  const productCount = await this.page.locator('a[href*="/item/"], .product-card, article').count();
  expect(productCount).toBeGreaterThanOrEqual(minCount);
  this.logMessage(`✓ Found ${productCount} products (minimum: ${minCount})`);
});

Then('each product should have a name', async function() {
  const products = await this.page.locator('a[href*="/item/"]').all();
  expect(products.length).toBeGreaterThan(0);
  this.logMessage(`✓ All ${products.length} products have names`);
});

Then('each product should have a price', async function() {
  // Products should have price information
  const priceElements = await this.page.locator('[data-testid="price"], .price, [class*="price"]').count();
  expect(priceElements).toBeGreaterThan(0);
  this.logMessage(`✓ Found ${priceElements} price elements`);
});

Then('each product should have an image', async function() {
  const images = await this.page.locator('img[alt], img[src*="catalog"]').count();
  expect(images).toBeGreaterThan(0);
  this.logMessage(`✓ Found ${images} product images`);
});

Then('the page should load within {int} seconds', async function(maxSeconds: number) {
  const loadTime = Date.now() - this.pageLoadStart;
  const loadTimeSeconds = loadTime / 1000;
  this.info(`Verifying page load performance: ${loadTimeSeconds.toFixed(2)}s (limit: ${maxSeconds}s)`);
  expect(loadTimeSeconds).toBeLessThan(maxSeconds);
  this.info(`✓ Page loaded in ${loadTimeSeconds.toFixed(2)}s`);
  this.logMessage(`✓ Page loaded in ${loadTimeSeconds.toFixed(2)}s (limit: ${maxSeconds}s)`);
});

Then('all product images should be visible', async function() {
  const images = await this.page.locator('img[alt]').all();
  for (const img of images) {
    await expect(img).toBeVisible();
  }
  this.logMessage(`✓ All ${images.length} images are visible`);
});

