import { Page } from '@playwright/test';
import { test, expect } from '../fixtures';
import { loadScenario, login } from './scenario';

// Seed data: testing/scenarios/catalog-edge. 14 items sorted by name (Edge 01..14), 9 per page.
// Brands: Alpha 5 (1,2,3,4,14), Beta 4 (5,6,7,8), Gamma 5 (9-13), Unused Brand 0.
// Types: Boots 4 (1,2,3,11), Packs 5 (4,5,6,12,13), Tents 5 (7,8,9,10,14), Unused Type 0.
const LONG_NAME = 'Edge 13 Ultra Long Name Hiking Pack Edition Max 50';

const names = (page: Page) => page.locator('.catalog-product .name');
const tag = (page: Page, label: string) => page.locator('.catalog-search-tag', { hasText: label });

// Tests are independent (only the last one writes state, a basket), so a failure does not skip the rest.
test.describe.configure({ mode: 'default' });

test.describe('catalog edge cases', () => {
  test.beforeAll(() => {
    loadScenario('catalog-edge');
  });

  test('first page shows 9 of 14 products with formatted prices', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.catalog-count')).toHaveText('14 products');
    await expect(page.locator('.catalog-product')).toHaveCount(9);
    await expect(names(page).first()).toHaveText('Edge 01 Penny Boot');
    await expect(names(page).last()).toHaveText('Edge 09 Tunnel Tent');

    const priceOf = (name: string) =>
      page.locator('.catalog-product', { hasText: name }).locator('.price');
    await expect(priceOf('Edge 01 Penny Boot')).toHaveText('$0.01');
    await expect(priceOf('Edge 03 Summit Boot')).toHaveText('$1,234.50');
    await expect(priceOf('Edge 04 Day Pack')).toHaveText('$25.00');
    await expect(priceOf('Edge 05 Expedition Pack')).toHaveText('$9,999.99');
    await expect(priceOf('Edge 06 Zero Stock Pack')).toHaveText('$45.00');
    await expect(priceOf('Edge 09 Tunnel Tent')).toHaveText('$349.95');
  });

  test('pagination has exactly two pages', async ({ page }) => {
    await page.goto('/');
    const pager = page.getByRole('navigation', { name: 'Catalog pages' });
    await expect(pager.locator('.page-status')).toHaveText('1 / 2');
    await expect(pager.locator('.page-number')).toHaveText(['1', '2']);
    await expect(pager.locator('.page-nav.is-disabled')).toHaveText(/Prev/);

    await pager.getByRole('link', { name: '2', exact: true }).click();
    await expect(page).toHaveURL(/\?page=2$/);
    await expect(page.locator('.catalog-product')).toHaveCount(5);
    await expect(names(page)).toHaveText([
      'Edge 10 Bivy',
      'Edge 11 Hiking Boot',
      'Edge 12 Mega Pack',
      LONG_NAME,
      'Edge 14 Last Item',
    ]);
    await expect(pager.locator('.page-status')).toHaveText('2 / 2');
    await expect(pager.locator('.page-nav.is-disabled')).toHaveText(/Next/);
    await expect(page.locator('.catalog-count')).toHaveText('14 products');

    await page.goto('/?page=3');
    await expect(page.locator('.catalog-product')).toHaveCount(0);
  });

  test('brand and type facets show contextual counts', async ({ page }) => {
    await page.goto('/');
    const counts = async (label: string) =>
      (await tag(page, label).locator('.catalog-search-tag-count').textContent())?.trim();

    await expect(tag(page, 'Edge Alpha').locator('.catalog-search-tag-count')).toHaveText('5');
    expect(await counts('Edge Beta')).toBe('4');
    expect(await counts('Edge Gamma')).toBe('5');
    expect(await counts('Edge Unused Brand')).toBe('0');
    expect(await counts('Edge Boots')).toBe('4');
    expect(await counts('Edge Packs')).toBe('5');
    expect(await counts('Edge Tents')).toBe('5');
    expect(await counts('Edge Unused Type')).toBe('0');
    await expect(tag(page, 'Edge Unused Brand')).toHaveClass(/is-empty/);
    await expect(tag(page, 'Edge Unused Type')).toHaveClass(/is-empty/);
    await expect(tag(page, 'Edge Alpha')).not.toHaveClass(/is-empty/);

    // Brand counts are evaluated within the active type: Packs = items 4, 5, 6, 12, 13.
    await page.goto('/?type=2');
    await expect(page.locator('.catalog-count')).toHaveText('5 products');
    await expect(tag(page, 'Edge Alpha').locator('.catalog-search-tag-count')).toHaveText('1');
    expect(await counts('Edge Beta')).toBe('2');
    expect(await counts('Edge Gamma')).toBe('2');
  });

  test('filtering by a brand or type with items leaves one page', async ({ page }) => {
    await page.goto('/?brand=1');
    await expect(page.locator('.catalog-count')).toHaveText('5 products');
    await expect(names(page)).toHaveText([
      'Edge 01 Penny Boot',
      'Edge 02 Trail Boot',
      'Edge 03 Summit Boot',
      'Edge 04 Day Pack',
      'Edge 14 Last Item',
    ]);
    await expect(page.getByRole('navigation', { name: 'Catalog pages' })).toHaveCount(0);

    // Brand and type intersect: Alpha + Packs is only Edge 04.
    await page.goto('/?brand=1&type=2');
    await expect(page.locator('.catalog-count')).toHaveText('1 product');
    await expect(names(page)).toHaveText(['Edge 04 Day Pack']);

    // Selections within a facet are additive: Beta + Gamma = 9 items, still one page.
    await page.goto('/?brand=2&brand=3');
    await expect(page.locator('.catalog-count')).toHaveText('9 products');
    await expect(page.getByRole('navigation', { name: 'Catalog pages' })).toHaveCount(0);
  });

  test('brand without items shows the empty state', async ({ page }) => {
    await page.goto('/');
    await expect(tag(page, 'Edge Unused Brand')).toHaveAttribute('aria-disabled', 'true');
    await page.goto('/?brand=4');
    await expect(page.getByRole('heading', { name: 'No products match these filters' })).toBeVisible();
    await expect(page.locator('.catalog-product')).toHaveCount(0);
    await expect(page.locator('.catalog-count')).toHaveCount(0);
    await expect(page.getByRole('navigation', { name: 'Catalog pages' })).toHaveCount(0);

    await page.getByRole('link', { name: 'Clear all filters' }).first().click();
    await expect(page).not.toHaveURL(/brand=/);
    await expect(page.locator('.catalog-count')).toHaveText('14 products');
  });

  test('type without items shows the empty state', async ({ page }) => {
    await page.goto('/?type=4');
    await expect(page.getByRole('heading', { name: 'No products match these filters' })).toBeVisible();
    await expect(page.locator('.catalog-product')).toHaveCount(0);
  });

  test('filter combination with no match shows the empty state', async ({ page }) => {
    // Beta (items 5-8) has no Boots.
    await page.goto('/?brand=2&type=1');
    await expect(page.getByRole('heading', { name: 'No products match these filters' })).toBeVisible();
    await expect(page.locator('.catalog-product')).toHaveCount(0);
  });

  test('the 50-character name renders in full in the list and on its product page', async ({ page }) => {
    expect(LONG_NAME).toHaveLength(50);
    await page.goto('/?page=2');
    await expect(page.locator('.catalog-product .name', { hasText: LONG_NAME })).toHaveText(LONG_NAME);

    await page.getByRole('link', { name: LONG_NAME }).click();
    await expect(page).toHaveURL(/\/item\/13$/);
    await expect(page.getByRole('heading', { name: LONG_NAME })).toBeVisible();
    await expect(page.locator('.item-desc')).toHaveText('Name uses the full 50 characters.');
    await expect(page.locator('.price')).toHaveText('$15.00');
  });

  test('product page shows the lowest and highest prices', async ({ page }) => {
    await page.goto('/item/1');
    await expect(page.getByRole('heading', { name: 'Edge 01 Penny Boot' })).toBeVisible();
    await expect(page.locator('.item-meta dd')).toHaveText('Edge Alpha');
    await expect(page.locator('.add-to-cart .price')).toHaveText('$0.01');

    await page.goto('/item/5');
    await expect(page.getByRole('heading', { name: 'Edge 05 Expedition Pack' })).toBeVisible();
    await expect(page.locator('.item-meta dd')).toHaveText('Edge Beta');
    await expect(page.locator('.add-to-cart .price')).toHaveText('$9,999.99');
  });

  test('product page with an empty description renders without one', async ({ page }) => {
    await page.goto('/item/8');
    await expect(page.getByRole('heading', { name: 'Edge 08 Blank Description Tent' })).toBeVisible();
    await expect(page.locator('.item-desc')).toHaveText('');
    await expect(page.locator('.add-to-cart .price')).toHaveText('$120.00');
  });

  test('product images are served for seeded picture file names', async ({ page }) => {
    await page.goto('/item/1');
    const image = page.locator('.item-media-img');
    await expect(image).toBeVisible();
    await expect.poll(() => image.evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth)).toBeGreaterThan(0);
  });

  test('unknown product id shows the not-found message', async ({ page }) => {
    const response = await page.goto('/item/9999');
    expect(response?.status()).toBe(404);
    await expect(page.getByText("Sorry, we couldn't find what you were looking for.")).toBeVisible();
  });

  test('anonymous visitors are asked to log in to buy the zero-stock item', async ({ page }) => {
    await page.goto('/item/6');
    await expect(page.getByRole('heading', { name: 'Edge 06 Zero Stock Pack' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Log in to purchase' })).toBeVisible();
  });

  test('the zero-stock item is listed and can still be added to the cart', async ({ page }) => {
    // The WebApp does not look at AvailableStock: no "sold out" marker and no disabled button.
    await page.goto('/');
    const card = page.locator('.catalog-product', { hasText: 'Edge 06 Zero Stock Pack' });
    await expect(card).toBeVisible();
    await expect(card).not.toContainText(/sold out|out of stock/i);

    await login(page, 'alice');
    await page.goto('/item/6');
    const addButton = page.getByRole('button', { name: 'Add to shopping bag' });
    await expect(addButton).toBeEnabled();
    await addButton.click();
    await expect(page.locator('.in-cart')).toContainText('1 in your shopping bag');

    await page.goto('/item/5');
    await page.getByRole('button', { name: 'Add to shopping bag' }).click();
    await expect(page.locator('.in-cart')).toContainText('1 in your shopping bag');

    await page.goto('/cart');
    await expect(page.locator('.cart-item')).toHaveCount(2);
    const line = (name: string) => page.locator('.cart-item', { hasText: name });
    await expect(line('Edge 06 Zero Stock Pack').locator('.price')).toHaveText('$45.00');
    await expect(line('Edge 05 Expedition Pack').locator('.price')).toHaveText('$9,999.99');
    await expect(page.locator('.cart-summary-total')).toContainText('$10,044.99');
  });
});
