import { test, expect, APIRequestContext, APIResponse } from '@playwright/test';
import { loadScenario } from './scenario';

// Read-only Catalog.API contract tests against http://localhost:18082.
// Seed data: testing/scenarios/catalog-pictures = catalog-edge (items 1-14) plus
//   15 "Edge 15 No Picture" (PictureFileName null, Tents, Alpha)
//   16 "Edge 16 Missing Picture" (missing.webp, Tents, Alpha)
//   17 "Edge 17 100% Wool_Sock" (1.webp, Boots, Alpha)
// Brands: 1 Alpha 8 (1-4,14-17), 2 Beta 4 (5-8), 3 Gamma 5 (9-13), 4 Unused 0.
// Types: 1 Boots 5 (1,2,3,11,17), 2 Packs 5 (4,5,6,12,13), 3 Tents 7 (7-10,14,15,16), 4 Unused 0.
// Flow map: testing/flows/catalog.md.
const API = 'http://localhost:18082/api/catalog';
const TOTAL = 17;

type Item = { id: number; name: string; price: number; catalogBrandId: number; catalogTypeId: number; catalogBrand: { brand: string } | null };
type Page = { pageIndex: number; pageSize: number; count: number; data: Item[] };

const ids = (page: Page) => page.data.map(i => i.id);

async function getJson<T>(request: APIRequestContext, url: string): Promise<T> {
  const response = await request.get(`${API}${url}`);
  expect(response.status(), `${url}: ${await response.text()}`).toBe(200);
  return (await response.json()) as T;
}

async function expectProblem(response: APIResponse, status: number, code?: string) {
  expect(response.status()).toBe(status);
  expect(response.headers()['content-type']).toContain('application/problem+json');
  const body = await response.json();
  expect(body.status).toBe(status);
  if (code) {
    expect(body.code).toBe(code);
  }
  return body;
}

test.describe.configure({ mode: 'default' });

