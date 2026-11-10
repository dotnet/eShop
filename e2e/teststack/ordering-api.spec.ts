import { randomUUID } from 'crypto';
import { test, expect } from '../fixtures';
import { loadScenario } from './scenario';
import {
  ALICE_ID, ORDERING_URL, V1, bearer, changeStatus, getOrder, listOrders, tokenFor,
} from './ordering-helpers';

// testing/scenarios/ordering-two-buyers: alice owns orders 1-6, bob owns 7 and 8.
const aliceOrders = [
  { orderNumber: 1, status: 'Submitted', total: 109.99 },
  { orderNumber: 2, status: 'Submitted', total: 119.98 },
  { orderNumber: 3, status: 'StockConfirmed', total: 149.99 },
  { orderNumber: 4, status: 'Paid', total: 249.98 },
  { orderNumber: 5, status: 'Shipped', total: 299.99 },
  { orderNumber: 6, status: 'Cancelled', total: 79.99 },
];

const createOrderBody = (userId: string, userName: string, items: object[]) => ({
  userId,
  userName,
  city: 'Redmond',
  street: '1 Microsoft Way',
  state: 'WA',
  country: 'U.S.',
  zipCode: '98052',
  cardNumber: '4012888888881881',
  cardHolderName: 'Test Holder',
  cardExpiration: new Date(Date.now() + 365 * 24 * 3600 * 1000).toISOString(),
  cardSecurityNumber: '123',
  cardTypeId: 2,
  buyer: userId,
  items,
});

const harness = {
  id: randomUUID(), productId: 2, productName: 'Summit Pro Harness', unitPrice: 89.99, oldUnitPrice: 0, quantity: 2, pictureUrl: '',
};

let alice: string;
let bob: string;

test.describe.configure({ mode: 'serial' });

test.beforeAll(async ({ browser }) => {
  loadScenario('ordering-two-buyers');
  alice = await tokenFor(browser, 'alice', 'ordering');
  bob = await tokenFor(browser, 'bob', 'ordering');
});

test.describe('authentication and versioning', () => {
  test('anonymous and invalid-token requests are rejected with 401', async ({ request }) => {
    const calls = [
      request.get(`${ORDERING_URL}?${V1}`),
      request.get(`${ORDERING_URL}/1?${V1}`),
      request.get(`${ORDERING_URL}/cardtypes?${V1}`),
      request.post(`${ORDERING_URL}/draft?${V1}`, { data: { buyerId: ALICE_ID, items: [] } }),
      request.put(`${ORDERING_URL}/cancel?${V1}`, { headers: { 'x-requestid': randomUUID() }, data: { orderNumber: 1 } }),
      request.put(`${ORDERING_URL}/ship?${V1}`, { headers: { 'x-requestid': randomUUID() }, data: { orderNumber: 4 } }),
      request.post(`${ORDERING_URL}?${V1}`, { headers: { 'x-requestid': randomUUID() }, data: createOrderBody(ALICE_ID, 'alice', [harness]) }),
      request.get(`${ORDERING_URL}?${V1}`, { headers: { Authorization: 'Bearer not-a-real-token' } }),
    ];
    for (const response of await Promise.all(calls)) {
      expect(response.status(), response.url()).toBe(401);
    }
    // Nothing changed.
    expect(await listOrders(request, alice)).toHaveLength(6);
  });

  test('a missing or unsupported api-version is rejected with 400', async ({ request }) => {
    expect((await request.get(ORDERING_URL, { headers: bearer(alice) })).status()).toBe(400);
    const v2 = await request.get(`${ORDERING_URL}?api-version=2.0`, { headers: bearer(alice) });
    expect(v2.status()).toBe(400);
    expect((await v2.json()).code).toBe('UnsupportedApiVersion');
  });
});

