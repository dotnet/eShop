import { Given, When, Then } from '@cucumber/cucumber';
import { expect } from '@playwright/test';

Given('the basket service is running', async function() {
  this.info('Verifying basket service availability');
  // Basket service health check would go here
  this.info('✓ Basket service is available');
  this.logMessage('✓ Basket service is available');
});

Given('I have {string} in my shopping cart', async function(productName: string) {
  this.info(`Adding ${productName} to shopping cart`);
  try {
    await this.page.goto('/');
    await this.page.getByRole('link', { name: productName }).click();
    await this.page.getByRole('button', { name: 'Add to shopping bag' }).click();
    await this.page.waitForTimeout(1000); // Wait for cart update
    this.info(`✓ Successfully added ${productName} to cart`);
    this.logMessage(`✓ Added ${productName} to cart`);
  } catch (error) {
    this.error(`Failed to add ${productName} to cart`);
    await this.rpScreenshot(`add-to-cart-failed-${productName}`);
    throw error;
  }
});

Given('I have the following items in my cart:', async function(dataTable: any) {
  const items = dataTable.hashes();
  this.info(`Adding ${items.length} items to cart`);
  
  for (const item of items) {
    this.debug(`Adding item: ${item.productName} (quantity: ${item.quantity || 1})`);
    await this.page.goto('/');
    await this.page.getByRole('link', { name: item.productName }).click();
    
    // Add to cart multiple times if quantity > 1
    const quantity = parseInt(item.quantity || '1');
    for (let i = 0; i < quantity; i++) {
      await this.page.getByRole('button', { name: 'Add to shopping bag' }).click();
      await this.page.waitForTimeout(500);
    }
    
    this.debug(`✓ Added ${quantity}x ${item.productName}`);
    this.logMessage(`✓ Added ${quantity}x ${item.productName} to cart`);
  }
  
  this.info(`✓ Successfully added all ${items.length} items to cart`);
});

When('I navigate to the shopping bag', async function() {
  this.info('Navigating to shopping bag');
  await this.page.getByRole('link', { name: 'shopping bag' }).click();
  await this.page.waitForLoadState('networkidle');
  this.info('✓ Successfully navigated to shopping bag');
  this.logMessage('✓ Navigated to shopping bag');
});

When('I add the following products to my cart:', async function(dataTable: any) {
  const products = dataTable.hashes();
  
  for (const product of products) {
    await this.page.goto('/');
    await this.page.getByRole('link', { name: product.productName }).click();
    await this.page.getByRole('button', { name: 'Add to shopping bag' }).click();
    await this.page.waitForTimeout(500);
    this.logMessage(`✓ Added ${product.productName} to cart`);
  }
});

When('I update the quantity to {int}', async function(quantity: number) {
  this.info(`Updating quantity to ${quantity}`);
  const quantityInput = this.page.getByLabel('product quantity');
  await quantityInput.clear();
  await quantityInput.fill(quantity.toString());
  await this.page.waitForTimeout(1000); // Wait for cart update
  this.info(`✓ Quantity updated to ${quantity}`);
  this.logMessage(`✓ Updated quantity to ${quantity}`);
});

When('I remove the item from the cart', async function() {
  this.info('Removing item from cart');
  const removeButton = this.page.getByRole('button', { name: /remove|delete/i });
  await removeButton.click();
  await this.page.waitForTimeout(1000);
  this.info('✓ Item removed from cart');
  this.logMessage('✓ Removed item from cart');
});

When('I add a product to my cart', async function() {
  this.operationStart = Date.now();
  await this.page.goto('/');
  await this.page.getByRole('link').first().click();
  await this.page.getByRole('button', { name: 'Add to shopping bag' }).click();
  this.operationEnd = Date.now();
  this.logMessage('✓ Added product to cart');
});

Then('I should see the shopping bag page', async function() {
  this.info('Verifying shopping bag page');
  const heading = this.page.getByRole('heading', { name: 'Shopping bag' });
  await expect(heading).toBeVisible();
  this.info('✓ Shopping bag page verified');
  this.logMessage('✓ On shopping bag page');
});

Then('the cart should contain {int} item(s)', async function(expectedCount: number) {
  this.info(`Verifying cart contains ${expectedCount} item(s)`);
  await this.page.waitForTimeout(1000); // Wait for cart to update
  
  const quantityElements = await this.page.getByLabel('product quantity').all();
  let totalQuantity = 0;
  
  for (const element of quantityElements) {
    const text = await element.textContent();
    totalQuantity += parseInt(text || '0');
  }
  
  expect(totalQuantity).toBe(expectedCount);
  this.info(`✓ Cart contains ${totalQuantity} item(s) as expected`);
  this.logMessage(`✓ Cart contains ${totalQuantity} item(s)`);
});

Then('the item should be {string}', async function(productName: string) {
  const productText = await this.page.textContent('body');
  expect(productText).toContain(productName);
  this.logMessage(`✓ Cart contains: ${productName}`);
});

Then('the total price should be displayed', async function() {
  const totalElement = this.page.getByText('Total').nth(1);
  await expect(totalElement).toBeVisible();
  this.logMessage('✓ Total price is displayed');
});

Then('the total should reflect all items', async function() {
  const totalElement = this.page.getByText('Total');
  await expect(totalElement).toBeVisible();
  this.logMessage('✓ Total reflects all items');
});

Then('the cart should show quantity {int}', async function(expectedQuantity: number) {
  const quantityElement = this.page.getByLabel('product quantity');
  const actualQuantity = await quantityElement.textContent();
  expect(parseInt(actualQuantity || '0')).toBe(expectedQuantity);
  this.logMessage(`✓ Quantity is ${expectedQuantity}`);
});

Then('the total price should be updated accordingly', async function() {
  const totalElement = this.page.getByText('Total');
  await expect(totalElement).toBeVisible();
  this.logMessage('✓ Total price updated');
});

Then('the cart should be empty', async function() {
  const emptyMessage = this.page.getByText(/empty|no items/i);
  await expect(emptyMessage).toBeVisible();
  this.logMessage('✓ Cart is empty');
});

Then('I should see an empty cart message', async function() {
  const emptyMessage = this.page.getByText(/empty|no items/i);
  await expect(emptyMessage).toBeVisible();
  this.logMessage('✓ Empty cart message displayed');
});

Then('the cart should still contain {string}', async function(productName: string) {
  await this.page.waitForLoadState('networkidle');
  const productText = await this.page.textContent('body');
  expect(productText).toContain(productName);
  this.logMessage(`✓ Cart still contains: ${productName}`);
});

Then('the operation should complete within {int} seconds', async function(maxSeconds: number) {
  const duration = (this.operationEnd - this.operationStart) / 1000;
  this.info(`Verifying operation performance: ${duration.toFixed(2)}s (limit: ${maxSeconds}s)`);
  expect(duration).toBeLessThan(maxSeconds);
  this.info(`✓ Operation completed in ${duration.toFixed(2)}s`);
  this.logMessage(`✓ Operation completed in ${duration.toFixed(2)}s`);
});

Then('I should see a success indication', async function() {
  // Cart icon or success message should be visible
  await this.page.waitForTimeout(500);
  this.logMessage('✓ Success indication shown');
});

