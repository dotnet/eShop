import { test, expect, Page } from '@playwright/test';
import { loadScenario } from './scenario';

// Seed data: testing/scenarios/default. 101 items sorted by name, 9 per page = 12 pages (the last has 2).
// Flow map: testing/flows/catalog.md.
const names = (page: Page) => page.locator('.catalog-product .name');
const pager = (page: Page) => page.getByRole('navigation', { name: 'Catalog pages' });

test.describe.configure({ mode: 'default' });

test.describe('catalog pagination over the default catalog', () => {
  test.beforeAll(() => {
    loadScenario('default');
  });

  test('page 1 shows the first 9 of 101 products and a compact pager', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.catalog-count')).toHaveText('101 products');
    await expect(page.locator('.catalog-product')).toHaveCount(9);
    await expect(names(page).first()).toHaveText('Adventurer GPS Watch');
    await expect(names(page).last()).toHaveText('Arctic Shield Insulated Jacket');

    await expect(pager(page).locator('.page-status')).toHaveText('1 / 12');
    // First, current +/- 1 and last, with an ellipsis for the gap.
    await expect(pager(page).locator('.page-number')).toHaveText(['1', '2', '12']);
    await expect(pager(page).locator('.page-ellipsis')).toHaveCount(1);
    await expect(pager(page).locator('[aria-current="page"]')).toHaveText('1');
    await expect(pager(page).locator('.page-nav.is-disabled')).toHaveText(/Prev/);
  });

  test('a middle page shows ellipses on both sides', async ({ page }) => {
    await page.goto('/?page=6');
    await expect(names(page).first()).toHaveText('GripTrek Hiking Poles');
    await expect(pager(page).locator('.page-number')).toHaveText(['1', '5', '6', '7', '12']);
    await expect(pager(page).locator('.page-ellipsis')).toHaveCount(2);
    await expect(pager(page).locator('.page-nav.is-disabled')).toHaveCount(0);
  });

  test('Next and Prev move one page and page 1 drops the page parameter', async ({ page }) => {
    await page.goto('/');
    await pager(page).getByRole('link', { name: 'Next page' }).click();
    await expect(page).toHaveURL(/\?page=2$/);
    await expect(pager(page).locator('.page-status')).toHaveText('2 / 12');
    await expect(names(page).first()).not.toHaveText('Adventurer GPS Watch');

    await pager(page).getByRole('link', { name: 'Previous page' }).click();
    await expect(page).not.toHaveURL(/page=/);
    await expect(names(page).first()).toHaveText('Adventurer GPS Watch');
  });

  test('the last page holds the remaining 2 products and disables Next', async ({ page }) => {
    await page.goto('/');
    await pager(page).getByRole('link', { name: '12', exact: true }).click();
    await expect(page).toHaveURL(/\?page=12$/);
    await expect(names(page)).toHaveText(['Zenith Cycling Jersey', 'Zero Gravity Ski Goggles']);
    await expect(pager(page).locator('.page-nav.is-disabled')).toHaveText(/Next/);
  });

  test('every page is distinct and together they list all 101 products', async ({ page }) => {
    const seen: string[] = [];
    for (let p = 1; p <= 12; p++) {
      await page.goto(p === 1 ? '/' : `/?page=${p}`);
      await expect(page.locator('.catalog-count')).toHaveText('101 products');
      seen.push(...(await names(page).allTextContents()));
    }
    expect(seen).toHaveLength(101);
    // Names are sorted ascending across pages (Postgres collation, same order as the API).
    expect(seen[0]).toBe('Adventurer GPS Watch');
    expect(seen[100]).toBe('Zero Gravity Ski Goggles');
  });
});
