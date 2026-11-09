import { test, expect } from '@playwright/test';
import { loadScenario } from './scenario';

// Mobile BFF (YARP, http://localhost:18087) catalog routes, configured in
// src/eShop.AppHost/Extensions.cs (ConfigureMobileBffRoutes). Every route matches only when the
// api-version query value is one of the listed values; anything else matches no route and YARP answers 404.
// Seed data: testing/scenarios/catalog-edge (14 items; brands/types 1-4).
// Flow map: testing/flows/catalog.md.
const BFF = 'http://localhost:18087';
const PREFIXED = `${BFF}/catalog-api/api/catalog`;

test.describe.configure({ mode: 'default' });

test.describe('mobile BFF catalog routes', () => {
  test.beforeAll(() => {
    loadScenario('catalog-edge');
  });

  test('the /catalog-api prefix is stripped and requests reach Catalog.API', async ({ request }) => {
    const response = await request.get(`${PREFIXED}/items?api-version=1.0&pageSize=3`);
    expect(response.status()).toBe(200);
    expect(response.headers()['api-supported-versions']).toBe('1.0, 2.0');
    const page = await response.json();
    expect(page).toMatchObject({ pageSize: 3, count: 14 });
    expect(page.data.map((i: { name: string }) => i.name)).toEqual(['Edge 01 Penny Boot', 'Edge 02 Trail Boot', 'Edge 03 Summit Boot']);
  });

  test('every documented catalog route answers through the BFF', async ({ request }) => {
    const routes: [string, (body: any) => void][] = [
      ['/items?api-version=2.0&brand=2', b => expect(b.count).toBe(4)],
      ['/items/by?api-version=1.0&ids=1&ids=2', b => expect(b).toHaveLength(2)],
      ['/items/5?api-version=2.0', b => expect(b.name).toBe('Edge 05 Expedition Pack')],
      ['/items/by/Edge%2001?api-version=1', b => expect(b.count).toBe(1)],
      ['/items/withsemanticrelevance/Edge%2001?api-version=1.0', b => expect(b.count).toBe(1)],
      ['/items/withsemanticrelevance?api-version=2.0&text=Edge%2001', b => expect(b.count).toBe(1)],
      ['/items/type/1/brand/1?api-version=1.0', b => expect(b.count).toBe(3)],
      ['/items/type/all/brand/3?api-version=1.0', b => expect(b.count).toBe(5)],
      ['/catalogTypes?api-version=1.0', b => expect(b).toHaveLength(4)],
      ['/catalogBrands?api-version=2.0', b => expect(b[0].brand).toBe('Edge Alpha')],
    ];
    for (const [path, check] of routes) {
      const response = await request.get(`${PREFIXED}${path}`);
      expect(response.status(), path).toBe(200);
      check(await response.json());
    }
  });

  test('pictures are proxied with their content type', async ({ request }) => {
    const response = await request.get(`${PREFIXED}/items/1/pic?api-version=1.0`);
    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toBe('image/webp');
  });

  test('the generic /api/catalog catch-all forwards unprefixed paths, including facets', async ({ request }) => {
    const items = await request.get(`${BFF}/api/catalog/items?api-version=1.0`);
    expect(items.status()).toBe(200);
    expect((await items.json()).count).toBe(14);
    const facets = await request.get(`${BFF}/api/catalog/items/facets?api-version=2.0`);
    expect(facets.status()).toBe(200);
    expect((await facets.json()).brandTotal).toBe(14);
  });

  test('Catalog.API errors pass through unchanged', async ({ request }) => {
    const notFound = await request.get(`${PREFIXED}/items/99999?api-version=1.0`);
    expect(notFound.status()).toBe(404);
    expect(notFound.headers()['content-type']).toContain('application/problem+json');

    const invalidId = await request.get(`${PREFIXED}/items/0?api-version=1.0`);
    expect(invalidId.status()).toBe(400);
    expect((await invalidId.json()).detail).toBe('Id is not valid');

    expect((await request.get(`${PREFIXED}/items/99999/pic?api-version=2.0`)).status()).toBe(404);
  });

  test('a missing or unlisted api-version matches no BFF route (404, not the API 400)', async ({ request }) => {
    for (const url of [
      `${PREFIXED}/items`,
      `${PREFIXED}/items?api-version=3.0`,
      `${PREFIXED}/items?api-version=2`,
      `${BFF}/api/catalog/items`,
      `${BFF}/api/catalog/items?api-version=3.0`,
    ]) {
      expect((await request.get(url)).status(), url).toBe(404);
    }
  });

  test('v1-only routes are not routed for version 2.0', async ({ request }) => {
    for (const path of ['/items/by/Edge?api-version=2.0', '/items/type/1/brand/1?api-version=2.0', '/items/withsemanticrelevance/Edge?api-version=2.0']) {
      expect((await request.get(`${PREFIXED}${path}`)).status(), path).toBe(404);
    }
    // The v2-only search route is not matched for 1.0, but "/items/{id}" (no int constraint in YARP)
    // catches it and Catalog.API rejects the version instead.
    const search = await request.get(`${PREFIXED}/items/withsemanticrelevance?api-version=1.0&text=Edge`);
    expect(search.status()).toBe(400);
    expect((await search.json()).code).toBe('UnsupportedApiVersion');
  });

  test('unknown paths under the BFF are 404', async ({ request }) => {
    expect((await request.get(`${BFF}/nothing`)).status()).toBe(404);
    expect((await request.get(`${BFF}/catalog-api/api/other?api-version=1.0`)).status()).toBe(404);
  });
});
