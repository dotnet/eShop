import { test, expect, Page } from '@playwright/test';
import { loadScenario } from './scenario';

// Seed data: testing/scenarios/catalog-edge. 14 items sorted by name (Edge 01..14), 9 per page.
// Brands: 1 Alpha (1,2,3,4,14), 2 Beta (5,6,7,8), 3 Gamma (9-13), 4 Unused. Types: 1 Boots, 2 Packs, 3 Tents, 4 Unused.
// Flow map: testing/flows/catalog.md.
const names = (page: Page) => page.locator('.catalog-product .name');
const tag = (page: Page, label: string) => page.locator('.catalog-search-tag', { hasText: label });
const count = (page: Page) => page.locator('.catalog-count');
const notFoundText = "Sorry, we couldn't find what you were looking for.";

test.describe.configure({ mode: 'default' });

test.describe('catalog browsing (UI)', () => {
  test.beforeAll(() => {
    loadScenario('catalog-edge');
  });

  test('home page lists products with name, price, brand chip and a loaded image', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle('AdventureWorks');
    await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
    await expect(count(page)).toHaveText('14 products');

    const card = page.locator('.catalog-product', { hasText: 'Edge 05 Expedition Pack' });
    await expect(card.locator('.catalog-product-brand')).toHaveText('Edge Beta');
    await expect(card.locator('.price')).toHaveText('$9,999.99');
    await expect(card).toHaveAttribute('href', 'item/5');

    const images = page.locator('.catalog-product img');
    await expect(images).toHaveCount(9);
    for (const img of await images.all()) {
      await expect(img).toHaveAttribute('src', /^product-images\/\d+\?api-version=2\.0$/);
      await expect.poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth)).toBeGreaterThan(0);
    }
  });

  test('clicking brand tags toggles them and combines brands additively', async ({ page }) => {
    await page.goto('/');
    await tag(page, 'Edge Beta').click();
    await expect(page).toHaveURL(/\?brand=2$/);
    await expect(count(page)).toHaveText('4 products');
    await expect(tag(page, 'Edge Beta')).toHaveClass(/active/);
    await expect(page.locator('.catalog-search-title-count')).toHaveText('1');

    await tag(page, 'Edge Gamma').click();
    await expect(page).toHaveURL(/\?brand=2&brand=3$/);
    await expect(count(page)).toHaveText('9 products');
    await expect(page.locator('.catalog-search-title-count')).toHaveText('2');

    // Clicking an active brand removes it from the selection.
    await tag(page, 'Edge Beta').click();
    await expect(page).toHaveURL(/\?brand=3$/);
    await expect(count(page)).toHaveText('5 products');
    await expect(tag(page, 'Edge Beta')).not.toHaveClass(/active/);
  });

  test('brand and type tags intersect, and "All" or "Clear all filters" reset them', async ({ page }) => {
    await page.goto('/');
    await tag(page, 'Edge Alpha').click();
    await expect(count(page)).toHaveText('5 products');
    await tag(page, 'Edge Boots').click();
    await expect(page).toHaveURL(/brand=1/);
    await expect(page).toHaveURL(/type=1/);
    await expect(count(page)).toHaveText('3 products');
    await expect(names(page)).toHaveText(['Edge 01 Penny Boot', 'Edge 02 Trail Boot', 'Edge 03 Summit Boot']);

    // "All" in the Brand group clears only the brand facet.
    await page.locator('.catalog-search-group', { hasText: 'Brand' }).locator('.catalog-search-tag', { hasText: 'All' }).click();
    await expect(page).not.toHaveURL(/brand=/);
    await expect(page).toHaveURL(/type=1/);
    await expect(count(page)).toHaveText('4 products');

    await page.locator('.catalog-search-clear').click();
    await expect(page).not.toHaveURL(/type=/);
    await expect(count(page)).toHaveText('14 products');
    await expect(page.locator('.catalog-search-clear')).toHaveCount(0);
  });

  test('changing a filter resets the page number', async ({ page }) => {
    await page.goto('/?page=2');
    await expect(names(page).first()).toHaveText('Edge 10 Bivy');
    await tag(page, 'Edge Gamma').click();
    await expect(page).toHaveURL(/\?brand=3$/);
    await expect(names(page).first()).toHaveText('Edge 09 Tunnel Tent');
  });

  test('sorting by price reorders the current page and persists across visits', async ({ page }) => {
    await page.goto('/');
    await expect(names(page).first()).toHaveText('Edge 01 Penny Boot');
    await page.locator('#catalog-sort-trigger').click();
    await page.getByRole('option', { name: 'Price: High to Low' }).click();
    await expect(page.locator('.catalog-sort-value')).toHaveText('Price: High to Low');
    await expect(names(page).first()).toHaveText('Edge 05 Expedition Pack');
    await expect(names(page).last()).toHaveText('Edge 01 Penny Boot');

    // Sorting is client-side only: page 2 still holds the items 10..14 by name, now sorted by price.
    await page.goto('/?page=2');
    await expect(names(page)).toHaveText([
      'Edge 12 Mega Pack',
      'Edge 10 Bivy',
      'Edge 11 Hiking Boot',
      'Edge 13 Ultra Long Name Hiking Pack Edition Max 50',
      'Edge 14 Last Item',
    ]);

    await page.locator('#catalog-sort-trigger').click();
    await page.getByRole('option', { name: 'Name: Z to A' }).click();
    await expect(names(page).first()).toHaveText('Edge 14 Last Item');
  });

  test('opening a product from a filtered list and going back keeps the filter', async ({ page }) => {
    await page.goto('/?brand=2');
    await page.getByRole('link', { name: /Edge 07 Dome Tent/ }).click();
    await expect(page).toHaveURL(/\/item\/7$/);
    await expect(page).toHaveTitle('Edge 07 Dome Tent | AdventureWorks');
    await expect(page.locator('.item-desc')).toHaveText('Plain dome tent.');
    await expect(page.locator('.item-back')).toHaveAttribute('href', '/?brand=2');

    await page.locator('.item-back').click();
    await expect(page).toHaveURL(/\/\?brand=2$/);
    await expect(count(page)).toHaveText('4 products');
  });

  test('a deep link to a product goes back to the unfiltered catalog', async ({ page }) => {
    await page.goto('/item/7');
    await expect(page.locator('.item-back')).toHaveAttribute('href', '');
    await page.locator('.item-back').click();
    await expect(count(page)).toHaveText('14 products');
  });

  test('the product image opens in a zoom dialog', async ({ page }) => {
    await page.goto('/item/2');
    const dialog = page.getByRole('dialog', { name: 'Enlarged image of Edge 02 Trail Boot' });
    await expect(dialog).toBeHidden();
    await page.getByRole('button', { name: 'Zoom image of Edge 02 Trail Boot' }).click();
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Close image' }).click();
    await expect(dialog).toBeHidden();
  });

  test('anonymous "Log in to purchase" sends the visitor to the login page', async ({ page }) => {
    await page.goto('/item/2');
    await page.getByRole('button', { name: 'Log in to purchase' }).click();
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  });

  for (const path of ['/item/0', '/item/-1', '/item/abc', '/item/2147483648', '/item/1/extra']) {
    test(`invalid product URL ${path} returns the not-found page`, async ({ page }) => {
      const response = await page.goto(path);
      expect(response?.status()).toBe(404);
      await expect(page.getByText(notFoundText)).toBeVisible();
    });
  }

  test('unknown routes return the not-found page with a link back to the catalog', async ({ page }) => {
    const response = await page.goto('/no-such-page');
    expect(response?.status()).toBe(404);
    await expect(page.getByText(notFoundText)).toBeVisible();
    await page.getByRole('link', { name: 'Back to catalog' }).click();
    await expect(count(page)).toHaveText('14 products');
  });

  for (const query of ['page=abc', 'brand=abc', 'type=1.5']) {
    test(`non-numeric query ?${query} returns the not-found page`, async ({ page }) => {
      const response = await page.goto(`/?${query}`);
      expect(response?.status()).toBe(404);
      await expect(page.getByText(notFoundText)).toBeVisible();
    });
  }

  test('unknown brand and type ids show the empty state', async ({ page }) => {
    for (const query of ['brand=999', 'type=999', 'brand=-1']) {
      const response = await page.goto(`/?${query}`);
      expect(response?.status()).toBe(200);
      await expect(page.getByRole('heading', { name: 'No products match these filters' })).toBeVisible();
      await expect(page.locator('.catalog-product')).toHaveCount(0);
    }
  });

  test('a page beyond the last one lists no products', async ({ page }) => {
    await page.goto('/?page=99');
    await expect(count(page)).toHaveText('14 products');
    await expect(page.locator('.catalog-product')).toHaveCount(0);
  });

  // BUG: page=0 and negative pages are passed to Catalog.API as pageIndex -1 / -2, which answers 500
  // ("OFFSET must not be negative"); the catalog then never leaves the loading skeleton and Blazor
  // shows "An unhandled error has occurred." The page should clamp to page 1 (or return 404).
  for (const pageParam of ['0', '-1']) {
    test.fixme(`?page=${pageParam} shows the first page`, async ({ page }) => {
      await page.goto(`/?page=${pageParam}`);
      await expect(count(page)).toHaveText('14 products');
      await expect(names(page).first()).toHaveText('Edge 01 Penny Boot');
    });
  }

  test('the product-images proxy serves pictures and 404s for unknown items', async ({ request }) => {
    const ok = await request.get('/product-images/1?api-version=2.0');
    expect(ok.status()).toBe(200);
    expect(ok.headers()['content-type']).toBe('image/webp');
    expect((await ok.body()).length).toBeGreaterThan(1000);

    expect((await request.get('/product-images/9999?api-version=2.0')).status()).toBe(404);
    expect((await request.get('/product-images/abc?api-version=2.0')).status()).toBe(404);
    // The proxy forwards the query unchanged, so the API version is still required.
    expect((await request.get('/product-images/1')).status()).toBe(400);
  });
});
