import { test, expect } from '../demo';
import { loadScenario, login } from './scenario';

// Example of a narrated feature video. Every e2e test is recorded; the `demo` fixture adds chapter cards,
// captions and highlights so the video reads as a demo of the feature.

test.describe.configure({ mode: 'serial' });

test.describe('add to cart (narrated)', () => {
  test.beforeAll(() => {
    loadScenario('default');
  });

  test('alice adds a product to her shopping bag', async ({ page, demo }) => {
    await demo.chapter('Add to shopping bag', 'A signed-in shopper adds a product and sees it in the bag.');

    await demo.caption('Sign in as alice');
    await login(page, 'alice');

    await demo.caption('Open a product from the catalog');
    await page.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
    const heading = page.getByRole('heading', { name: 'Adventurer GPS Watch' });
    await expect(heading).toBeVisible();
    await demo.highlight(heading, 'Product details page');

    await demo.caption('Add it to the shopping bag');
    await page.getByRole('button', { name: 'Add to shopping bag' }).click();
    const inCart = page.locator('.in-cart').getByRole('link', { name: 'shopping bag' });
    await expect(inCart).toBeVisible();
    await demo.highlight(inCart, 'Confirmation: item is in the bag');

    await demo.chapter('Verify the shopping bag', 'The product is listed with quantity 1.');
    await page.goto('/cart');
    const line = page.locator('.cart-item').filter({ has: page.getByText('Adventurer GPS Watch', { exact: true }) });
    await expect(line).toBeVisible();
    await expect(line.locator('[data-cart-quantity-input]')).toHaveValue('1');
    await demo.highlight(line, 'Adventurer GPS Watch × 1', 2500);
  });
});
