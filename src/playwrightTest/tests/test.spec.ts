import { test, expect } from '@playwright/test';


test('Browsing for item', async ({ page }) => {
  await page.goto('https://localhost:19888/');

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/Resources/,  { timeout: 10000 });

  await page.goto('https://localhost:7298/');
  await expect(page).toHaveTitle(/Northern Mountains/, { timeout: 10000 });

  // Find multiple items on the page using CSS selector
  const multipleItems = await page.$$('.catalog-item');
  if (multipleItems.length > 0) {
      console.log('Multiple items found:', multipleItems.length);
  } else {
      console.log('No items found');
  }

  const imageElement = await page.$('img[alt="Adventurer GPS Watch"]');
  expect(imageElement).not.toBeNull(); // Check if the image element is not null
  const isVisible = await imageElement?.isVisible(); // Check if the image element is visible
  expect(isVisible).toBe(true); // Assert that the image element is visible
  console.log('Image with alt text "Adventurer GPS Watch" is present and visible');
});

test('Adding item to cart', async ({ page }) => {
  await page.goto('https://localhost:19888/');

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/Resources/,  { timeout: 10000 });

  await page.goto('https://localhost:7298/');
  await expect(page).toHaveTitle(/Northern Mountains/,  { timeout: 10000 });

  const ariaLabel = 'Sign in';
  await page.click(`[aria-label="${ariaLabel}"]`);

  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();

  // Fill in the login form with credentials
  await page.fill('input[name="Username"]', 'bob');
  await page.fill('input[name="Password"]', 'Pass123$');

  // Submit the login form
  await page.click('button[value="login"]');

  // Wait for navigation after login
  await page.waitForNavigation();

  // Check if login was successful (for example, by verifying the URL or presence of a logout button)
  const isLoggedIn = await page.url() === 'https://localhost:7298/';
  if (isLoggedIn) {
      console.log('Login successful');
  } else {
      console.log('Login failed');
  }
  
   // Click on the first catalog item
   const firstCatalogItem = await page.$('.catalog-item');
   if (firstCatalogItem) {
       await firstCatalogItem.click();
       console.log('Clicked on the first catalog item');
   } else {
       console.log('No catalog items found');
   }

   await expect(page.getByRole('button', { name: 'Add to shopping bag' })).toBeVisible();

   await page.click('button[title="Add to basket"]');
   const pageTextContent = await page.textContent('html');

    // Wait for the text '1 in shopping bag' to appear on the page
    await page.waitForSelector('text=in shopping bag');
        
    // If the text is found, log a message indicating success
    console.log("item added in shopping bag");
});

test('Adding and Removing item from cart', async ({ page }) => {
  await page.goto('https://localhost:19888/');

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/Resources/, { timeout: 10000 });

  await page.goto('https://localhost:7298/');
  await expect(page).toHaveTitle(/Northern Mountains/, { timeout: 10000 });

  const ariaLabel = 'Sign in';
  await page.click(`[aria-label="${ariaLabel}"]`);

  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();

  // Fill in the login form with credentials
  await page.fill('input[name="Username"]', 'bob');
  await page.fill('input[name="Password"]', 'Pass123$');

  // Submit the login form
  await page.click('button[value="login"]');

  // Wait for navigation after login
  await page.waitForNavigation();

  // Check if login was successful (for example, by verifying the URL or presence of a logout button)
  const isLoggedIn = await page.url() === 'https://localhost:7298/';
  if (isLoggedIn) {
      console.log('Login successful');
  } else {
      console.log('Login failed');
  }
  
   // Click on the first catalog item
   const firstCatalogItem = await page.$('.catalog-item');
   if (firstCatalogItem) {
       await firstCatalogItem.click();
       console.log('Clicked on the first catalog item');
   } else {
       console.log('No catalog items found');
   }

   await expect(page.getByRole('button', { name: 'Add to shopping bag' })).toBeVisible();

   await page.click('button[title="Add to basket"]');
   const pageTextContent = await page.textContent('html');

    // Wait for the text '1 in shopping bag' to appear on the page
    await page.waitForSelector('text=in shopping bag');
        
    // If the text is found, log a message indicating success
    console.log("item added in shopping bag");

   await page.goto('https://localhost:7298/cart');

   const a = await page.$eval('input[name="UpdateQuantityValue"]', (input: HTMLInputElement) => input.value);
   console.log('current item in cart : %d', a);
   await page.fill('input[name="UpdateQuantityValue"]', '0');

   const b = await page.$eval('input[name="UpdateQuantityValue"]', (input: HTMLInputElement) => input.value);
   console.log('current item in cart after removing : %d', b);

   // Update the quantity to 0 to remove element from cart
  await page.click('button[name="UpdateQuantityId"]');

  const inputValue = await page.$eval('input[type="number"]', (input: HTMLInputElement) => input.value);

  // Expect the input value to be a certain value
  expect(inputValue).toEqual("0");

});