test.describe('queries', () => {
  test('card type lookup returns Amex, Visa and MasterCard', async ({ request }) => {
    const response = await request.get(`${ORDERING_URL}/cardtypes?${V1}`, { headers: bearer(alice) });
    expect(response.status()).toBe(200);
    expect(await response.json()).toEqual([
      { id: 1, name: 'Amex' },
      { id: 2, name: 'Visa' },
      { id: 3, name: 'MasterCard' },
    ]);
  });

  test('each buyer lists only their own orders, with status and total', async ({ request }) => {
    const mine = await listOrders(request, alice);
    expect(mine.map(({ orderNumber, status, total }) => ({ orderNumber, status, total })).sort((a, b) => a.orderNumber - b.orderNumber))
      .toEqual(aliceOrders);

    const bobs = await listOrders(request, bob);
    expect(bobs.map((o) => o.orderNumber).sort()).toEqual([7, 8]);
    expect(bobs.find((o) => o.orderNumber === 7)).toMatchObject({ status: 'Paid', total: 599.97 });
  });

  test('order details return the address, items and total; an unknown order is 404', async ({ request }) => {
    const order = await getOrder(request, alice, 4);
    expect(order).toMatchObject({
      orderNumber: 4,
      status: 'Paid',
      street: '221 Baker Street',
      city: 'Boston',
      state: 'MA',
      zipcode: '02108',
      country: 'U.S.',
      total: 249.98,
    });
    expect(order.orderItems.map((i) => [i.productName, i.units, i.unitPrice]).sort()).toEqual([
      ['Carbon Fiber Trekking Poles', 1, 69.99],
      ['Frostbite Insulated Jacket', 1, 179.99],
    ]);

    expect((await request.get(`${ORDERING_URL}/999?${V1}`, { headers: bearer(alice) })).status()).toBe(404);
    expect((await request.get(`${ORDERING_URL}/abc?${V1}`, { headers: bearer(alice) })).status()).toBe(404);
  });

  test('a buyer cannot read another buyer\'s order details', async ({ request }) => {
    // BUG: GET /api/orders/{id} (OrdersApi.GetOrderAsync -> OrderQueries.GetOrderAsync) never checks the buyer,
    // so alice can read bob's order 7, including his shipping address (currently 200).
    test.fixme(true, 'Ordering.API returns any order to any authenticated user (IDOR)');
    const response = await request.get(`${ORDERING_URL}/7?${V1}`, { headers: bearer(alice) });
    expect([403, 404]).toContain(response.status());
  });
});

test.describe('draft orders', () => {
  test('a draft computes items and total from the basket without creating an order', async ({ request }) => {
    const response = await request.post(`${ORDERING_URL}/draft?${V1}`, {
      headers: bearer(alice),
      data: {
        buyerId: ALICE_ID,
        items: [harness, { ...harness, id: randomUUID(), productId: 10, productName: 'Trailblazer Bike Helmet', unitPrice: 59.99, quantity: 1 }],
      },
    });
    expect(response.status()).toBe(200);
    const draft = await response.json();
    expect(draft.total).toBeCloseTo(239.97, 2);
    expect(draft.orderItems).toHaveLength(2);
    expect(draft.orderItems[0]).toMatchObject({ productId: 2, units: 2, unitPrice: 89.99, discount: 0 });

    const empty = await request.post(`${ORDERING_URL}/draft?${V1}`, { headers: bearer(alice), data: { buyerId: ALICE_ID, items: [] } });
    expect(await empty.json()).toEqual({ orderItems: [], total: 0 });

    expect(await listOrders(request, alice)).toHaveLength(6);
  });

  test('a draft request without a body is rejected with 400', async ({ request }) => {
    // BUG: Ordering.API answers a POST /api/orders/draft without a JSON body with 500 instead of 400.
    test.fixme(true, 'missing draft body returns 500');
    const response = await request.post(`${ORDERING_URL}/draft?${V1}`, { headers: bearer(alice) });
    expect(response.status()).toBe(400);
  });
});

