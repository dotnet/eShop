import { randomUUID } from 'crypto';
import { APIRequestContext, Browser, Page, expect } from '@playwright/test';
import { login } from './scenario';

export const ALICE_ID = 'a11ce000-0000-4000-8000-000000000001';
export const BOB_ID = 'b0b00000-0000-4000-8000-000000000002';

export const ORDERING_URL = 'http://localhost:18084/api/orders';
export const WEBHOOKS_URL = 'http://localhost:18085/api/webhooks';
export const WEBHOOKS_CLIENT_URL = 'http://localhost:18086';
export const CATALOG_URL = 'http://localhost:18082/api/catalog';
export const V1 = 'api-version=1.0';

// The Swagger UI clients use the implicit flow, so a signed-in browser gets an access token straight from
// /connect/authorize. Their redirect URIs point at container host names (ordering-api, webhooks-api) that the
// browser cannot resolve, so the token is read from the Location header of the authorize response.
const swaggerClients = {
  ordering: { clientId: 'orderingswaggerui', scope: 'orders', host: 'ordering-api' },
  webhooks: { clientId: 'webhooksswaggerui', scope: 'webhooks', host: 'webhooks-api' },
} as const;

export async function accessToken(page: Page, api: keyof typeof swaggerClients): Promise<string> {
  const { clientId, scope, host } = swaggerClients[api];
  const params = new URLSearchParams({
    client_id: clientId,
    response_type: 'token',
    scope,
    redirect_uri: `http://${host}:8080/swagger/oauth2-redirect.html`,
    state: randomUUID(),
    nonce: randomUUID(),
  });
  const authorize = page.waitForResponse((r) => r.url().includes('/connect/authorize'));
  await page.goto(`http://host.docker.internal:18081/connect/authorize?${params}`).catch(() => {
    // Expected: the redirect target is not resolvable from the host.
  });
  const location = (await authorize).headers()['location'];
  expect(location, 'authorize did not redirect with a token').toContain('access_token=');
  return new URLSearchParams(new URL(location).hash.slice(1)).get('access_token')!;
}

/** Signs the user in (in a fresh context) and returns a bearer token for the given API. */
export async function tokenFor(browser: Browser, user: string, api: keyof typeof swaggerClients): Promise<string> {
  const context = await browser.newContext();
  try {
    const page = await context.newPage();
    await login(page, user);
    return await accessToken(page, api);
  } finally {
    await context.close();
  }
}

export const bearer = (token: string, extra: Record<string, string> = {}) => ({
  Authorization: `Bearer ${token}`,
  ...extra,
});

export type OrderSummary = { orderNumber: number; date: string; status: string; total: number };
export type OrderDetails = {
  orderNumber: number;
  status: string;
  description: string | null;
  street: string;
  city: string;
  state: string;
  zipcode: string;
  country: string;
  total: number;
  orderItems: { productName: string; units: number; unitPrice: number }[];
};

export async function listOrders(request: APIRequestContext, token: string): Promise<OrderSummary[]> {
  const response = await request.get(`${ORDERING_URL}?${V1}`, { headers: bearer(token) });
  expect(response.status()).toBe(200);
  return response.json();
}

export async function getOrder(request: APIRequestContext, token: string, id: number): Promise<OrderDetails> {
  const response = await request.get(`${ORDERING_URL}/${id}?${V1}`, { headers: bearer(token) });
  expect(response.status(), `GET order ${id}`).toBe(200);
  return response.json();
}

export function changeStatus(
  request: APIRequestContext,
  token: string,
  action: 'cancel' | 'ship',
  orderNumber: number,
  requestId: string = randomUUID(),
) {
  return request.put(`${ORDERING_URL}/${action}?${V1}`, {
    headers: bearer(token, { 'x-requestid': requestId }),
    data: { orderNumber },
  });
}