test.describe('Catalog.API read endpoints', () => {
  test.beforeAll(() => {
    loadScenario('catalog-pictures');
  });

  test.describe('list items', () => {
    test('v1 returns the first page of 10 sorted by name, with brands and version headers', async ({ request }) => {
      const response = await request.get(`${API}/items?api-version=1.0`);
      expect(response.status()).toBe(200);
      expect(response.headers()['api-supported-versions']).toBe('1.0, 2.0');
      const page = (await response.json()) as Page;
      expect(page).toMatchObject({ pageIndex: 0, pageSize: 10, count: TOTAL });
      expect(ids(page)).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
      expect(page.data[0]).toMatchObject({ name: 'Edge 01 Penny Boot', price: 0.01, catalogBrand: { brand: 'Edge Alpha' } });
      expect(page.data[0]).not.toHaveProperty('embedding');
    });

    test('"1" is accepted as a short form of version 1.0', async ({ request }) => {
      expect((await getJson<Page>(request, '/items?api-version=1')).count).toBe(TOTAL);
    });

    test('v1 ignores the v2-only name, type and brand filters', async ({ request }) => {
      const page = await getJson<Page>(request, '/items?api-version=1.0&name=Edge%2001&type=1&brand=2');
      expect(page.count).toBe(TOTAL);
    });

    test('pageSize and pageIndex select a window; the last page is partial', async ({ request }) => {
      const page = await getJson<Page>(request, '/items?api-version=2.0&pageSize=5&pageIndex=3');
      expect(page).toMatchObject({ pageIndex: 3, pageSize: 5, count: TOTAL });
      expect(ids(page)).toEqual([16, 17]);
    });

    test('a page beyond the end and a zero page size return no data but the full count', async ({ request }) => {
      expect(await getJson<Page>(request, '/items?api-version=1.0&pageIndex=100')).toEqual({ pageIndex: 100, pageSize: 10, count: TOTAL, data: [] });
      expect(await getJson<Page>(request, '/items?api-version=1.0&pageSize=0')).toEqual({ pageIndex: 0, pageSize: 0, count: TOTAL, data: [] });
    });

    test('non-numeric paging parameters are rejected with 400', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items?api-version=1.0&pageSize=abc`), 400);
      await expectProblem(await request.get(`${API}/items?api-version=1.0&pageIndex=1.5`), 400);
    });

    // BUG: negative paging values reach Postgres unchecked ("LIMIT/OFFSET must not be negative") and the
    // API answers 500 with an empty body. They should be rejected as a 400 validation problem.
    test.fixme('negative pageSize or pageIndex is rejected with 400', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items?api-version=1.0&pageSize=-1`), 400);
      await expectProblem(await request.get(`${API}/items?api-version=1.0&pageIndex=-1`), 400);
    });
  });

  test.describe('v2 filters', () => {
    test('name is a case-sensitive prefix match', async ({ request }) => {
      expect(ids(await getJson<Page>(request, '/items?api-version=2.0&name=Edge%200'))).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9]);
      expect((await getJson<Page>(request, '/items?api-version=2.0&name=edge')).count).toBe(0);
      expect((await getJson<Page>(request, '/items?api-version=2.0&name=Boot')).count).toBe(0);
      expect((await getJson<Page>(request, '/items?api-version=2.0&name=')).count).toBe(TOTAL);
    });

    test('LIKE wildcards in the name are matched literally', async ({ request }) => {
      expect(ids(await getJson<Page>(request, '/items?api-version=2.0&name=Edge%2017%20100%25'))).toEqual([17]);
      expect(ids(await getJson<Page>(request, '/items?api-version=2.0&name=Edge%2017%20100%25%20Wool_'))).toEqual([17]);
      // "_" and "%" would match any character(s) if they were not escaped.
      expect((await getJson<Page>(request, '/items?api-version=2.0&name=Edge%201_')).count).toBe(0);
      expect((await getJson<Page>(request, '/items?api-version=2.0&name=%25')).count).toBe(0);
    });

    test('types and brands are additive within a facet and intersect across facets', async ({ request }) => {
      expect((await getJson<Page>(request, '/items?api-version=2.0&type=1')).count).toBe(5);
      expect((await getJson<Page>(request, '/items?api-version=2.0&brand=2&brand=3&pageSize=20')).count).toBe(9);
      const bootsAlpha = await getJson<Page>(request, '/items?api-version=2.0&type=1&brand=1');
      expect(ids(bootsAlpha)).toEqual([1, 2, 3, 17]);
      const named = await getJson<Page>(request, '/items?api-version=2.0&type=3&brand=1&name=Edge%2015');
      expect(ids(named)).toEqual([15]);
    });

    test('unknown or unused brand and type ids return an empty page', async ({ request }) => {
      for (const query of ['brand=999', 'type=999', 'brand=4', 'type=4', 'brand=-1', 'brand=2&type=1']) {
        const page = await getJson<Page>(request, `/items?api-version=2.0&${query}`);
        expect(page, query).toMatchObject({ count: 0, data: [] });
      }
    });

    test('non-integer filter ids are rejected with 400', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items?api-version=2.0&type=abc`), 400);
      await expectProblem(await request.get(`${API}/items?api-version=2.0&brand=1&brand=x`), 400);
    });
  });

  test.describe('single item', () => {
    test('returns the item with its brand under both versions', async ({ request }) => {
      for (const version of ['1.0', '2.0']) {
        const item = await getJson<Item & { description: string; pictureFileName: string; availableStock: number }>(request, `/items/6?api-version=${version}`);
        expect(item).toMatchObject({
          id: 6,
          name: 'Edge 06 Zero Stock Pack',
          price: 45,
          pictureFileName: '6.webp',
          availableStock: 0,
          catalogBrandId: 2,
          catalogTypeId: 2,
          catalogBrand: { id: 2, brand: 'Edge Beta' },
        });
      }
    });

    test('an unknown id is 404 problem details', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items/99999?api-version=1.0`), 404);
    });

    test('zero and negative ids are 400 "Id is not valid"', async ({ request }) => {
      for (const id of [0, -1]) {
        const response = await request.get(`${API}/items/${id}?api-version=1.0`);
        expect(response.status()).toBe(400);
        expect(await response.json()).toMatchObject({ status: 400, detail: 'Id is not valid' });
      }
    });

    test('non-integer and overflowing ids do not match the route (404)', async ({ request }) => {
      for (const id of ['abc', '1.5', '2147483648']) {
        expect((await request.get(`${API}/items/${id}?api-version=1.0`)).status(), id).toBe(404);
      }
    });
  });

  test.describe('batch get by ids', () => {
    test('returns the existing items and skips unknown and duplicate ids', async ({ request }) => {
      const items = await getJson<Item[]>(request, '/items/by?api-version=1.0&ids=5&ids=1&ids=99999&ids=5&ids=-3');
      expect(items.map(i => i.id).sort((a, b) => a - b)).toEqual([1, 5]);
    });

    test('no ids returns an empty array', async ({ request }) => {
      expect(await getJson<Item[]>(request, '/items/by?api-version=2.0')).toEqual([]);
    });

    test('a non-integer id is rejected with 400', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items/by?api-version=1.0&ids=1&ids=abc`), 400);
    });
  });

  test.describe('v1-only routes', () => {
    test('by name returns a paginated prefix match', async ({ request }) => {
      const page = await getJson<Page>(request, '/items/by/Edge%201?api-version=1.0&pageSize=3');
      expect(page).toMatchObject({ pageSize: 3, count: 8 });
      expect(ids(page)).toEqual([10, 11, 12]);
      expect((await getJson<Page>(request, '/items/by/Nope?api-version=1.0')).count).toBe(0);
    });

    test('by type and brand, by type only, by brand only and all', async ({ request }) => {
      expect(ids(await getJson<Page>(request, '/items/type/1/brand/1?api-version=1.0'))).toEqual([1, 2, 3, 17]);
      expect((await getJson<Page>(request, '/items/type/3/brand?api-version=1.0')).count).toBe(7);
      expect(ids(await getJson<Page>(request, '/items/type/all/brand/2?api-version=1.0'))).toEqual([5, 6, 7, 8]);
      expect((await getJson<Page>(request, '/items/type/all/brand?api-version=1.0')).count).toBe(TOTAL);
      expect((await getJson<Page>(request, '/items/type/2/brand/4?api-version=1.0')).count).toBe(0);
    });

    test('non-integer type or brand ids are rejected', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items/type/abc/brand/1?api-version=1.0`), 400);
      await expectProblem(await request.get(`${API}/items/type/1/brand/abc?api-version=1.0`), 400);
      // "all/brand/{brandId:int?}" has a route constraint, so a bad brand there does not match at all.
      expect((await request.get(`${API}/items/type/all/brand/abc?api-version=1.0`)).status()).not.toBe(200);
    });

    test('are not available in v2', async ({ request }) => {
      for (const url of ['/items/by/Edge?api-version=2.0', '/items/type/1/brand/1?api-version=2.0', '/items/type/all/brand/1?api-version=2.0', '/items/withsemanticrelevance/Edge?api-version=2.0']) {
        await expectProblem(await request.get(`${API}${url}`), 400, 'UnsupportedApiVersion');
      }
    });
  });

  test.describe('semantic search with AI disabled', () => {
    test('falls back to a name prefix match in v1 and v2', async ({ request }) => {
      expect(ids(await getJson<Page>(request, '/items/withsemanticrelevance/Edge%2005?api-version=1.0'))).toEqual([5]);
      const v2 = await getJson<Page>(request, '/items/withsemanticrelevance?api-version=2.0&text=Edge%201');
      expect(v2.count).toBe(8);
      expect((await getJson<Page>(request, '/items/withsemanticrelevance?api-version=2.0&text=warm%20boots')).count).toBe(0);
    });

    test('v2 requires the text parameter', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items/withsemanticrelevance?api-version=2.0`), 400);
    });
  });

  test.describe('brands, types and facets', () => {
    test('brands and types are listed alphabetically, including unused ones', async ({ request }) => {
      expect(await getJson(request, '/catalogbrands?api-version=1.0')).toEqual([
        { id: 1, brand: 'Edge Alpha' },
        { id: 2, brand: 'Edge Beta' },
        { id: 3, brand: 'Edge Gamma' },
        { id: 4, brand: 'Edge Unused Brand' },
      ]);
      expect(await getJson(request, '/catalogtypes?api-version=2.0')).toEqual([
        { id: 1, type: 'Edge Boots' },
        { id: 2, type: 'Edge Packs' },
        { id: 3, type: 'Edge Tents' },
        { id: 4, type: 'Edge Unused Type' },
      ]);
    });

    type Facets = { brandCounts: { id: number; count: number }[]; typeCounts: { id: number; count: number }[]; brandTotal: number; typeTotal: number };
    const asMap = (counts: { id: number; count: number }[]) => Object.fromEntries(counts.map(c => [c.id, c.count]));

    test('facets count every brand and type; unused ones are absent', async ({ request }) => {
      const facets = await getJson<Facets>(request, '/items/facets?api-version=1.0');
      expect(asMap(facets.brandCounts)).toEqual({ 1: 8, 2: 4, 3: 5 });
      expect(asMap(facets.typeCounts)).toEqual({ 1: 5, 2: 5, 3: 7 });
      expect(facets.brandTotal).toBe(TOTAL);
      expect(facets.typeTotal).toBe(TOTAL);
    });

    test('brand counts are scoped by type and type counts by brand', async ({ request }) => {
      const facets = await getJson<Facets>(request, '/items/facets?api-version=2.0&type=2&brand=3');
      expect(asMap(facets.brandCounts)).toEqual({ 1: 1, 2: 2, 3: 2 });
      expect(facets.brandTotal).toBe(5);
      expect(asMap(facets.typeCounts)).toEqual({ 1: 1, 2: 2, 3: 2 });
      expect(facets.typeTotal).toBe(5);

      const none = await getJson<Facets>(request, '/items/facets?api-version=1.0&brand=999&type=999');
      expect(none).toEqual({ brandCounts: [], typeCounts: [], brandTotal: 0, typeTotal: 0 });
    });

    test('non-integer facet filters are rejected with 400', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items/facets?api-version=1.0&brand=abc`), 400);
    });
  });

  test.describe('pictures', () => {
    test('serves the picture with its image content type and Last-Modified', async ({ request }) => {
      for (const id of [1, 17]) {
        const response = await request.get(`${API}/items/${id}/pic?api-version=1.0`);
        expect(response.status()).toBe(200);
        expect(response.headers()['content-type']).toBe('image/webp');
        expect(response.headers()['last-modified']).toBeTruthy();
        const body = await response.body();
        expect(body.subarray(0, 4).toString('ascii')).toBe('RIFF');
        expect(body.subarray(8, 12).toString('ascii')).toBe('WEBP');
      }
    });

    test('an item without a picture, a missing file and an unknown item are 404', async ({ request }) => {
      for (const id of [15, 16, 99999]) {
        await expectProblem(await request.get(`${API}/items/${id}/pic?api-version=2.0`), 404);
      }
    });
  });

  test.describe('API versioning and routing errors', () => {
    test('a missing version is 400 ApiVersionUnspecified and lists the supported versions', async ({ request }) => {
      const response = await request.get(`${API}/items`);
      await expectProblem(response, 400, 'ApiVersionUnspecified');
      expect(response.headers()['api-supported-versions']).toBe('1.0, 2.0');
    });

    test('the version is only read from the query string', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items`, { headers: { 'api-version': '1.0' } }), 400, 'ApiVersionUnspecified');
    });

    test('unsupported, invalid and ambiguous versions are 400', async ({ request }) => {
      await expectProblem(await request.get(`${API}/items?api-version=3.0`), 400, 'UnsupportedApiVersion');
      await expectProblem(await request.get(`${API}/items/1?api-version=abc`), 400, 'InvalidApiVersion');
      await expectProblem(await request.get(`${API}/items?api-version=1.0&api-version=2.0`), 400, 'AmbiguousApiVersion');
    });

    test('unknown routes are 404 and unsupported methods are 405', async ({ request }) => {
      await expectProblem(await request.get(`${API}/nothing?api-version=1.0`), 404);
      await expectProblem(await request.get('http://localhost:18082/'), 404);
      await expectProblem(await request.post(`${API}/catalogbrands?api-version=1.0`), 405);
      await expectProblem(await request.patch(`${API}/items/1?api-version=1.0`, { data: {} }), 405);
    });

    test('OpenAPI documents are published per version', async ({ request }) => {
      const v1 = await (await request.get('http://localhost:18082/openapi/v1.json')).json();
      expect(Object.keys(v1.paths)).toEqual(expect.arrayContaining(['/api/catalog/items/by/{name}', '/api/catalog/items/{id}/pic']));
      const v2 = await (await request.get('http://localhost:18082/openapi/v2.json')).json();
      expect(Object.keys(v2.paths)).toContain('/api/catalog/items/withsemanticrelevance');
      expect(Object.keys(v2.paths)).not.toContain('/api/catalog/items/by/{name}');
    });
  });
});