test.describe('cancel and ship', () => {
  test('cancel and ship require a valid, non-empty x-requestid header', async ({ request }) => {
    for (const action of ['cancel', 'ship'] as const) {
      const url = `${ORDERING_URL}/${action}?${V1}`;
      const orderNumber = action === 'cancel' ? 1 : 4;
      expect((await request.put(url, { headers: bearer(alice), data: { orderNumber } })).status()).toBe(400);
      expect((await request.put(url, { headers: bearer(alice, { 'x-requestid': 'not-a-guid' }), data: { orderNumber } })).status()).toBe(400);
      const empty = await request.put(url, {
        headers: bearer(alice, { 'x-requestid': '00000000-0000-0000-0000-000000000000' }),
        data: { orderNumber },
      });
      expect(empty.status()).toBe(400);
      expect(await empty.json()).toBe('Empty GUID is not valid for request ID');
    }
    expect((await getOrder(request, alice, 1)).status).toBe('Submitted');
    expect((await getOrder(request, alice, 4)).status).toBe('Paid');
  });

  test('cancelling a Submitted order succeeds; a repeated x-requestid is ignored', async ({ request }) => {
    const requestId = randomUUID();
    expect((await changeStatus(request, alice, 'cancel', 1, requestId)).status()).toBe(200);
    expect(await getOrder(request, alice, 1)).toMatchObject({ status: 'Cancelled', description: 'The order was cancelled.' });

    // Same request id, different order: reported as success but not executed (IdentifiedCommandHandler).
    expect((await changeStatus(request, alice, 'cancel', 3, requestId)).status()).toBe(200);
    expect((await getOrder(request, alice, 3)).status).toBe('StockConfirmed');

    // A new request id cancels a StockConfirmed order.
    expect((await changeStatus(request, alice, 'cancel', 3)).status()).toBe(200);
    expect((await getOrder(request, alice, 3)).status).toBe('Cancelled');
  });

  test('shipping a Paid order moves it to Shipped, after which it cannot be cancelled', async ({ request }) => {
    expect((await changeStatus(request, alice, 'ship', 4)).status()).toBe(200);
    expect(await getOrder(request, alice, 4)).toMatchObject({ status: 'Shipped', description: 'The order was shipped.' });

    const cancel = await changeStatus(request, alice, 'cancel', 4);
    expect(cancel.ok()).toBe(false);
    expect((await getOrder(request, alice, 4)).status).toBe('Shipped');
  });

  test('invalid transitions and unknown orders fail and leave orders unchanged', async ({ request }) => {
    const attempts: [action: 'cancel' | 'ship', orderNumber: number, expected: string | null][] = [
      ['ship', 2, 'Submitted'],
      ['ship', 6, 'Cancelled'],
      ['ship', 5, 'Shipped'], // shipping twice is not allowed either
      ['cancel', 5, 'Shipped'],
      ['cancel', 999, null],
      ['ship', 999, null],
    ];
    for (const [action, orderNumber, expected] of attempts) {
      const response = await changeStatus(request, alice, action, orderNumber);
      expect(response.ok(), `${action} ${orderNumber}`).toBe(false);
      if (expected) {
        expect((await getOrder(request, alice, orderNumber)).status).toBe(expected);
      }
    }
  });

  test('invalid transitions return a client error, not 500', async ({ request }) => {
    // BUG: IdentifiedCommandHandler swallows the OrderingDomainException and the endpoint maps any failure to
    // 500 "Cancel order failed to process." A business-rule violation should be 400/409 and an unknown order 404.
    test.fixme(true, 'rejected status changes are reported as 500 Internal Server Error');
    expect((await changeStatus(request, alice, 'cancel', 5)).status()).toBeGreaterThanOrEqual(400);
    expect((await changeStatus(request, alice, 'cancel', 5)).status()).toBeLessThan(500);
    expect((await changeStatus(request, alice, 'ship', 2)).status()).toBeLessThan(500);
    expect((await changeStatus(request, alice, 'cancel', 999)).status()).toBe(404);
  });

  test('a buyer cannot cancel or ship another buyer\'s order', async ({ request }) => {
    // BUG: CancelOrderCommandHandler / ShipOrderCommandHandler load the order by id only; any signed-in user can
    // cancel or ship anyone's order. (Ship is an admin action with no role check at all.)
    test.fixme(true, 'cancel/ship do not check order ownership');
    const ship = await changeStatus(request, alice, 'ship', 7);
    expect([403, 404]).toContain(ship.status());
    expect((await getOrder(request, bob, 7)).status).toBe('Paid');
  });
});

test.describe('create order', () => {
  test('creating an order requires x-requestid', async ({ request }) => {
    const body = createOrderBody(ALICE_ID, 'alice', [harness]);
    expect((await request.post(`${ORDERING_URL}?${V1}`, { headers: bearer(alice), data: body })).status()).toBe(400);
    const empty = await request.post(`${ORDERING_URL}?${V1}`, {
      headers: bearer(alice, { 'x-requestid': '00000000-0000-0000-0000-000000000000' }),
      data: body,
    });
    expect(empty.status()).toBe(400);
    expect(await empty.json()).toBe('RequestId is missing.');
    expect(await listOrders(request, alice)).toHaveLength(6);
  });

  test('posting the same x-requestid twice creates exactly one Submitted order', async ({ request }) => {
    const before = (await listOrders(request, alice)).map((o) => o.orderNumber);
    const requestId = randomUUID();
    const body = createOrderBody(ALICE_ID, 'alice', [harness]);
    for (let i = 0; i < 2; i++) {
      const response = await request.post(`${ORDERING_URL}?${V1}`, { headers: bearer(alice, { 'x-requestid': requestId }), data: body });
      expect(response.status()).toBe(200);
    }
    const created = (await listOrders(request, alice)).filter((o) => !before.includes(o.orderNumber));
    expect(created).toHaveLength(1);
    expect(created[0]).toMatchObject({ status: 'Submitted', total: 179.98 });

    const details = await getOrder(request, alice, created[0].orderNumber);
    expect(details).toMatchObject({ street: '1 Microsoft Way', city: 'Redmond', zipcode: '98052' });
    expect(details.orderItems).toEqual([expect.objectContaining({ productName: 'Summit Pro Harness', units: 2, unitPrice: 89.99 })]);
    // bob does not see it.
    expect((await listOrders(request, bob)).map((o) => o.orderNumber).sort()).toEqual([7, 8]);
  });

  test('an order without items or with an invalid card is not created', async ({ request }) => {
    const before = await listOrders(request, alice);
    const invalid = [
      createOrderBody(ALICE_ID, 'alice', []),
      { ...createOrderBody(ALICE_ID, 'alice', [harness]), cardNumber: '123' },
      { ...createOrderBody(ALICE_ID, 'alice', [harness]), cardExpiration: '2020-01-01T00:00:00Z' },
      { ...createOrderBody(ALICE_ID, 'alice', [harness]), city: '' },
    ];
    for (const body of invalid) {
      await request.post(`${ORDERING_URL}?${V1}`, { headers: bearer(alice, { 'x-requestid': randomUUID() }), data: body });
    }
    expect(await listOrders(request, alice)).toHaveLength(before.length);
  });

  test('an invalid order is rejected with 400', async ({ request }) => {
    // BUG: OrdersApi.CreateOrderAsync returns 200 OK even when CreateOrderCommand fails validation
    // (the result of mediator.Send is only logged), so clients believe the order was placed.
    test.fixme(true, 'create order returns 200 for an order that was not created');
    const response = await request.post(`${ORDERING_URL}?${V1}`, {
      headers: bearer(alice, { 'x-requestid': randomUUID() }),
      data: createOrderBody(ALICE_ID, 'alice', []),
    });
    expect(response.status()).toBe(400);
  });

  test('an order is created for the signed-in buyer, not the userId in the body', async ({ request }) => {
    // BUG: CreateOrderAsync trusts request.UserId, so bob can place an order on alice's account.
    test.fixme(true, 'create order uses the userId from the request body instead of the token');
    const before = (await listOrders(request, alice)).length;
    await request.post(`${ORDERING_URL}?${V1}`, {
      headers: bearer(bob, { 'x-requestid': randomUUID() }),
      data: createOrderBody(ALICE_ID, 'alice', [harness]),
    });
    expect(await listOrders(request, alice)).toHaveLength(before);
  });
});
